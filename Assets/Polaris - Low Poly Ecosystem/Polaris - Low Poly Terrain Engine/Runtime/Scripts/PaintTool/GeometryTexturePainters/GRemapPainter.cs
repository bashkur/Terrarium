using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Griffin.PaintTool
{
    public class GRemapPainter : IGTexturePainter, IGTexturePainterWithCustomParams, IGTexturePainterWithLivePreview
    {
        public string HistoryPrefix
        {
            get
            {
                return "Remap Painting";
            }
        }

        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Remap evelation value by a curve.\n" +
                    "   - Use Left Mouse to paint.");
                return s;
            }
        }

        public void Paint(Pinwheel.Griffin.GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (terrain.TerrainData == null)
                return;
            if (args.MouseEventType == GPainterMouseEventType.Down)
            {
                terrain.ForceLOD(0);
                GRuntimeSettings.Instance.isEditingGeometry = true;
            }
            if (args.MouseEventType == GPainterMouseEventType.Up)
            {
                terrain.ForceLOD(-1);
                GRuntimeSettings.Instance.isEditingGeometry = false;
                terrain.UpdateTreesPosition();
                terrain.UpdateGrassPatches();
                terrain.TerrainData.Foliage.ClearTreeDirtyRegions();
                terrain.TerrainData.Foliage.ClearGrassDirtyRegions();
                return;
            }

            Vector2[] uvCorners = new Vector2[args.WorldPointCorners.Length];
            for (int i = 0; i < uvCorners.Length; ++i)
            {
                uvCorners[i] = terrain.WorldPointToUV(args.WorldPointCorners[i]);
            }

            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvCorners);
            if (!dirtyRect.Overlaps(new Rect(0, 0, 1, 1)))
                return;

            Texture2D bg = terrain.TerrainData.Geometry.HeightMap;
            int heightMapResolution = terrain.TerrainData.Geometry.HeightMapResolution;
            RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(heightMapResolution);
            GCommon.CopyToRT(bg, rt);

            Texture2D remapTex = GCommon.CreateTextureFromCurve(GTexturePainterCustomParams.Instance.Remap.Curve, 512, 1);
            Material mat = GInternalMaterials.RemapPainterMaterial;
            mat.SetTexture("_MainTex", bg);
            mat.SetTexture("_Mask", args.BrushMask);
            mat.SetFloat("_Opacity", Mathf.Pow(args.Opacity, GTerrainTexturePainter.GEOMETRY_OPACITY_EXPONENT));
            mat.SetTexture("_RemapTex", remapTex);
            if (args.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", terrain.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            int pass = 0;
            GCommon.DrawQuad(rt, uvCorners, mat, pass);

            RenderTexture.active = rt;
            terrain.TerrainData.Geometry.HeightMap.ReadPixels(
                new Rect(0, 0, heightMapResolution, heightMapResolution), 0, 0);
            terrain.TerrainData.Geometry.HeightMap.Apply();
            RenderTexture.active = null;

            GUtilities.DestroyObject(remapTex);

            terrain.TerrainData.Geometry.SetRegionDirty(dirtyRect);
            terrain.TerrainData.Foliage.SetTreeRegionDirty(dirtyRect);
            terrain.TerrainData.Foliage.SetGrassRegionDirty(dirtyRect);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);
        }

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return GCommon.HeightMapAndFoliageResourceFlags;
        }

        public void Editor_DrawCustomParamsGUI()
        {
#if UNITY_EDITOR
            string label = "Remap Painting";
            string id = "remap-painter";

            GCommonGUI.Foldout(label, true, id, () =>
            {
                GRemapPainterParams param = GTexturePainterCustomParams.Instance.Remap;
                param.Curve = EditorGUILayout.CurveField("Curve", param.Curve, Color.red, GCommon.UnitRect);
                GTexturePainterCustomParams.Instance.Remap = param;
                EditorUtility.SetDirty(GTexturePainterCustomParams.Instance);
            });
#endif
        }

        public void Editor_DrawLivePreview(GStylizedTerrain terrain, GTexturePainterArgs args, Camera cam)
        {
#if UNITY_EDITOR
            Vector2[] uvCorners = new Vector2[args.WorldPointCorners.Length];
            for (int i = 0; i < uvCorners.Length; ++i)
            {
                uvCorners[i] = terrain.WorldPointToUV(args.WorldPointCorners[i]);
            }

            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvCorners);
            if (!dirtyRect.Overlaps(new Rect(0, 0, 1, 1)))
                return;

            Texture2D bg = terrain.TerrainData.Geometry.HeightMap;
            int heightMapResolution = terrain.TerrainData.Geometry.HeightMapResolution;
            RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(terrain, heightMapResolution);
            GCommon.CopyToRT(bg, rt);

            Texture2D remapTex = GCommon.CreateTextureFromCurve(GTexturePainterCustomParams.Instance.Remap.Curve, 512, 1);
            Material mat = GInternalMaterials.RemapPainterMaterial;
            mat.SetTexture("_MainTex", bg);
            mat.SetTexture("_Mask", args.BrushMask);
            mat.SetFloat("_Opacity", Mathf.Pow(args.Opacity, GTerrainTexturePainter.GEOMETRY_OPACITY_EXPONENT));
            mat.SetTexture("_RemapTex", remapTex); 
            if (args.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", terrain.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            int pass = 0;
            GCommon.DrawQuad(rt, uvCorners, mat, pass);

            GLivePreviewDrawer.DrawGeometryLivePreview(terrain, cam, rt, dirtyRect);

            GUtilities.DestroyObject(remapTex);
#endif
        }
    }
}
