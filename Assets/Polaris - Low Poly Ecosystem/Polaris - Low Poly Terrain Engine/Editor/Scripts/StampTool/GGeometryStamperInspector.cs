using Pinwheel.Griffin.BackupTool;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Griffin.StampTool
{
    [CustomEditor(typeof(GGeometryStamper))]
    public class GGeometryStamperInspector : Editor
    {
        private GGeometryStamper instance;
        private Dictionary<GStylizedTerrain, RenderTexture> previewTextures;

        private const string INSTRUCTION = "Stamp features onto the terrain surface.";
        private static readonly string HISTORY_PREFIX = "Stamp Geometry";

        private readonly Vector3[] worldPoints = new Vector3[4];
        private readonly Vector2[] normalizedPoints = new Vector2[4];
        private readonly Vector3[] worldBox = new Vector3[8];

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            SceneView.duringSceneGui += DuringSceneGUI;
            instance = target as GGeometryStamper;
            Tools.hidden = true;

            instance.Internal_UpdateFalloffTexture();
            GCommon.RegisterBeginRender(OnCameraRender);
            GCommon.RegisterBeginRenderSRP(OnCameraRenderSRP);
            UpdatePreview();
        }

        private void OnDisable()
        {
            Tools.hidden = false;
            Undo.undoRedoPerformed -= OnUndoRedo;
            SceneView.duringSceneGui -= DuringSceneGUI;
            GCommon.UnregisterBeginRender(OnCameraRender);
            GCommon.UnregisterBeginRenderSRP(OnCameraRenderSRP);
            if (previewTextures != null)
            {
                foreach (GStylizedTerrain t in previewTextures.Keys)
                {
                    RenderTexture rt = previewTextures[t];
                    if (rt == null)
                        continue;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
        }

        private void OnUndoRedo()
        {
            if (Selection.activeGameObject != instance.gameObject)
                return;
            if (string.IsNullOrEmpty(GUndoCompatibleBuffer.Instance.CurrentBackupName))
                return;
            GBackup.Restore(GUndoCompatibleBuffer.Instance.CurrentBackupName);
            UpdatePreview();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            instance.GroupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption("Group Id", instance.GroupId);
            instance.EnableTerrainMask = EditorGUILayout.Toggle("Enable Terrain Mask", instance.EnableTerrainMask);
            GEditorSettings.Instance.topographic.enable = EditorGUILayout.Toggle("Enable Topographic", GEditorSettings.Instance.topographic.enable);
            DrawInstructionGUI();
            DrawTransformGUI();
            DrawStampGUI();
            DrawGizmosGUI();
            DrawActionGUI();
#if GRIFFIN_VEGETATION_STUDIO_PRO
            GEditorCommon.DrawVspIntegrationGUI();
#endif
            GEditorCommon.DrawBackupHelpBox();
            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        private void DrawInstructionGUI()
        {
            string label = "Instruction";
            string id = "instruction" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                EditorGUILayout.LabelField(INSTRUCTION, GEditorCommon.WordWrapItalicLabel);
            });
        }

        private void DrawTransformGUI()
        {
            string label = "Transform";
            string id = "transform" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                instance.Position = GEditorCommon.InlineVector3Field("Position", instance.Position);
                instance.Rotation = Quaternion.Euler(GEditorCommon.InlineVector3Field("Rotation", instance.Rotation.eulerAngles));
                instance.Scale = GEditorCommon.InlineVector3Field("Scale", instance.Scale);

                Vector3 euler = instance.Rotation.eulerAngles;
                euler = new Vector3(0, euler.y, 0);
                instance.Rotation = Quaternion.Euler(euler);
            });
        }

        private void DrawStampGUI()
        {
            string label = "Stamp";
            string id = "stamp" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                instance.Stamp = EditorGUILayout.ObjectField("Mask", instance.Stamp, typeof(Texture2D), false) as Texture2D;
                instance.Channel = (GGeometryStamper.GStampChannel)EditorGUILayout.EnumPopup("Channel", instance.Channel);
                EditorGUI.BeginChangeCheck();
                instance.Falloff = EditorGUILayout.CurveField("Falloff", instance.Falloff, Color.red, new Rect(0, 0, 1, 1));
                if (EditorGUI.EndChangeCheck())
                {
                    instance.Internal_UpdateFalloffTexture();
                }

                instance.Operation = (GStampOperation)EditorGUILayout.EnumPopup("Operation", instance.Operation);
                if (instance.Operation == GStampOperation.Lerp)
                {
                    instance.LerpFactor = EditorGUILayout.Slider("Lerp Factor", instance.LerpFactor, 0f, 1f);
                }
                instance.AdditionalMeshResolution = EditorGUILayout.IntSlider("Additional Mesh Resolution", instance.AdditionalMeshResolution, 0, 10);
                instance.InverseStamp = EditorGUILayout.Toggle("Inverse", instance.InverseStamp);
                instance.UseFalloffAsBlendFactor = EditorGUILayout.Toggle("Blend Using Falloff", instance.UseFalloffAsBlendFactor);
            });
        }

        private void DrawGizmosGUI()
        {
            string label = "Gizmos";
            string id = "gizmos" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                instance.Editor_ShowLivePreview = EditorGUILayout.Toggle("Live Preview", instance.Editor_ShowLivePreview);
                instance.Editor_ShowBounds = EditorGUILayout.Toggle("Bounds", instance.Editor_ShowBounds);
            });
        }

        private void DrawActionGUI()
        {
            string label = "Action";
            string id = "action" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Snap To Terrain"))
                {
                    IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
                    while (terrains.MoveNext())
                    {
                        GStylizedTerrain t = terrains.Current;
                        Bounds b = t.Bounds;
                        Rect r = new Rect(new Vector2(b.min.x, b.min.z), new Vector2(b.size.x, b.size.z));
                        Vector2 p = new Vector2(instance.Position.x, instance.Position.z);
                        if (r.Contains(p))
                        {
                            instance.Position = new Vector3(r.center.x, b.min.y, r.center.y);
                            instance.Rotation = Quaternion.identity;
                            instance.Scale = new Vector3(b.size.x, b.size.y, b.size.z);
                            break;
                        }
                    }
                }
                if (GUILayout.Button("Snap To Level Bounds"))
                {
                    Bounds b = GCommon.GetLevelBounds();
                    instance.Position = new Vector3(b.center.x, b.min.y, b.center.z);
                    instance.Rotation = Quaternion.identity;
                    instance.Scale = new Vector3(b.size.x, b.size.y, b.size.z);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Apply"))
                {
                    GAnalytics.Record(GAnalytics.STAMPER_GEOMETRY);
                    CreateInitialBackup();
                    ApplyStamp();
                    CreateAfterStampBackup();
                }
            });
        }

        private void CreateInitialBackup()
        {
            //GBackup.TryCreateInitialBackup(HISTORY_PREFIX, instance.GroupId, GCommon.HeightMapAndFoliageResourceFlags);
            List<GStylizedTerrain> terrains = GStampToolUtilities.CheckOverlap(instance.GroupId, instance.Rect);
            GBackupInternal.TryCreateAndMergeInitialBackup(HISTORY_PREFIX, terrains, GCommon.HeightMapAndFoliageResourceFlags, true);
        }

        private void ApplyStamp()
        {
            EditorUtility.DisplayProgressBar("Applying", "Stamping terrain geometry...", 1f);
            try
            {
                instance.Apply();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
            EditorUtility.ClearProgressBar();
        }

        private void CreateAfterStampBackup()
        {
            //GBackup.TryCreateBackup(HISTORY_PREFIX, instance.GroupId, GCommon.HeightMapAndFoliageResourceFlags);
            List<GStylizedTerrain> terrains = GStampToolUtilities.CheckOverlap(instance.GroupId, instance.Rect);
            GBackupInternal.TryCreateAndMergeBackup(HISTORY_PREFIX, terrains, GCommon.HeightMapAndFoliageResourceFlags, true);
        }

        private void DuringSceneGUI(SceneView sv)
        {
            EditorGUI.BeginChangeCheck();
            HandleEditingTransform();
            DrawStampBounds();
            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
            }
        }

        private void HandleEditingTransform()
        {
            if (Tools.current == Tool.Move)
            {
                instance.Position = Handles.PositionHandle(instance.Position, instance.Rotation);
            }
            else if (Tools.current == Tool.Rotate)
            {
                instance.Rotation = Handles.RotationHandle(instance.Rotation, instance.Position);
                Vector3 euler = instance.Rotation.eulerAngles;
                euler = new Vector3(0, euler.y, 0);
                instance.Rotation = Quaternion.Euler(euler);
            }
            else if (Tools.current == Tool.Scale)
            {
                instance.Scale = Handles.ScaleHandle(instance.Scale, instance.Position, instance.Rotation, HandleUtility.GetHandleSize(instance.Position));
            }
        }

        private void OnCameraRender(Camera cam)
        {
            if (instance.Editor_ShowLivePreview)
                DrawLivePreview(cam);
        }

        private void OnCameraRenderSRP(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
        {
            OnCameraRender(cam);
        }

        private void DrawLivePreview(Camera cam)
        {
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (t.TerrainData == null)
                    continue;
                if (instance.GroupId >= 0 &&
                    instance.GroupId != t.GroupId)
                    continue;
                DrawLivePreview(t, cam);
            }

            if (instance.EnableTerrainMask)
            {
                DrawMask(cam);
            }
        }

        private void DrawMask(Camera cam)
        {
            GCommon.ForEachTerrain(instance.GroupId, (t) =>
            {
                GLivePreviewDrawer.DrawTerrainMask(t, cam);
            });
        }

        private void DrawLivePreview(GStylizedTerrain t, Camera cam)
        {
            if (t.transform.rotation != Quaternion.identity ||
                t.transform.lossyScale != Vector3.one)
                return;

            RenderTexture rt = GetPreviewTexture(t);
            instance.GetQuad(worldPoints);
            for (int i = 0; i < normalizedPoints.Length; ++i)
            {
                normalizedPoints[i] = t.WorldPointToUV(worldPoints[i]);
            }

            Rect dirtyRect = GUtilities.GetRectContainsPoints(normalizedPoints);

            if (instance.Channel == GGeometryStamper.GStampChannel.Elevation)
            {
                GLivePreviewDrawer.DrawGeometryLivePreview(t, cam, rt, dirtyRect);
            }
            else if (instance.Channel == GGeometryStamper.GStampChannel.Visibility)
            {
                Matrix4x4 worldToMaskMatrix = Matrix4x4.TRS(
                worldPoints[0],
                instance.Rotation,
                instance.Scale).inverse;
                GLivePreviewDrawer.DrawVisibilityLivePreview(t, cam, rt, dirtyRect, instance.Stamp, worldToMaskMatrix);
            }
        }

        private void UpdatePreview()
        {
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (t.TerrainData == null)
                    continue;
                if (instance.GroupId >= 0 &&
                    instance.GroupId != t.GroupId)
                    continue;
                UpdatePreview(t);
            }
        }

        private void UpdatePreview(GStylizedTerrain t)
        {
            RenderTexture rt = GetPreviewTexture(t);
            instance.Internal_Apply(t, rt);
        }

        private RenderTexture GetPreviewTexture(GStylizedTerrain t)
        {
            if (previewTextures == null)
            {
                previewTextures = new Dictionary<GStylizedTerrain, RenderTexture>();
            }

            int resolution = t.TerrainData.Geometry.HeightMapResolution;
            if (!previewTextures.ContainsKey(t) ||
                previewTextures[t] == null)
            {
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, GGeometry.HeightMapRTFormat, RenderTextureReadWrite.Linear);
                previewTextures[t] = rt;
            }
            else if (previewTextures[t].width != resolution ||
                previewTextures[t].height != resolution ||
                previewTextures[t].format != GGeometry.HeightMapRTFormat)
            {
                previewTextures[t].Release();
                Object.DestroyImmediate(previewTextures[t]);
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, GGeometry.HeightMapRTFormat, RenderTextureReadWrite.Linear);
                previewTextures[t] = rt;
            }

            previewTextures[t].wrapMode = TextureWrapMode.Clamp;

            return previewTextures[t];
        }

        private void DrawStampBounds()
        {
            if (!instance.Editor_ShowBounds)
                return;
            instance.GetBox(worldBox);

            Vector3[] b = worldBox;
            Handles.color = Color.yellow;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawLines(new Vector3[]
            {
                //bottom quad
                b[0], b[1],
                b[1], b[2],
                b[2], b[3],
                b[3], b[0],
                //top quad
                b[4], b[5],
                b[5], b[6],
                b[6], b[7],
                b[7], b[4],
                //vertical lines
                b[0], b[4],
                b[1], b[5],
                b[2], b[6],
                b[3], b[7]
            });
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }
    }
}
