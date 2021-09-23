using Pinwheel.Griffin.BackupTool;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [CustomEditor(typeof(GPathPainter))]
    public class GPathPainterInspector : Editor
    {
        private GPathPainter instance;
        private Dictionary<string, RenderTexture> previewTextures;
        private MaterialPropertyBlock previewPropertyBlock;

        private static readonly string HISTORY_PREFIX_ALBEDO_METALLIC = "Make Path Albedo Metallic";
        private static readonly string HISTORY_PREFIX_SPLAT = "Make Path Splat";

        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            instance = (GPathPainter)target;
            instance.Internal_UpdateFalloffTexture();
            previewPropertyBlock = new MaterialPropertyBlock();
            GCommon.RegisterBeginRender(OnCameraRender);
            GCommon.RegisterBeginRenderSRP(OnCameraRenderSRP);
            GCommon.UpdateMaterials(instance.SplineCreator.GroupId);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            GCommon.UnregisterBeginRender(OnCameraRender);
            GCommon.UnregisterBeginRenderSRP(OnCameraRenderSRP);

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

        private void OnUndoRedo()
        {
            if (Selection.activeGameObject != instance.gameObject)
                return;
            if (string.IsNullOrEmpty(GUndoCompatibleBuffer.Instance.CurrentBackupName))
                return;
            GBackup.Restore(GUndoCompatibleBuffer.Instance.CurrentBackupName);
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

        public override void OnInspectorGUI()
        {
            instance.SplineCreator = EditorGUILayout.ObjectField("Spline Creator", instance.SplineCreator, typeof(GSplineCreator), true) as GSplineCreator;
            if (instance.SplineCreator == null)
                return;
            instance.Channel = (GPathPainter.PaintChannel)EditorGUILayout.EnumPopup("Channel", instance.Channel);
            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            EditorGUI.BeginChangeCheck();
            instance.Falloff = EditorGUILayout.CurveField("Falloff", instance.Falloff, Color.red, new Rect(0, 0, 1, 1));
            if (EditorGUI.EndChangeCheck())
            {
                instance.Internal_UpdateFalloffTexture();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Falloff Noise");
            instance.FalloffNoise = EditorGUILayout.ObjectField(instance.FalloffNoise, typeof(Texture2D), false) as Texture2D;
            EditorGUILayout.EndHorizontal();
            if (instance.FalloffNoise != null)
                instance.FalloffNoiseSize = EditorGUILayout.Vector2Field("Falloff Noise Size", instance.FalloffNoiseSize);
            EditorGUIUtility.wideMode = wideMode;

            if (instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic)
            {
                DrawAlbedoAndMetallicGUI();
            }
            else if (instance.Channel == GPathPainter.PaintChannel.Splat)
            {
                DrawSplatGUI();
            }
            instance.Editor_ShowLivePreview = EditorGUILayout.Toggle("Live Preview", instance.Editor_ShowLivePreview);
            if (GUILayout.Button("Apply"))
            {
                GAnalytics.Record(GAnalytics.SPLINE_PATH_PAINTER);
                CreateInitialBackup();
                ApplyPath();
                CreateBackupAfterApplyPath();
            }
        }

        private void CreateInitialBackup()
        {
            string historyPrefix =
                instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic ? HISTORY_PREFIX_ALBEDO_METALLIC :
                instance.Channel == GPathPainter.PaintChannel.Splat ? HISTORY_PREFIX_SPLAT : "Unknown Action";
            List<GTerrainResourceFlag> resourceFlag = new List<GTerrainResourceFlag>();
            if (instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic)
            {
                resourceFlag.Add(GTerrainResourceFlag.AlbedoMap);
                resourceFlag.Add(GTerrainResourceFlag.MetallicMap);
            }
            else if (instance.Channel == GPathPainter.PaintChannel.Splat)
            {
                resourceFlag.Add(GTerrainResourceFlag.SplatControlMaps);
            }

            //GBackup.TryCreateInitialBackup(historyPrefix, instance.SplineCreator.GroupId, resourceFlag);
            List<GStylizedTerrain> terrains = GSplineToolUtilities.OverlapTest(instance.SplineCreator.GroupId, instance.SplineCreator);
            GBackupInternal.TryCreateAndMergeInitialBackup(historyPrefix, terrains, resourceFlag, true);
        }

        private void ApplyPath()
        {
            EditorUtility.DisplayProgressBar("Applying", "Creating path...", 1f);
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

        private void CreateBackupAfterApplyPath()
        {
            string historyPrefix =
                instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic ? HISTORY_PREFIX_ALBEDO_METALLIC :
                instance.Channel == GPathPainter.PaintChannel.Splat ? HISTORY_PREFIX_SPLAT : "Unknown Action";
            List<GTerrainResourceFlag> resourceFlag = new List<GTerrainResourceFlag>();
            if (instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic)
            {
                resourceFlag.Add(GTerrainResourceFlag.AlbedoMap);
                resourceFlag.Add(GTerrainResourceFlag.MetallicMap);
            }
            else if (instance.Channel == GPathPainter.PaintChannel.Splat)
            {
                resourceFlag.Add(GTerrainResourceFlag.SplatControlMaps);
            }

            //GBackup.TryCreateBackup(historyPrefix, instance.SplineCreator.GroupId, resourceFlag);
            List<GStylizedTerrain> terrains = GSplineToolUtilities.OverlapTest(instance.SplineCreator.GroupId, instance.SplineCreator);
            GBackupInternal.TryCreateAndMergeBackup(historyPrefix, terrains, resourceFlag, true);
        }

        private void DrawAlbedoAndMetallicGUI()
        {
            instance.Color = EditorGUILayout.ColorField("Color", instance.Color);
            instance.Metallic = EditorGUILayout.Slider("Metallic", instance.Metallic, 0f, 1f);
            instance.Smoothness = EditorGUILayout.Slider("Smoothness", instance.Smoothness, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Use a material that utilizes Albedo & Metallic map to see the result!", GEditorCommon.WordWrapItalicLabel);
        }

        private void DrawSplatGUI()
        {
            instance.SplatIndex = GEditorCommon.SplatSetSelectionGrid(instance.SplineCreator.GroupId, instance.SplatIndex);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Use a material that utilizes Splat map to see the result!", GEditorCommon.WordWrapItalicLabel);
        }

        private void DrawLivePreview(Camera cam)
        {
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (t.TerrainData == null)
                    continue;
                if (instance.SplineCreator.GroupId >= 0 &&
                    instance.SplineCreator.GroupId != t.GroupId)
                    continue;
                DrawLivePreview(t, cam);
            }
        }

        private void DrawLivePreview(GStylizedTerrain t, Camera cam)
        {
            List<Rect> dirtyRects = new List<Rect>(instance.SplineCreator.SweepDirtyRect(t));
            Rect r = new Rect(0, 0, 0, 0);
            for (int i = 0; i < dirtyRects.Count; ++i)
            {
                r.xMin = Mathf.Min(r.xMin, dirtyRects[i].xMin);
                r.xMax = Mathf.Max(r.xMax, dirtyRects[i].xMax);
                r.yMin = Mathf.Min(r.yMin, dirtyRects[i].yMin);
                r.yMax = Mathf.Max(r.yMax, dirtyRects[i].yMax);
            }

            if (instance.Channel == GPathPainter.PaintChannel.AlbedoAndMetallic)
            {
                SetupAlbedoMetallicPreview(t, cam, r);
            }
            else if (instance.Channel == GPathPainter.PaintChannel.Splat)
            {
                SetupSplatPreview(t, cam, r);
            }
        }

        private void SetupAlbedoMetallicPreview(GStylizedTerrain t, Camera cam, Rect dirtyRect)
        {
            Material mat = t.TerrainData.Shading.MaterialToRender;
            if (mat == null)
                return;
            int albedoResolution = t.TerrainData.Shading.AlbedoMapResolution;
            FilterMode albedoFilter = t.TerrainData.Shading.AlbedoMapOrDefault.filterMode;
            RenderTexture rtAlbedo = GetPreviewTexture(t, "albedo", albedoResolution, albedoFilter);
            instance.Internal_ApplyAlbedo(t, instance.SplineCreator.Editor_Vertices, rtAlbedo);

            int metallicResolution = t.TerrainData.Shading.MetallicMapResolution;
            FilterMode metallicFilter = t.TerrainData.Shading.MetallicMapOrDefault.filterMode;
            RenderTexture rtMetallic = GetPreviewTexture(t, "metallic", metallicResolution, metallicFilter);
            instance.Internal_ApplyMetallic(t, instance.SplineCreator.Editor_Vertices, rtMetallic);

            GLivePreviewDrawer.DrawAMSLivePreview(t, cam, rtAlbedo, rtMetallic, dirtyRect);
        }

        private void SetupSplatPreview(GStylizedTerrain t, Camera cam, Rect dirtyRect)
        {
            Material mat = t.TerrainData.Shading.MaterialToRender;
            if (mat == null)
                return;
            int controlMapResolution = t.TerrainData.Shading.SplatControlResolution;
            int controlMapCount = t.TerrainData.Shading.SplatControlMapCount;
            if (controlMapCount == 0)
                return;
            RenderTexture[] rtControls = new RenderTexture[controlMapCount];
            for (int i = 0; i < controlMapCount; ++i)
            {
                rtControls[i] = GetPreviewTexture(t, "splatControl" + i, controlMapResolution, t.TerrainData.Shading.GetSplatControlOrDefault(i).filterMode);
            }
            instance.Internal_ApplySplat(t, instance.SplineCreator.Editor_Vertices, rtControls);

            GLivePreviewDrawer.DrawSplatLivePreview(t, cam, rtControls, dirtyRect);
        }

        private void DrawTerrainMeshPreview(GStylizedTerrain t, Camera cam)
        {
            Material mat = t.TerrainData.Shading.MaterialToRender;
            if (mat == null)
                return;

            GTerrainChunk[] chunks = t.GetChunks();
            List<Rect> dirtyRects = new List<Rect>(instance.SplineCreator.SweepDirtyRect(t));
            int chunkGridSize = t.TerrainData.Geometry.ChunkGridSize;
            for (int i = 0; i < chunks.Length; ++i)
            {
                Rect uvRange = chunks[i].GetUvRange();
                for (int r = 0; r < dirtyRects.Count; ++r)
                {
                    if (uvRange.Overlaps(dirtyRects[r]))
                    {
                        Vector3Int key = GTerrainChunk.GetChunkMeshKey(chunks[i].Index, 0);
                        Mesh chunkMesh = t.TerrainData.GeometryData.GetMesh(key);
                        if (chunkMesh != null)
                        {
                            Graphics.DrawMesh(
                                chunkMesh,
                                chunks[i].transform.localToWorldMatrix,
                                mat,
                                chunks[i].gameObject.layer,
                                cam,
                                0,
                                previewPropertyBlock,
                                t.TerrainData.Rendering.CastShadow,
                                t.TerrainData.Rendering.ReceiveShadow);
                        }
                        break;
                    }
                }
            }
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
    }
}
