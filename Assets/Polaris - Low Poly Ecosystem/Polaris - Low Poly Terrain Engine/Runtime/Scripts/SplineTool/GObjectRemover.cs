using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Object Remover")]
    public class GObjectRemover : GSplineModifier
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
            RemoveObjectFromTerrain(t, maskColors);

            rt.Release();
            GUtilities.DestroyObject(rt);
            GUtilities.DestroyObject(mask);
        }

        private void RemoveObjectFromTerrain(GStylizedTerrain t, Color[] maskData)
        {
            for (int i = 0; i < PrototypeIndices.Count; ++i)
            {
                int prototypeIndex = PrototypeIndices[i];
                if (prototypeIndex < 0 || prototypeIndex >= Prototypes.Count)
                    continue;
                GameObject g = Prototypes[prototypeIndex];
                if (g == null)
                    continue;

                GSpawner.DestroyIf(t, g, (instance) =>
                {
                    Vector2 uv = t.WorldPointToUV(instance.transform.position);
                    float alpha = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, uv).r;
                    return Random.value <= alpha;
                });
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
            MaskResolution = 1024;
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }
    }
}
