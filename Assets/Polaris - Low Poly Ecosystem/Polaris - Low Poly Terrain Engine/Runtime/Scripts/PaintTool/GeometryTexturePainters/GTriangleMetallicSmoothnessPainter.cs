using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.PaintTool
{
    [GDisplayName("Triangle MS")]
    public class GTriangleMetallicSmoothnessPainter : IGTexturePainter
    {
        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Paint metallic and smoothness onto each triangle of the terrain mesh.\n" +
                    "   - Use Left Mouse to paint.\n" +
                    "Use a material that utilizes Metallic Map to see the result.");
                return s;
            }
        }

        public string HistoryPrefix
        {
            get
            {
                return "Triangle Metallic Smoothness Painting";
            }
        }

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return GCommon.MetallicResourceFlags;
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

            int metallicMapResolution = terrain.TerrainData.Shading.MetallicMapResolution;
            RenderTexture rt = new RenderTexture(metallicMapResolution, metallicMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            GCommon.CopyToRT(terrain.TerrainData.Shading.MetallicMapOrDefault, rt);
            Color c = new Color(args.Color.r, args.Color.r, args.Color.r, args.Color.a);
            GCommon.DrawTriangle(rt, v0, v1, v2, c);

            RenderTexture.active = rt;
            terrain.TerrainData.Shading.MetallicMap.ReadPixels(
                new Rect(0, 0, metallicMapResolution, metallicMapResolution), 0, 0);
            terrain.TerrainData.Shading.MetallicMap.Apply();
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);

        }
    }
}
