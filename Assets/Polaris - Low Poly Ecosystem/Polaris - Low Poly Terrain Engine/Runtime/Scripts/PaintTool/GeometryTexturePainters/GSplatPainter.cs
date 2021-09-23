using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.PaintTool
{
    public class GSplatPainter : IGTexturePainter, IGTexturePainterWithLivePreview
    {
        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Paint blend weight on terrain Splat Control maps.\n" +
                    "   - Hold Left Mouse to paint.\n" +
                    "   - Hold Ctrl & Left Mouse to erase selected layer.\n" +
                    "   - Hold Shift & Left Mouse to erase all layer.\n" +
                    "Use a material that utilizes splat maps to see the result.");
                return s;
            }
        }

        public string HistoryPrefix
        {
            get
            {
                return "Splat Painting";
            }
        }

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return GCommon.SplatResourceFlags;
        }

        public void Paint(Pinwheel.Griffin.GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (terrain.TerrainData == null)
                return;
            if (terrain.TerrainData.Shading.Splats == null)
                return;
            if (args.MouseEventType == GPainterMouseEventType.Up)
            {
                return;
            }
            int splatIndex = args.SplatIndex;
            if (splatIndex < 0 || splatIndex >= terrain.TerrainData.Shading.SplatControlMapCount * 4)
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
            Material mat = GInternalMaterials.SplatPainterMaterial;
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
            int controlMapResolution = terrain.TerrainData.Shading.SplatControlResolution;
            int controlMapCount = terrain.TerrainData.Shading.SplatControlMapCount;
            for (int i = 0; i < controlMapCount; ++i)
            {
                Texture2D splatControl = terrain.TerrainData.Shading.GetSplatControl(i);
                RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(controlMapResolution);
                GCommon.CopyToRT(splatControl, rt);

                mat.SetTexture("_MainTex", splatControl);
                if (splatIndex / 4 == i)
                {
                    mat.SetInt("_ChannelIndex", splatIndex % 4);
                }
                else
                {
                    mat.SetInt("_ChannelIndex", -1);
                }
                int pass =
                    args.ActionType == GPainterActionType.Normal ? 0 :
                    args.ActionType == GPainterActionType.Negative ? 1 :
                    args.ActionType == GPainterActionType.Alternative ? 2 : 0;
                GCommon.DrawQuad(rt, uvCorners, mat, pass);

                RenderTexture.active = rt;
                splatControl.ReadPixels(new Rect(0, 0, controlMapResolution, controlMapResolution), 0, 0);
                splatControl.Apply();
                RenderTexture.active = null;
            }
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);

            if (!args.ForceUpdateGeometry)
                return;
            terrain.TerrainData.Geometry.SetRegionDirty(dirtyRect);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);
        }

        public void Editor_DrawLivePreview(GStylizedTerrain terrain, GTexturePainterArgs args, Camera cam)
        {
#if UNITY_EDITOR
            int splatIndex = args.SplatIndex;
            if (splatIndex < 0 || splatIndex >= terrain.TerrainData.Shading.SplatControlMapCount * 4)
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
            Material mat = GInternalMaterials.SplatPainterMaterial;
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
            int controlMapResolution = terrain.TerrainData.Shading.SplatControlResolution;
            int controlMapCount = terrain.TerrainData.Shading.SplatControlMapCount;
            for (int i = 0; i < controlMapCount; ++i)
            {
                Texture2D splatControl = terrain.TerrainData.Shading.GetSplatControlOrDefault(i);
                RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(terrain, controlMapResolution, i);
                GCommon.CopyToRT(splatControl, rt);

                mat.SetTexture("_MainTex", splatControl);
                if (splatIndex / 4 == i)
                {
                    mat.SetInt("_ChannelIndex", splatIndex % 4);
                }
                else
                {
                    mat.SetInt("_ChannelIndex", -1);
                }
                int pass =
                    args.ActionType == GPainterActionType.Normal ? 0 :
                    args.ActionType == GPainterActionType.Negative ? 1 :
                    args.ActionType == GPainterActionType.Alternative ? 2 : 0;
                GCommon.DrawQuad(rt, uvCorners, mat, pass);
            }

            Texture[] controls = new Texture[controlMapCount];
            for (int i = 0; i < controlMapCount; ++i)
            {
                controls[i] = GTerrainTexturePainter.Internal_GetRenderTexture(terrain, controlMapResolution, i);
            }

            GLivePreviewDrawer.DrawSplatLivePreview(terrain, cam, controls, dirtyRect);
#endif
        }
    }
}
