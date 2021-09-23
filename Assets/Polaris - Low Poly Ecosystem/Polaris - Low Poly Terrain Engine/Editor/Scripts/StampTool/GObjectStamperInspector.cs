using Pinwheel.Griffin.TextureTool;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Griffin.StampTool
{
    [CustomEditor(typeof(GObjectStamper))]
    public class GObjectStamperInspector : Editor
    {
        private GObjectStamper instance;
        private Dictionary<string, RenderTexture> previewTextures;
        private MaterialPropertyBlock previewPropertyBlock;

        private readonly Vector3[] worldBox = new Vector3[8];

        private const string INSTRUCTION = "Stamp objects onto the terrain surface.";

        private void OnEnable()
        {
            instance = target as GObjectStamper;
            Tools.hidden = true;

            instance.Internal_UpdateFalloffTexture();
            instance.Internal_UpdateLayerTransitionTextures();
            previewPropertyBlock = new MaterialPropertyBlock();
            GCommon.RegisterBeginRender(OnCameraRender);
            GCommon.RegisterBeginRenderSRP(OnCameraRenderSRP);
            SceneView.duringSceneGui += DuringSceneGUI;
            UpdatePreview();
        }

        private void OnDisable()
        {
            Tools.hidden = false;

            GCommon.UnregisterBeginRender(OnCameraRender);
            GCommon.UnregisterBeginRenderSRP(OnCameraRenderSRP);
            SceneView.duringSceneGui -= DuringSceneGUI;

            if (previewTextures != null)
            {
                foreach (string k in previewTextures.Keys)
                {
                    RenderTexture rt = previewTextures[k];
                    if (rt == null)
                        continue;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            instance.GroupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption("Group Id", instance.GroupId);
            instance.EnableTerrainMask = EditorGUILayout.Toggle("Enable Terrain Mask", instance.EnableTerrainMask);
            DrawInstructionGUI();
            DrawTransformGUI();
            DrawStampGUI();
            DrawStampLayersGUI();
            DrawGizmosGUI();
            DrawActionGUI();
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
                instance.Mask = EditorGUILayout.ObjectField("Mask", instance.Mask, typeof(Texture2D), false) as Texture2D;

                EditorGUI.BeginChangeCheck();
                instance.Falloff = EditorGUILayout.CurveField("Falloff", instance.Falloff, Color.red, new Rect(0, 0, 1, 1));
                if (EditorGUI.EndChangeCheck())
                {
                    instance.Internal_UpdateFalloffTexture();
                }
                instance.MaskResolution = EditorGUILayout.DelayedIntField("Mask Resolution", instance.MaskResolution);
            });
        }

        private void DrawStampLayersGUI()
        {
            string label = "Layers";
            string id = "layers" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                List<GObjectStampLayer> layers = instance.Layers;
                for (int i = 0; i < layers.Count; ++i)
                {
                    DrawLayer(layers[i], i);
                }

                if (layers.Count > 0)
                {
                    GEditorCommon.Separator();
                }
                if (GUILayout.Button("Add Layer"))
                {
                    GObjectStampLayer layer = GObjectStampLayer.Create();
                    layers.Add(layer);
                }
            });
        }

        private void DrawLayer(GObjectStampLayer layer, int index)
        {
            string label = string.Format("Layer: {0} {1}",
                !string.IsNullOrEmpty(layer.Name) ? layer.Name : index.ToString(),
                layer.Ignore ? "[Ignored]" : string.Empty);
            string id = "stamperlayer" + index + instance.GetInstanceID().ToString();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Remove"),
                false,
                () =>
                {
                    ConfirmAndRemoveLayerAt(index);
                });

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUI.indentLevel -= 1;
                layer.Ignore = EditorGUILayout.Toggle("Ignore", layer.Ignore);
                layer.Name = EditorGUILayout.TextField("Name", layer.Name);
                layer.VisualizeColor = EditorGUILayout.ColorField("Visualize Color", layer.VisualizeColor);
                layer.MinRotation = EditorGUILayout.FloatField("Min Rotation", layer.MinRotation);
                layer.MaxRotation = EditorGUILayout.FloatField("Max Rotation", layer.MaxRotation);
                layer.MinScale = GEditorCommon.InlineVector3Field("Min Scale", layer.MinScale);
                layer.MaxScale = GEditorCommon.InlineVector3Field("Max Scale", layer.MaxScale);
                layer.InstanceCount = EditorGUILayout.IntField("Instance Count", layer.InstanceCount);
                DrawObjectSelectorGUI(layer);

                layer.BlendHeight = EditorGUILayout.Toggle("Blend Height", layer.BlendHeight);
                if (layer.BlendHeight)
                {
                    EditorGUI.indentLevel += 1;
                    layer.MinHeight = EditorGUILayout.FloatField("Min", layer.MinHeight);
                    layer.MaxHeight = EditorGUILayout.FloatField("Max", layer.MaxHeight);
                    EditorGUI.BeginChangeCheck();
                    layer.HeightTransition = EditorGUILayout.CurveField("Transition", layer.HeightTransition, Color.red, new Rect(0, 0, 1, 1));
                    if (EditorGUI.EndChangeCheck())
                    {
                        layer.UpdateCurveTextures();
                    }
                    EditorGUI.indentLevel -= 1;
                }

                layer.BlendSlope = EditorGUILayout.Toggle("Blend Slope", layer.BlendSlope);
                if (layer.BlendSlope)
                {
                    EditorGUI.indentLevel += 1;
                    layer.NormalMapMode = (GNormalMapMode)EditorGUILayout.EnumPopup("Mode", layer.NormalMapMode);
                    layer.MinSlope = EditorGUILayout.FloatField("Min", layer.MinSlope);
                    layer.MaxSlope = EditorGUILayout.FloatField("Max", layer.MaxSlope);
                    EditorGUI.BeginChangeCheck();
                    layer.SlopeTransition = EditorGUILayout.CurveField("Transition", layer.SlopeTransition, Color.red, new Rect(0, 0, 1, 1));
                    if (EditorGUI.EndChangeCheck())
                    {
                        layer.UpdateCurveTextures();
                    }
                    EditorGUI.indentLevel -= 1;
                }

                layer.BlendNoise = EditorGUILayout.Toggle("Blend Noise", layer.BlendNoise);
                if (layer.BlendNoise)
                {
                    EditorGUI.indentLevel += 1;
                    layer.NoiseOrigin = GEditorCommon.InlineVector2Field("Origin", layer.NoiseOrigin);
                    layer.NoiseFrequency = EditorGUILayout.FloatField("Frequency", layer.NoiseFrequency);
                    layer.NoiseLacunarity = EditorGUILayout.FloatField("Lacunarity", layer.NoiseLacunarity);
                    layer.NoisePersistence = EditorGUILayout.Slider("Persistence", layer.NoisePersistence, 0.01f, 1f);
                    layer.NoiseOctaves = EditorGUILayout.IntSlider("Octaves", layer.NoiseOctaves, 1, 4);
                    EditorGUI.BeginChangeCheck();
                    layer.NoiseRemap = EditorGUILayout.CurveField("Remap", layer.NoiseRemap, Color.red, new Rect(0, 0, 1, 1));
                    if (EditorGUI.EndChangeCheck())
                    {
                        layer.UpdateCurveTextures();
                    }
                    EditorGUI.indentLevel -= 1;
                }
                layer.AlignToSurface = EditorGUILayout.Toggle("Align To Surface", layer.AlignToSurface);
                EditorGUI.indentLevel += 1;
            },
            menu);
        }

        private void DrawObjectSelectorGUI(GObjectStampLayer layer)
        {
            if (layer.Prototypes.Count > 0)
            {
                GSelectionGridArgs args = new GSelectionGridArgs();
                args.collection = layer.Prototypes;
                args.selectedIndices = layer.PrototypeIndices;
                args.itemSize = GEditorCommon.selectionGridTileSizeSmall;
                args.itemPerRow = 4;
                args.drawPreviewFunction = GEditorCommon.DrawGameObjectPreview;
                args.contextClickFunction = OnObjectSelectorContextClick;
                layer.PrototypeIndices = GEditorCommon.MultiSelectionGrid(args);
            }
            else
            {
                EditorGUILayout.LabelField("No Game Object found!", GEditorCommon.WordWrapItalicLabel);
            }

            Rect r1 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
            GameObject prefab = GEditorCommon.ObjectSelectorDragDrop<GameObject>(r1, "Drop a Game Object here!", "t:GameObject");
            if (prefab != null)
            {
                layer.Prototypes.AddIfNotContains(prefab);
            }

            GEditorCommon.SpacePixel(0);
            Rect r2 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
            GPrefabPrototypeGroup group = GEditorCommon.ObjectSelectorDragDrop<GPrefabPrototypeGroup>(r2, "Drop a Prefab Prototype Group here!", "t:GPrefabPrototypeGroup");
            if (group != null)
            {
                layer.Prototypes.AddIfNotContains(group.Prototypes);
            }
        }

        private void OnObjectSelectorContextClick(Rect r, int index, ICollection collection)
        {
            List<GameObject> prototypes = (List<GameObject>)collection;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Remove"),
                false,
                () =>
                {
                    prototypes.RemoveAt(index);
                });

            menu.ShowAsContext();
        }

        private void ConfirmAndRemoveLayerAt(int index)
        {
            GObjectStampLayer layer = instance.Layers[index];
            string layerName = string.IsNullOrEmpty(layer.Name) ? ("Layer " + index) : ("Layer " + layer.Name);
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove " + layerName + "?",
                "Yes", "Cancel"))
            {
                instance.Layers.RemoveAt(index);
            }
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
            string label = "Actions";
            string id = "actions" + instance.GetInstanceID().ToString();
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

                if (GUILayout.Button("Clear Objects"))
                {
                    instance.ClearObjects();
                    Event.current.Use();
                }

                if (GUILayout.Button("Apply"))
                {
                    GAnalytics.Record(GAnalytics.STAMPER_OBJECT);
                    ApplyStamp();
                    EditorGUIUtility.ExitGUI();
                }
            });
        }

        private void ApplyStamp()
        {
            EditorUtility.DisplayProgressBar("Applying", "Stamping foliage...", 1f);
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
            if (instance.EnableTerrainMask)
                DrawTerrainMask(cam);
        }

        private void DrawTerrainMask(Camera cam)
        {
            GCommon.ForEachTerrain(instance.GroupId, (t) =>
            {
                GLivePreviewDrawer.DrawTerrainMask(t, cam);
            });
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
        }

        private void DrawLivePreview(GStylizedTerrain t, Camera cam)
        {
            if (t.transform.rotation != Quaternion.identity ||
                t.transform.lossyScale != Vector3.one)
                return;
            RenderTexture[] brushes = new RenderTexture[instance.Layers.Count];
            for (int i = 0; i < brushes.Length; ++i)
            {
                brushes[i] = GetPreviewTexture(t, "brush" + i.ToString(), instance.MaskResolution, FilterMode.Bilinear);
            }

            Vector3[] worldPoints = instance.GetQuad();
            Vector2[] uvPoint = new Vector2[worldPoints.Length];
            for (int i = 0; i < uvPoint.Length; ++i)
            {
                uvPoint[i] = t.WorldPointToUV(worldPoints[i]);
            }
            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvPoint);
            if (!dirtyRect.Overlaps(new Rect(0, 0, 1, 1)))
                return;

            Color[] colors = new Color[instance.Layers.Count];
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = instance.Layers[i].VisualizeColor;
            }

            GLivePreviewDrawer.DrawMasksLivePreview(t, cam, brushes, colors, dirtyRect);
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
            RenderTexture[] brushes = new RenderTexture[instance.Layers.Count];
            for (int i = 0; i < brushes.Length; ++i)
            {
                brushes[i] = GetPreviewTexture(t, "brush" + i.ToString(), instance.MaskResolution, FilterMode.Bilinear);
            }

            Vector3[] worldPoints = instance.GetQuad();
            Vector2[] uvPoint = new Vector2[worldPoints.Length];
            for (int i = 0; i < uvPoint.Length; ++i)
            {
                uvPoint[i] = t.WorldPointToUV(worldPoints[i]);
            }
            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvPoint);
            if (!dirtyRect.Overlaps(new Rect(0, 0, 1, 1)))
                return;
            instance.Internal_RenderBrushes(brushes, t, uvPoint);
        }

        private RenderTexture GetPreviewTexture(GStylizedTerrain t, string mapName, int resolution, FilterMode filter)
        {
            if (previewTextures == null)
            {
                previewTextures = new Dictionary<string, RenderTexture>();
            }

            string key = string.Format("{0}_{1}", t.GetInstanceID(), mapName);
            if (!previewTextures.ContainsKey(key) ||
                previewTextures[key] == null)
            {
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.wrapMode = TextureWrapMode.Clamp;
                previewTextures[key] = rt;
            }
            else if (previewTextures[key].width != resolution || previewTextures[key].height != resolution)
            {
                previewTextures[key].Release();
                Object.DestroyImmediate(previewTextures[key]);
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.wrapMode = TextureWrapMode.Clamp;
                previewTextures[key] = rt;
            }

            previewTextures[key].filterMode = filter;
            return previewTextures[key];
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
