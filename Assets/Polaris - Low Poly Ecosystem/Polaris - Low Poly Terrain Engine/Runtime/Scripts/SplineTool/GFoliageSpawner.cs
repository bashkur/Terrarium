using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Foliage Spawner")]
    public class GFoliageSpawner : GSplineModifier
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
        private bool spawnTrees;
        public bool SpawnTrees
        {
            get
            {
                return spawnTrees;
            }
            set
            {
                spawnTrees = value;
            }
        }

        [SerializeField]
        private bool spawnGrasses;
        public bool SpawnGrasses
        {
            get
            {
                return spawnGrasses;
            }
            set
            {
                spawnGrasses = value;
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

        [SerializeField]
        private int treeDensity;
        public int TreeDensity
        {
            get
            {
                return treeDensity;
            }
            set
            {
                treeDensity = Mathf.Clamp(value, 1, 100);
            }
        }

        [SerializeField]
        private int grassDensity;
        public int GrassDensity
        {
            get
            {
                return grassDensity;
            }
            set
            {
                grassDensity = Mathf.Clamp(value, 1, 100);
            }
        }

        [SerializeField]
        private float minRotation;
        public float MinRotation
        {
            get
            {
                return minRotation;
            }
            set
            {
                minRotation = value;
            }
        }

        [SerializeField]
        private float maxRotation;
        public float MaxRotation
        {
            get
            {
                return maxRotation;
            }
            set
            {
                maxRotation = value;
            }
        }

        [SerializeField]
        private Vector3 minScale;
        public Vector3 MinScale
        {
            get
            {
                return minScale;
            }
            set
            {
                minScale = value;
            }
        }

        [SerializeField]
        private Vector3 maxScale;
        public Vector3 MaxScale
        {
            get
            {
                return maxScale;
            }
            set
            {
                maxScale = value;
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

            t.TerrainData.Foliage.SetTreeRegionDirty(SplineCreator.SweepDirtyRect(t));
            t.TerrainData.Foliage.SetGrassRegionDirty(SplineCreator.SweepDirtyRect(t));
            Color[] maskColors = mask.GetPixels();
            if (SpawnTrees &&
                t.TerrainData.Foliage.Trees != null &&
                TreePrototypeIndices.Count > 0)
            {
                SpawnTreesOnTerrain(t, maskColors, vertices);
                t.UpdateTreesPosition();
            }
            if (SpawnGrasses &&
                t.TerrainData.Foliage.Grasses != null &&
                GrassPrototypeIndices.Count > 0)
            {
                SpawnGrassesOnTerrain(t, maskColors, vertices);
                t.UpdateGrassPatches();
            }

            t.TerrainData.Foliage.ClearTreeDirtyRegions();
            t.TerrainData.Foliage.ClearGrassDirtyRegions();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);

            rt.Release();
            GUtilities.DestroyObject(rt);
            GUtilities.DestroyObject(mask);
        }

        private void SpawnTreesOnTerrain(GStylizedTerrain t, Color[] maskData, List<Vector4> vertices)
        {
            int treeIndex = -1;
            Vector2 v0 = Vector2.zero;
            Vector2 v1 = Vector2.zero;
            Vector2 v2 = Vector2.zero;
            Vector2 center = Vector2.zero;
            float radius = 0;
            Vector2 pos = Vector2.zero;
            Vector3 bary = Vector3.zero;
            float maskValue = 0;
            int prototypeCount = t.TerrainData.Foliage.Trees.Prototypes.Count;

            List<GTreeInstance> newInstances = new List<GTreeInstance>();
            int trisCount = vertices.Count / 3;
            for (int i = 0; i < trisCount; ++i)
            {
                v0 = t.WorldPointToUV(vertices[i * 3 + 0]);
                v1 = t.WorldPointToUV(vertices[i * 3 + 1]);
                v2 = t.WorldPointToUV(vertices[i * 3 + 2]);

                center = (v0 + v1 + v2) / 3;
                radius = Vector2.Distance(center, v0);

                for (int s = 0; s < TreeDensity; ++s)
                {
                    treeIndex = TreePrototypeIndices[Random.Range(0, TreePrototypeIndices.Count)];
                    if (treeIndex < 0 || treeIndex >= prototypeCount)
                        continue;

                    pos = center + Random.insideUnitCircle * radius;
                    if (pos.x < 0 || pos.x > 1 ||
                        pos.y < 0 || pos.x > 1)
                        continue;

                    GUtilities.CalculateBarycentricCoord(pos, v0, v1, v2, ref bary);
                    if (bary.x < 0 || bary.y < 0 || bary.z < 0)
                        continue;

                    maskValue = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, pos).r;
                    if (Random.value > maskValue)
                        continue;


                    GTreeInstance tree = GTreeInstance.Create(treeIndex);
                    tree.Position = new Vector3(pos.x, 0, pos.y);
                    tree.Rotation = Quaternion.Euler(0, Random.Range(MinRotation, MaxRotation), 0);
                    tree.Scale = Vector3.Lerp(MinScale, MaxScale, Random.value);

                    newInstances.Add(tree);
                }
            }
            t.TerrainData.Foliage.AddTreeInstances(newInstances);
            newInstances.Clear();
        }

        private void SpawnGrassesOnTerrain(GStylizedTerrain t, Color[] maskData, List<Vector4> vertices)
        {
            int grassIndex = -1;
            List<GGrassInstance> grassInstances = new List<GGrassInstance>();
            Vector2 v0 = Vector2.zero;
            Vector2 v1 = Vector2.zero;
            Vector2 v2 = Vector2.zero;
            Vector2 center = Vector2.zero;
            float radius = 0;
            Vector2 pos = Vector2.zero;
            Vector3 bary = Vector3.zero;
            float maskValue = 0;
            int sampleCount = Mathf.Clamp(GrassDensity * GrassDensity / 10, 1, 1000);
            int prototypeCount = t.TerrainData.Foliage.Grasses.Prototypes.Count;

            int trisCount = vertices.Count / 3;
            for (int i = 0; i < trisCount; ++i)
            {
                v0 = t.WorldPointToUV(vertices[i * 3 + 0]);
                v1 = t.WorldPointToUV(vertices[i * 3 + 1]);
                v2 = t.WorldPointToUV(vertices[i * 3 + 2]);

                center = (v0 + v1 + v2) / 3;
                radius = Vector2.Distance(center, v0);

                for (int s = 0; s < sampleCount; ++s)
                {
                    grassIndex = GrassPrototypeIndices[Random.Range(0, GrassPrototypeIndices.Count)];
                    if (grassIndex < 0 || grassIndex >= prototypeCount)
                        continue;

                    pos = center + Random.insideUnitCircle * radius;
                    if (pos.x < 0 || pos.x > 1 ||
                        pos.y < 0 || pos.x > 1)
                        continue;

                    GUtilities.CalculateBarycentricCoord(pos, v0, v1, v2, ref bary);
                    if (bary.x < 0 || bary.y < 0 || bary.z < 0)
                        continue;

                    maskValue = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, pos).r;
                    if (Random.value > maskValue)
                        continue;

                    GGrassInstance grass = GGrassInstance.Create(grassIndex);
                    grass.Position = new Vector3(pos.x, 0, pos.y);
                    grass.Rotation = Quaternion.Euler(0, Random.Range(MinRotation, MaxRotation), 0);
                    grass.Scale = Vector3.Lerp(MinScale, MaxScale, Random.value);

                    grassInstances.Add(grass);
                }
            }

            t.TerrainData.Foliage.AddGrassInstances(grassInstances);
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
            SpawnTrees = true;
            SpawnGrasses = true;
            MaskResolution = 1024;
            TreePrototypeIndices = null;
            GrassPrototypeIndices = null;
            MinRotation = 0;
            MaxRotation = 360;
            MinScale = new Vector3(0.7f, 0.8f, 0.7f);
            MaxScale = new Vector3(1.2f, 1.5f, 1.2f);
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }
    }
}
