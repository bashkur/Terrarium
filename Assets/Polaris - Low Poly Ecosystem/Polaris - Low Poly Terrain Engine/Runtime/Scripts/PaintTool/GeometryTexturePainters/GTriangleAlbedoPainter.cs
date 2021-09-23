using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.PaintTool
{
    [GDisplayName("Triangle Albedo")]
    public class GTriangleAlbedoPainter : IGTexturePainter
    {
        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Paint color onto each triangle of the terrain mesh.\n" +
                    "   - Use Left Mouse to paint.\n" +
                    "   - Use Ctrl & Left Mouse to paint with default color.\n" +
                    "Put a HEX (#RRGGBB) value to Custom Painter Args to define the default color.\n" +
                    "Default color will be white if the HEX value is invalid.\n" +
                    "Use a material that utilizes Albedo Map to see the result.");
                return s;
            }
        }

        public string HistoryPrefix
        {
            get
            {
                return "Triangle Albedo Painting";
            }
        }

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return GCommon.AlbedoResourceFlags;
        }

        public void Paint(Pinwheel.Griffin.GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (terrain.TerrainData == null)
                return;
            if (args.MouseEventType == GPainterMouseEventType.Up)
                return;
            if (!args.Transform.IsChildOf(terrain.transform))
                return;
            if (!(args.Collider is MeshCollider))
                return;
            MeshCollider mc = args.Collider as MeshCollider;
            if (mc.sharedMesh == null)
                return;
            Mesh m = mc.sharedMesh;
            Vector2[] uvs = m.uv;
            int[] tris = m.triangles;
            int trisIndex = args.TriangleIndex;
            Vector2 v0 = uvs[tris[trisIndex * 3 + 0]];
            Vector2 v1 = uvs[tris[trisIndex * 3 + 1]];
            Vector2 v2 = uvs[tris[trisIndex * 3 + 2]];

            int albedoMapResolution = terrain.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture rt = new RenderTexture(albedoMapResolution, albedoMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            GCommon.CopyToRT(terrain.TerrainData.Shading.AlbedoMapOrDefault, rt);
            Color c = args.Color;
            if (args.ActionType == GPainterActionType.Negative)
            {
                if (!ColorUtility.TryParseHtmlString(args.CustomArgs, out c))
                {
                    c = Color.white;
                }
            }
            GCommon.DrawTriangle(rt, v0, v1, v2, c);

            RenderTexture.active = rt;
            terrain.TerrainData.Shading.AlbedoMap.ReadPixels(
                new Rect(0, 0, albedoMapResolution, albedoMapResolution), 0, 0);
            terrain.TerrainData.Shading.AlbedoMap.Apply();
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);

        }
    }
}
