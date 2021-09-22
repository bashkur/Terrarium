using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Foliage Remover")]
    public class GFoliageRemover : GSplineModifier
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
        private bool removeTrees;
        public bool RemoveTrees
        {
            get
            {
                return removeTrees;
            }
            set
            {
                removeTrees = value;
            }
        }

        [SerializeField]
        private bool removeGrasses;
        public bool RemoveGrasses
        {
            get
            {
                return removeGrasses;
            }
            set
            {
                removeGrasses = value;
            }
        }

        [SerializeField]
        private List<int> treePrototypeIndices;
        public List<int> TreePrototypeIndices
        {
            get
            {
                if (treePrototypeIndices == null)
                {
                    treePrototypeIndices = new List<int>();
                }
                return treePrototypeIndices;
            }
            set
            {
                treePrototypeIndices = value;
            }
        }

        [SerializeField]
        private List<int> grassPrototypeIndices;
        public List<int> GrassPrototypeIndices
        {
            get
            {
                if (grassPrototypeIndices == null)
                {
                    grassPrototypeIndices = new List<int>();
                }
                return grassPrototypeIndices;
            }
            set
            {
                grassPrototypeIndices = value;
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

        [SerializeField]
        private int maskResolution;
        public int MaskResolution
        {
            get
            {
                return maskResolution;
            }
            set
            {
                maskResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(value), GCommon.TEXTURE_SIZE_MIN, GCommon.TEXTURE_SIZE_MAX);
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
        }

        private void Apply(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;
            RenderTexture rt = new RenderTexture(MaskResolution, MaskResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            List<Vector4> vertices = SplineCreator.GenerateVerticesWithFalloff();
            Internal_Apply(t, rt, vertices);
            Texture2D mask = GCommon.CreateTexture(MaskResolution, Color.clear);
            GCommon.CopyFromRT(mask, rt);
            mask.wrapMode = TextureWrapMode.Clamp;

            Color[] maskColors = mask.GetPixels();
            if (RemoveTrees)
            {
                RemoveTreeOnTerrain(t, maskColors);
            }
            if (RemoveGrasses)
            {
                RemoveGrassOnTerrain(t, maskColors);
            }

            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);

            rt.Release();
            GUtilities.DestroyObject(rt);
            GUtilities.DestroyObject(mask);
        }

        private void RemoveTreeOnTerrain(GStylizedTerrain t, Color[] maskData)
        {
            for (int i = 0; i < TreePrototypeIndices.Count; ++i)
            {
                int treeIndex = TreePrototypeIndices[i];
                t.TerrainData.Foliage.RemoveTreeInstances(tree =>
                {
                    if (tree.PrototypeIndex != treeIndex)
                        return false;
                    Vector2 uv = new Vector2(tree.Position.x, tree.Position.z);
                    float alpha = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, uv).r;
                    return Random.value <= alpha;
                });
            }
        }

        private void RemoveGrassOnTerrain(GStylizedTerrain t, Color[] maskData)
        {
            GGrassPatch[] patches = t.TerrainData.Foliage.GrassPatches;
            for (int p = 0; p < patches.Length; ++p)
            {
                for (int i = 0; i < GrassPrototypeIndices.Count; ++i)
                {
                    int grassIndex = GrassPrototypeIndices[i];
                    patches[p].RemoveInstances(grass =>
                    {
                        if (grass.PrototypeIndex != grassIndex)
                            return false;
                        Vector2 uv = new Vector2(grass.Position.x, grass.Position.z);
                        float alpha = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, uv).r;
                        return Random.value <= alpha;
                    });
                }
            }
        }

        public void Internal_Apply(GStylizedTerrain t, RenderTexture rt, List<Vector4> vertices)
        {
            GCommon.ClearRT(rt);
            Material mat = GInternalMaterials.FoliageRemoverMaterial;
            mat.SetTexture("_Falloff", falloffTexture);
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
            int pass = 0;
            DrawOnTexture(rt, mat, pass, vertices, t);
        }

        private void DrawOnTexture(RenderTexture rt, Material mat, int pass, List<Vector4> worldPoints, GStylizedTerrain t)
        {
            List<Vector4> normalizedPoints = new List<Vector4>();
            for (int i = 0; i < worldPoints.Count; ++i)
            {
                Vector3 v = t.WorldPointToNormalized(worldPoints[i]);
                normalizedPoints.Add(new Vector4(v.x, v.y, v.z, worldPoints[i].w));
            }

            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadOrtho();

            GCommon.SetMaterialKeywordActive(mat, "FALLOFF", true);
            mat.SetPass(pass);
            GL.Begin(GL.TRIANGLES);
            int trisCount = worldPoints.Count / 3;
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

            GCommon.SetMaterialKeywordActive(mat, "FALLOFF", false);
            mat.SetPass(pass);
            GL.Begin(GL.TRIANGLES);
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
            RemoveTrees = true;
            RemoveGrasses = true;
            TreePrototypeIndices = null;
            GrassPrototypeIndices = null;
            MaskResolution = 1024;
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }
    }
}
