using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Ramp Maker")]
    public class GRampMaker : GSplineModifier
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool editor_ShowLivePreview = true;
        public bool Editor_ShowLivePreview
        {
            get
            {
                return editor_ShowLivePreview;
            }
            set
            {
                editor_ShowLivePreview = value;
            }
        }
#endif

        [SerializeField]
        private AnimationCurve falloff;
        public AnimationCurve Falloff
        {
            get
            {
                if (falloff == null)
                {
                    falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
                }
                return falloff;
            }
            set
            {
                falloff = value;
            }
        }

        [SerializeField]
        private bool lowerHeight;
        public bool LowerHeight
        {
            get
            {
                return lowerHeight;
            }
            set
            {
                lowerHeight = value;
            }
        }

        [SerializeField]
        private bool raiseHeight;
        public bool RaiseHeight
        {
            get
            {
                return raiseHeight;
            }
            set
            {
                raiseHeight = value;
            }
        }

        [SerializeField]
        private int additionalMeshResolution;
        public int AdditionalMeshResolution
        {
            get
            {
                return additionalMeshResolution;
            }
            set
            {
                additionalMeshResolution = Mathf.Clamp(value, 0, 10);
            }
        }

        [SerializeField]
        private float heightOffset;
        public float HeightOffset
        {
            get
            {
                return heightOffset;
            }
            set
            {
                heightOffset = value;
            }
        }

        [SerializeField]
        private int stepCount;
        public int StepCount
        {
            get
            {
                return stepCount;
            }
            set
            {
                stepCount = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        private Texture2D falloffNoise;
        public Texture2D FalloffNoise
        {
            get
            {
                return falloffNoise;
            }
            set
            {
                falloffNoise = value;
            }
        }

        [SerializeField]
        private Vector2 falloffNoiseSize;
        public Vector2 FalloffNoiseSize
        {
            get
            {
                return falloffNoiseSize;
            }
            set
            {
                falloffNoiseSize = value;
            }
        }

        private Texture2D falloffTexture;

        public override void Apply()
        {
            if (SplineCreator == null)
                return;
            if (falloffTexture != null)
                Object.DestroyImmediate(falloffTexture);
            Internal_UpdateFalloffTexture();
            int groupId = SplineCreator.GroupId;
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (groupId < 0 ||
                    (groupId >= 0 && groupId == t.GroupId))
                {
                    Apply(t);
                }
            }
            GStylizedTerrain.MatchEdges(groupId);
        }

        private void Apply(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;
            int heightMapResolution = t.TerrainData.Geometry.HeightMapResolution;
            RenderTexture rt = new RenderTexture(heightMapResolution, heightMapResolution, 0, GGeometry.HeightMapRTFormat, RenderTextureReadWrite.Linear);
            List<Vector4> vertices = SplineCreator.GenerateVerticesWithFalloff();
            Internal_Apply(t, rt, vertices);

            Color[] oldHeightMapColors = t.TerrainData.Geometry.HeightMap.GetPixels();
            RenderTexture.active = rt;
            t.TerrainData.Geometry.HeightMap.ReadPixels(new Rect(0, 0, heightMapResolution, heightMapResolution), 0, 0);
            t.TerrainData.Geometry.HeightMap.Apply();
            RenderTexture.active = null;
            Color[] newHeightMapColors = t.TerrainData.Geometry.HeightMap.GetPixels();

            rt.Release();
            Object.DestroyImmediate(rt);

            List<Rect> dirtyRects = new List<Rect>(GCommon.CompareTerrainTexture(t.TerrainData.Geometry.ChunkGridSize, oldHeightMapColors, newHeightMapColors));
            for (int i = 0; i < dirtyRects.Count; ++i)
            {
                t.TerrainData.Geometry.SetRegionDirty(dirtyRects[i]);
            }
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);

            for (int i = 0; i < dirtyRects.Count; ++i)
            {
                t.TerrainData.Foliage.SetTreeRegionDirty(dirtyRects[i]);
                t.TerrainData.Foliage.SetGrassRegionDirty(dirtyRects[i]);
            }
            t.UpdateTreesPosition();
            t.UpdateGrassPatches();
            t.TerrainData.Foliage.ClearTreeDirtyRegions();
            t.TerrainData.Foliage.ClearGrassDirtyRegions();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);
        }

        public void Internal_Apply(GStylizedTerrain t, RenderTexture rt, List<Vector4> vertices)
        {
            GCommon.CopyToRT(t.TerrainData.Geometry.HeightMap, rt);
            Material mat = GInternalMaterials.RampMakerMaterial;
            mat.SetTexture("_HeightMap", t.TerrainData.Geometry.HeightMap);
            mat.SetTexture("_Falloff", falloffTexture);
            mat.SetInt("_LowerHeight", LowerHeight ? 1 : 0);
            mat.SetInt("_RaiseHeight", RaiseHeight ? 1 : 0);
            mat.SetFloat("_AdditionalMeshResolution", GCommon.SUB_DIV_STEP * AdditionalMeshResolution);
            mat.SetTexture("_FalloffNoise", FalloffNoise);
            mat.SetTextureScale("_FalloffNoise", new Vector2(
                FalloffNoiseSize.x != 0 ? 1f / FalloffNoiseSize.x : 0,
                FalloffNoiseSize.y != 0 ? 1f / FalloffNoiseSize.y : 0));
            mat.SetTextureOffset("_FalloffNoise", Vector2.zero);
            if (SplineCreator.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", t.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            mat.SetInt("_StepCount", StepCount);
            int pass = 0;
            DrawOnTexture(rt, mat, pass, vertices, t);
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }

        private void DrawOnTexture(RenderTexture rt, Material mat, int pass, List<Vector4> worldPoints, GStylizedTerrain t)
        {
            Vector4 worldOffset = new Vector4(0, HeightOffset, 0, 0);
            List<Vector4> normalizedPoints = new List<Vector4>();
            for (int i = 0; i < worldPoints.Count; ++i)
            {
                Vector3 v = t.WorldPointToNormalized(worldPoints[i] + worldOffset);
                normalizedPoints.Add(new Vector4(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y), Mathf.Clamp01(v.z), worldPoints[i].w));
            }

            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            int trisCount = worldPoints.Count / 3;
            GCommon.SetMaterialKeywordActive(mat, "FALLOFF", true);
            mat.SetPass(pass);
            for (int i = 0; i < trisCount; ++i)
            {
                Vector4 v0 = worldPoints[i * 3 + 0];
                Vector4 v1 = worldPoints[i * 3 + 1];
                Vector4 v2 = worldPoints[i * 3 + 2];

                if (v0.w == 0 || v1.w == 0 || v2.w == 0)
                {
                    Vector4 vn0 = normalizedPoints[i * 3 + 0];
                    Vector4 vn1 = normalizedPoints[i * 3 + 1];
                    Vector4 vn2 = normalizedPoints[i * 3 + 2];

                    GL.MultiTexCoord3(0, vn0.x, vn0.z, vn0.y);
                    GL.MultiTexCoord3(1, v0.x, v0.z, v0.y);
                    GL.Color(new Color(vn0.w, vn0.w, vn0.w, vn0.w));
                    GL.Vertex3(vn0.x, vn0.z, vn0.y);

                    GL.MultiTexCoord3(0, vn1.x, vn1.z, vn1.y);
                    GL.MultiTexCoord3(1, v1.x, v1.z, v1.y);
                    GL.Color(new Color(vn1.w, vn1.w, vn1.w, vn1.w));
                    GL.Vertex3(vn1.x, vn1.z, vn1.y);

                    GL.MultiTexCoord3(0, vn2.x, vn2.z, vn2.y);
                    GL.MultiTexCoord3(1, v2.x, v2.z, v2.y);
                    GL.Color(new Color(vn2.w, vn2.w, vn2.w, vn2.w));
                    GL.Vertex3(vn2.x, vn2.z, vn2.y);
                }
            }
            GL.End();

            GL.Begin(GL.TRIANGLES);
            GCommon.SetMaterialKeywordActive(mat, "FALLOFF", false);
            mat.SetPass(pass);
            for (int i = 0; i < trisCount; ++i)
            {
                Vector4 v0 = worldPoints[i * 3 + 0];
                Vector4 v1 = worldPoints[i * 3 + 1];
                Vector4 v2 = worldPoints[i * 3 + 2];

                if (v0.w != 0 && v1.w != 0 && v2.w != 0)
                {
                    Vector4 vn0 = normalizedPoints[i * 3 + 0];
                    Vector4 vn1 = normalizedPoints[i * 3 + 1];
                    Vector4 vn2 = normalizedPoints[i * 3 + 2];

                    GL.MultiTexCoord3(0, vn0.x, vn0.z, vn0.y);
                    GL.MultiTexCoord3(1, v0.x, v0.z, v0.y);
                    GL.Color(new Color(vn0.w, vn0.w, vn0.w, vn0.w));
                    GL.Vertex3(vn0.x, vn0.z, vn0.y);

                    GL.MultiTexCoord3(0, vn1.x, vn1.z, vn1.y);
                    GL.MultiTexCoord3(1, v1.x, v1.z, v1.y);
                    GL.Color(new Color(vn1.w, vn1.w, vn1.w, vn1.w));
                    GL.Vertex3(vn1.x, vn1.z, vn1.y);

                    GL.MultiTexCoord3(0, vn2.x, vn2.z, vn2.y);
                    GL.MultiTexCoord3(1, v2.x, v2.z, v2.y);
                    GL.Color(new Color(vn2.w, vn2.w, vn2.w, vn2.w));
                    GL.Vertex3(vn2.x, vn2.z, vn2.y);
                }
            }
            GL.End();
            GL.PopMatrix();
            RenderTexture.active = null;
        }

        public void Reset()
        {
            SplineCreator = GetComponent<GSplineCreator>();
            Falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
            LowerHeight = true;
            RaiseHeight = true;
            HeightOffset = 0;
            StepCount = 1000;
        }
    }
}
