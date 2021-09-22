using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Path Painter")]
    public class GPathPainter : GSplineModifier
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

        public enum PaintChannel
        {
            AlbedoAndMetallic, Splat
        }

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
        private PaintChannel channel;
        public PaintChannel Channel
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
            }
        }

        [SerializeField]
        private Color color;
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        [SerializeField]
        private float metallic;
        public float Metallic
        {
            get
            {
                return metallic;
            }
            set
            {
                metallic = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float smoothness;
        public float Smoothness
        {
            get
            {
                return smoothness;
            }
            set
            {
                smoothness = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private int splatIndex;
        public int SplatIndex
        {
            get
            {
                return splatIndex;
            }
            set
            {
                splatIndex = Mathf.Max(0, value);
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

        private void Reset()
        {
            SplineCreator = GetComponent<GSplineCreator>();
            Falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
            Channel = PaintChannel.AlbedoAndMetallic;
            Color = Color.white;
            Metallic = 0;
            Smoothness = 0;
        }

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
            List<Vector4> worldPoints = SplineCreator.GenerateVerticesWithFalloff();

            if (Channel == PaintChannel.AlbedoAndMetallic)
            {
                ApplyAlbedoAndMetallic(t, worldPoints);
            }
            else if (Channel == PaintChannel.Splat)
            {
                ApplySplat(t, worldPoints);
            }
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        private void ApplyAlbedoAndMetallic(GStylizedTerrain t, List<Vector4> worldPoints)
        {
            int albedoResolution = t.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture rtAlbedo = new RenderTexture(albedoResolution, albedoResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Internal_ApplyAlbedo(t, worldPoints, rtAlbedo);

            RenderTexture.active = rtAlbedo;
            t.TerrainData.Shading.AlbedoMap.ReadPixels(new Rect(0, 0, albedoResolution, albedoResolution), 0, 0);
            t.TerrainData.Shading.AlbedoMap.Apply();
            RenderTexture.active = null;
            rtAlbedo.Release();
            Object.DestroyImmediate(rtAlbedo);

            int metallicResolution = t.TerrainData.Shading.MetallicMapResolution;
            RenderTexture rtMetallic = new RenderTexture(metallicResolution, metallicResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Internal_ApplyMetallic(t, worldPoints, rtMetallic);

            RenderTexture.active = rtMetallic;
            t.TerrainData.Shading.MetallicMap.ReadPixels(new Rect(0, 0, metallicResolution, metallicResolution), 0, 0);
            t.TerrainData.Shading.MetallicMap.Apply();
            RenderTexture.active = null;
            rtMetallic.Release();
            Object.DestroyImmediate(rtMetallic);
        }

        public void Internal_ApplyAlbedo(GStylizedTerrain t, List<Vector4> worldPoints, RenderTexture rtAlbedo)
        {
            GCommon.CopyToRT(t.TerrainData.Shading.AlbedoMapOrDefault, rtAlbedo);
            Material mat = GInternalMaterials.PathPainterMaterial;
            mat.SetTexture("_MainTex", t.TerrainData.Shading.AlbedoMapOrDefault);
            mat.SetFloat("_Metallic", Metallic);
            mat.SetFloat("_Smoothness", Smoothness);
            mat.SetTexture("_Falloff", falloffTexture);
            mat.SetColor("_Color", Color);
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
            DrawOnTexture(rtAlbedo, mat, pass, worldPoints, t);

            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        public void Internal_ApplyMetallic(GStylizedTerrain t, List<Vector4> worldPoints, RenderTexture rtMetallic)
        {
            GCommon.CopyToRT(t.TerrainData.Shading.MetallicMapOrDefault, rtMetallic);
            Material mat = GInternalMaterials.PathPainterMaterial;
            mat.SetTexture("_MainTex", t.TerrainData.Shading.MetallicMapOrDefault);
            mat.SetFloat("_Metallic", Metallic);
            mat.SetFloat("_Smoothness", Smoothness);
            mat.SetTexture("_FalloffNoise", FalloffNoise);
            mat.SetTextureScale("_FalloffNoise", new Vector2(
                FalloffNoiseSize.x != 0 ? 1f / FalloffNoiseSize.x : 0,
                FalloffNoiseSize.y != 0 ? 1f / FalloffNoiseSize.y : 0));
            mat.SetTextureOffset("_FalloffNoise", Vector2.zero);
            int pass = 1;
            DrawOnTexture(rtMetallic, mat, pass, worldPoints, t);
            if (SplineCreator.EnableTerrainMask)
            {
                mat.SetTexture("_TerrainMask", t.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture("_TerrainMask", Texture2D.blackTexture);
            }
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }

        private void ApplySplat(GStylizedTerrain t, List<Vector4> worldPoints)
        {
            int splatControlResolution = t.TerrainData.Shading.SplatControlResolution;
            int controlMapCount = t.TerrainData.Shading.SplatControlMapCount;
            RenderTexture[] rtControls = new RenderTexture[controlMapCount];
            for (int i = 0; i < controlMapCount; ++i)
            {
                rtControls[i] = new RenderTexture(splatControlResolution, splatControlResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            Internal_ApplySplat(t, worldPoints, rtControls);
            for (int i = 0; i < controlMapCount; ++i)
            {
                Texture2D splatControl = t.TerrainData.Shading.GetSplatControl(i);
                RenderTexture.active = rtControls[i];
                splatControl.ReadPixels(new Rect(0, 0, splatControlResolution, splatControlResolution), 0, 0);
                splatControl.Apply();
                RenderTexture.active = null;

                rtControls[i].Release();
                Object.DestroyImmediate(rtControls[i]);
            }
        }

        public void Internal_ApplySplat(GStylizedTerrain t, List<Vector4> worldPoints, RenderTexture[] rtControls)
        {
            Material mat = GInternalMaterials.PathPainterMaterial;
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
            int pass = 2;
            for (int i = 0; i < rtControls.Length; ++i)
            {
                Texture2D splatControl = t.TerrainData.Shading.GetSplatControlOrDefault(i);
                GCommon.CopyToRT(splatControl, rtControls[i]);
                mat.SetTexture("_MainTex", splatControl);
                if (SplatIndex / 4 == i)
                {
                    mat.SetInt("_ChannelIndex", SplatIndex % 4);
                }
                else
                {
                    mat.SetInt("_ChannelIndex", -1);
                }
                DrawOnTexture(rtControls[i], mat, pass, worldPoints, t);
            }

            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        private void DrawOnTexture(RenderTexture rt, Material mat, int pass, List<Vector4> worldPoints, GStylizedTerrain t)
        {
            List<Vector4> normalizedPoints = new List<Vector4>();
            for (int i = 0; i < worldPoints.Count; ++i)
            {
                Vector3 v = t.WorldPointToNormalized(worldPoints[i]);
                normalizedPoints.Add(new Vector4(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y), Mathf.Clamp01(v.z), worldPoints[i].w));
            }

            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadOrtho();
            int trisCount = worldPoints.Count / 3;

            GL.Begin(GL.TRIANGLES);
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
    }
}
