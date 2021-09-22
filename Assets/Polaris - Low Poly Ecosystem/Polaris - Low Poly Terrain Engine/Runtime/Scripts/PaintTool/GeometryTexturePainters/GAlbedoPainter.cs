using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.PaintTool
{
    public class GAlbedoPainter : IGTexturePainter, IGTexturePainterWithLivePreview
    {
        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Modify terrain Albedo Map color.\n" +
                    "   - Hold Left Mouse to paint.\n" +
                    "   - Hold Ctrl & Left Mouse to erase.\n" +
                    "   - Hold Shift & Left Mouse to smooth.\n" +
                    "Use a material that utilizes Albedo Map to see the result.");
                return s;
            }
        }

        public string HistoryPrefix
        {
            get
            {
                return "Albedo Painting";
            }
        }

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return GCommon.AlbedoResourceFlags;
        }

        private void SetupTextureGrid(GStylizedTerrain t, Material mat)
        {
            mat.SetTexture("_MainTex_Left",
                t.LeftNeighbor && t.LeftNeighbor.TerrainData ?
                t.LeftNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_TopLeft",
                t.LeftNeighbor && t.LeftNeighbor.TopNeighbor && t.LeftNeighbor.TopNeighbor.TerrainData ?
                t.LeftNeighbor.TopNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_TopLeft",
                t.TopNeighbor && t.TopNeighbor.LeftNeighbor && t.TopNeighbor.LeftNeighbor.TerrainData ?
                t.TopNeighbor.LeftNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Top",
                t.TopNeighbor && t.TopNeighbor.TerrainData ?
                t.TopNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_TopRight",
                t.RightNeighbor && t.RightNeighbor.TopNeighbor && t.RightNeighbor.TopNeighbor.TerrainData ?
                t.RightNeighbor.TopNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_TopRight",
                t.TopNeighbor && t.TopNeighbor.RightNeighbor && t.TopNeighbor.RightNeighbor.TerrainData ?
                t.TopNeighbor.RightNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Right",
                t.RightNeighbor && t.RightNeighbor.TerrainData ?
                t.RightNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_BottomRight",
                t.RightNeighbor && t.RightNeighbor.BottomNeighbor && t.RightNeighbor.BottomNeighbor.TerrainData ?
                t.RightNeighbor.BottomNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_BottomRight",
                t.BottomNeighbor && t.BottomNeighbor.RightNeighbor && t.BottomNeighbor.RightNeighbor.TerrainData ?
                t.BottomNeighbor.RightNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Bottom",
                t.BottomNeighbor && t.BottomNeighbor.TerrainData ?
                t.BottomNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_BottomLeft",
                t.LeftNeighbor && t.LeftNeighbor.BottomNeighbor && t.LeftNeighbor.BottomNeighbor.TerrainData ?
                t.LeftNeighbor.BottomNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_BottomLeft",
                t.BottomNeighbor && t.BottomNeighbor.LeftNeighbor && t.BottomNeighbor.LeftNeighbor.TerrainData ?
                t.BottomNeighbor.LeftNeighbor.TerrainData.Shading.AlbedoMapOrDefault :
                Texture2D.blackTexture);
        }

        public void Paint(Pinwheel.Griffin.GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (terrain.TerrainData == null)
                return;
            if (args.MouseEventType == GPainterMouseEventType.Up)
            {
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

            Texture2D bg = terrain.TerrainData.Shading.AlbedoMapOrDefault;
            int albedoResolution = terrain.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(albedoResolution);
            GCommon.CopyToRT(bg, rt);

            Material mat = GInternalMaterials.AlbedoPainterMaterial;
            mat.SetColor("_Color", args.Color);
            mat.SetTexture("_MainTex", bg);
            SetupTextureGrid(terrain, mat);
            mat.SetTexture("_Mask", args.BrushMask);
            mat.SetFloat("_Opacity", args.Opacity); 
            if (args.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", terrain.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            int pass =
                args.ActionType == GPainterActionType.Normal ? 0 :
                args.ActionType == GPainterActionType.Negative ? 1 :
                args.ActionType == GPainterActionType.Alternative ? 2 : 0;
            GCommon.DrawQuad(rt, uvCorners, mat, pass);

            RenderTexture.active = rt;
            terrain.TerrainData.Shading.AlbedoMap.ReadPixels(
                new Rect(0, 0, albedoResolution, albedoResolution), 0, 0);
            terrain.TerrainData.Shading.AlbedoMap.Apply();
            RenderTexture.active = null;

            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);

            if (!args.ForceUpdateGeometry)
                return;
            terrain.TerrainData.Geometry.SetRegionDirty(dirtyRect);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);
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

            Texture2D bg = terrain.TerrainData.Shading.AlbedoMapOrDefault;
            int albedoResolution = terrain.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(terrain, albedoResolution);
            GCommon.CopyToRT(bg, rt);

            Material mat = GInternalMaterials.AlbedoPainterMaterial;
            mat.SetColor("_Color", args.Color);
            mat.SetTexture("_MainTex", bg);
            SetupTextureGrid(terrain, mat);
            mat.SetTexture("_Mask", args.BrushMask);
            mat.SetFloat("_Opacity", args.Opacity);
            if (args.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", terrain.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            int pass =
                args.ActionType == GPainterActionType.Normal ? 0 :
                args.ActionType == GPainterActionType.Negative ? 1 :
                args.ActionType == GPainterActionType.Alternative ? 2 : 0;
            GCommon.DrawQuad(rt, uvCorners, mat, pass);

            GLivePreviewDrawer.DrawAlbedoLivePreview(terrain, cam, rt, dirtyRect);
#endif
        }
    }
}
