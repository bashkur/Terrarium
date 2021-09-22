using Pinwheel.Griffin.BackupTool;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [CustomEditor(typeof(GFoliageRemover))]
    public class GFoliageRemoverInspector : Editor
    {
        private GFoliageRemover instance;
        private Dictionary<GStylizedTerrain, RenderTexture> previewTextures;
        private MaterialPropertyBlock previewPropertyBlock;

        private const string HISTORY_PREFIX = "Remove Foliage Along Path";

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            instance = target as GFoliageRemover;
            instance.Internal_UpdateFalloffTexture();
            previewPropertyBlock = new MaterialPropertyBlock();

            GCommon.RegisterBeginRender(OnCameraRender);
            GCommon.RegisterBeginRenderSRP(OnCameraRenderSRP);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
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
        }

        public override void OnInspectorGUI()
        {
            instance.SplineCreator = EditorGUILayout.ObjectField("Spline Creator", instance.SplineCreator, typeof(GSplineCreator), true) as GSplineCreator;
            if (instance.SplineCreator == null)
                return;
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

            instance.MaskResolution = EditorGUILayout.DelayedIntField("Mask Resolution", instance.MaskResolution);

            instance.RemoveTrees = EditorGUILayout.Toggle("Remove Trees", instance.RemoveTrees);
            if (instance.RemoveTrees)
            {
                EditorGUI.indentLevel += 1;
                instance.TreePrototypeIndices = GEditorCommon.TreeSetMultiSelectionGrid(instance.SplineCreator.GroupId, instance.TreePrototypeIndices);
                EditorGUI.indentLevel -= 1;
            }

            instance.RemoveGrasses = EditorGUILayout.Toggle("Remove Grasses", instance.RemoveGrasses);
            if (instance.RemoveGrasses)
            {
                EditorGUI.indentLevel += 1;
                instance.GrassPrototypeIndices = GEditorCommon.GrassSetMultiSelectionGrid(instance.SplineCreator.GroupId, instance.GrassPrototypeIndices);
                EditorGUI.indentLevel -= 1;
            }

            instance.Editor_ShowLivePreview = EditorGUILayout.Toggle("Live Preview", instance.Editor_ShowLivePreview);
            EditorGUIUtility.wideMode = wideMode;

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply"))
            {
                GAnalytics.Record(GAnalytics.SPLINE_FOLIAGE_REMOVER);
                CreateInitialBackup();
                Apply();
                CreateBackupAfterApply();
            }
        }

        private void CreateInitialBackup()
        {
            //GBackup.TryCreateInitialBackup(HISTORY_PREFIX, instance.SplineCreator.GroupId, GCommon.FoliageInstancesResourceFlags);
            List<GStylizedTerrain> terrains = GSplineToolUtilities.OverlapTest(instance.SplineCreator.GroupId, instance.SplineCreator);
            GBackupInternal.TryCreateAndMergeInitialBackup(HISTORY_PREFIX, terrains, GCommon.FoliageInstancesResourceFlags, true);
        }

        private void Apply()
        {
            EditorUtility.DisplayProgressBar("Applying", "Removing foliage...", 1f);
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

        private void CreateBackupAfterApply()
        {
            //GBackup.TryCreateBackup(HISTORY_PREFIX, instance.SplineCreator.GroupId, GCommon.FoliageInstancesResourceFlags);
            List<GStylizedTerrain> terrains = GSplineToolUtilities.OverlapTest(instance.SplineCreator.GroupId, instance.SplineCreator);
            GBackupInternal.TryCreateAndMergeBackup(HISTORY_PREFIX, terrains, GCommon.FoliageInstancesResourceFlags, true);
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
                if (instance.SplineCreator.GroupId >= 0 &&
                    instance.SplineCreator.GroupId != t.GroupId)
                    continue;
                DrawLivePreview(t, cam);
            }
        }

        private void DrawLivePreview(GStylizedTerrain t, Camera cam)
        {
            if (t.transform.rotation != Quaternion.identity ||
                t.transform.lossyScale != Vector3.one)
                return;

            List<Rect> dirtyRects = new List<Rect>(instance.SplineCreator.SweepDirtyRect(t));
            Rect r = new Rect(0, 0, 0, 0);
            for (int i = 0; i < dirtyRects.Count; ++i)
            {
                r.xMin = Mathf.Min(r.xMin, dirtyRects[i].xMin);
                r.xMax = Mathf.Max(r.xMax, dirtyRects[i].xMax);
                r.yMin = Mathf.Min(r.yMin, dirtyRects[i].yMin);
                r.yMax = Mathf.Max(r.yMax, dirtyRects[i].yMax);
            }

            RenderTexture rt = GetPreviewTexture(t);
            instance.Internal_Apply(t, rt, instance.SplineCreator.Editor_Vertices);
            previewPropertyBlock.Clear();

            GLivePreviewDrawer.DrawMasksLivePreview(
                t, cam,
                new Texture[] { rt },
                new Color[] { GEditorSettings.Instance.splineTools.negativeHighlightColor },
                r);
        }

        private RenderTexture GetPreviewTexture(GStylizedTerrain t)
        {
            if (previewTextures == null)
            {
                previewTextures = new Dictionary<GStylizedTerrain, RenderTexture>();
            }

            int resolution = instance.MaskResolution;
            if (!previewTextures.ContainsKey(t) ||
                previewTextures[t] == null)
            {
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                previewTextures[t] = rt;
            }
            else if (previewTextures[t].width != resolution || previewTextures[t].height != resolution)
            {
                previewTextures[t].Release();
                Object.DestroyImmediate(previewTextures[t]);
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                previewTextures[t] = rt;
            }

            previewTextures[t].wrapMode = TextureWrapMode.Clamp;
            return previewTextures[t];
        }
    }
}
