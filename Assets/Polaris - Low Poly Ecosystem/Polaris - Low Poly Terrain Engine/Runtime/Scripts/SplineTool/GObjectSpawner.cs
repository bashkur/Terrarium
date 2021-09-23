using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Object Spawner")]
    public class GObjectSpawner : GSplineModifier
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
        private List<GameObject> prototypes;
        public List<GameObject> Prototypes
        {
            get
            {
                if (prototypes == null)
                {
                    prototypes = new List<GameObject>();
                }
                return prototypes;
            }
            set
            {
                prototypes = value;
            }
        }

        [SerializeField]
        private List<int> prototypeIndices;
        public List<int> PrototypeIndices
        {
            get
            {
                if (prototypeIndices == null)
                {
                    prototypeIndices = new List<int>();
                }
                return prototypeIndices;
            }
            set
            {
                prototypeIndices = value;
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
        private int density;
        public int Density
        {
            get
            {
                return density;
            }
            set
            {
                density = Mathf.Clamp(value, 1, 100);
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

        [SerializeField]
        private bool alignToSurface;
        public bool AlignToSurface
        {
            get
            {
                return alignToSurface;
            }
            set
            {
                alignToSurface = value;
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
            if (PrototypeIndices.Count == 0)
                return;
            if (Prototypes.Count == 0)
                return;
            RenderTexture rt = new RenderTexture(MaskResolution, MaskResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            List<Vector4> vertices = SplineCreator.GenerateVerticesWithFalloff();
            Internal_Apply(t, rt, vertices);
            Texture2D mask = GCommon.CreateTexture(MaskResolution, Color.clear);
            GCommon.CopyFromRT(mask, rt);
            mask.wrapMode = TextureWrapMode.Clamp;

            Color[] maskColors = mask.GetPixels();
            SpawnObjectsOnTerrain(t, maskColors, vertices);

            rt.Release();
            GUtilities.DestroyObject(rt);
            GUtilities.DestroyObject(mask);
        }

        private void SpawnObjectsOnTerrain(GStylizedTerrain t, Color[] maskData, List<Vector4> vertices)
        {
            int prototypeIndex = -1;
            Vector2 v0 = Vector2.zero;
            Vector2 v1 = Vector2.zero;
            Vector2 v2 = Vector2.zero;
            Vector2 center = Vector2.zero;
            float radius = 0;
            Vector2 pos = Vector2.zero;
            Vector3 bary = Vector3.zero;
            float maskValue = 0;
            RaycastHit hit;

            int trisCount = vertices.Count / 3;
            for (int i = 0; i < trisCount; ++i)
            {
                v0 = t.WorldPointToUV(vertices[i * 3 + 0]);
                v1 = t.WorldPointToUV(vertices[i * 3 + 1]);
                v2 = t.WorldPointToUV(vertices[i * 3 + 2]);

                center = (v0 + v1 + v2) / 3;
                radius = Vector2.Distance(center, v0);

                for (int s = 0; s < Density; ++s)
                {
                    prototypeIndex = PrototypeIndices[Random.Range(0, PrototypeIndices.Count)];
                    if (prototypeIndex < 0 || prototypeIndex >= Prototypes.Count)
                        continue;
                    GameObject g = Prototypes[prototypeIndex];
                    if (g == null)
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

                    if (t.Raycast(pos.ToX0Y(), out hit))
                    {
                        GameObject instance = GSpawner.Spawn(t, g, hit.point);
                        instance.transform.rotation = Quaternion.Euler(0, Random.Range(MinRotation, MaxRotation), 0);
                        instance.transform.localScale = Vector3.Lerp(MinScale, MaxScale, Random.value);
                        if (AlignToSurface)
                        {
                            instance.transform.up = hit.normal;
                        }
                    }
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
            Prototypes = null;
            PrototypeIndices = null;
            Density = 1;
            MaskResolution = 1024;
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
