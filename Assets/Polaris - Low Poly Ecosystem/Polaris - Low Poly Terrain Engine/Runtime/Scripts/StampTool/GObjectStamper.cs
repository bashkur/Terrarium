using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.StampTool
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class GObjectStamper : MonoBehaviour
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

        [SerializeField]
        private bool editor_ShowBounds = true;
        public bool Editor_ShowBounds
        {
            get
            {
                return editor_ShowBounds;
            }
            set
            {
                editor_ShowBounds = value;
            }
        }
#endif

        [SerializeField]
        private bool enableTerrainMask;
        public bool EnableTerrainMask
        {
            get
            {
                return enableTerrainMask;
            }
            set
            {
                enableTerrainMask = value;
            }
        }

        [SerializeField]
        private int groupId;
        public int GroupId
        {
            get
            {
                return groupId;
            }
            set
            {
                groupId = value;
            }
        }

        [SerializeField]
        private Vector3 position;
        public Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                transform.position = value;
            }
        }

        [SerializeField]
        private Quaternion rotation;
        public Quaternion Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
                transform.rotation = value;
            }
        }

        [SerializeField]
        private Vector3 scale;
        public Vector3 Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                transform.localScale = value;
            }
        }

        [SerializeField]
        private Texture2D mask;
        public Texture2D Mask
        {
            get
            {
                return mask;
            }
            set
            {
                mask = value;
            }
        }

        [SerializeField]
        private AnimationCurve falloff;
        public AnimationCurve Falloff
        {
            get
            {
                return falloff;
            }
            set
            {
                falloff = value;
            }
        }

        [SerializeField]
        private List<GObjectStampLayer> layers;
        public List<GObjectStampLayer> Layers
        {
            get
            {
                if (layers == null)
                {
                    layers = new List<GObjectStampLayer>();
                }
                return layers;
            }
            set
            {
                layers = value;
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

        public Rect Rect
        {
            get
            {
                Vector3[] quad = new Vector3[4];
                GetQuad(quad);
                Rect r = GUtilities.GetRectContainsPoints(quad);
                return r;
            }
        }

        private Texture2D falloffTexture;

        private Vector3[] worldPoints = new Vector3[4];
        private Vector2[] uvPoints = new Vector2[4];

        private Dictionary<string, RenderTexture> tempRt;
        private Dictionary<string, RenderTexture> TempRt
        {
            get
            {
                if (tempRt == null)
                {
                    tempRt = new Dictionary<string, RenderTexture>();
                }
                return tempRt;
            }
        }

        private void Reset()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one * 100;
            mask = null;
            falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
            maskResolution = 1024;
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void OnDestroy()
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            foreach (RenderTexture rt in TempRt.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    GUtilities.DestroyObject(rt);
                }
            }
        }

        public void Apply()
        {
            if (falloffTexture != null)
                Object.DestroyImmediate(falloffTexture);
            Internal_UpdateFalloffTexture();
            Internal_UpdateLayerTransitionTextures();
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();

            try
            {
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
            catch (GProgressCancelledException)
            {
                Debug.Log("Stamp process canceled, result may be incorrect. Use History to clean up!");
#if UNITY_EDITOR
                GCommonGUI.ClearProgressBar();
#endif
            }
        }

        private void Apply(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;
            if (Layers.Count == 0)
                return;

            GetQuad(worldPoints);
            GetUvPoints(t, worldPoints, uvPoints);
            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvPoints);
            if (!dirtyRect.Overlaps(new Rect(0, 0, 1, 1)))
                return;

            RenderTexture[] brushes = new RenderTexture[Layers.Count];
            for (int i = 0; i < Layers.Count; ++i)
            {
                brushes[i] = GetRenderTexture("brush" + i.ToString());
            }
            Internal_RenderBrushes(brushes, t, uvPoints);

            for (int i = 0; i < Layers.Count; ++i)
            {
                StampLayer(t, brushes[i], i);
            }

#if UNITY_EDITOR
            GCommonGUI.ClearProgressBar();
#endif
        }

        private void StampLayer(GStylizedTerrain t, RenderTexture brush, int layerIndex)
        {
            GObjectStampLayer layer = Layers[layerIndex];
            if (layer.Ignore)
                return;
            if (layer.InstanceCount == 0)
                return;
            if (layer.Prototypes.Count == 0 ||
                layer.PrototypeIndices.Count == 0)
                return;

            Texture2D tex = new Texture2D(brush.width, brush.height, TextureFormat.ARGB32, false, true);
            GCommon.CopyFromRT(tex, brush);
            Color[] maskData = tex.GetPixels();
            SpawnObjectOnTerrain(t, maskData, layerIndex);
        }

        private void SpawnObjectOnTerrain(GStylizedTerrain t, Color[] maskData, int layerIndex)
        {
            GObjectStampLayer layer = Layers[layerIndex];
            Vector3 centerPos = Vector3.zero;
            Vector3 samplePos = Vector3.zero;
            Vector2 uv = Vector2.zero;
            float maskValue = 0;
            Vector3 terrainSize = new Vector3(
                t.TerrainData.Geometry.Width,
                t.TerrainData.Geometry.Height,
                t.TerrainData.Geometry.Length);
            Vector3 scale = new Vector3(
                GUtilities.InverseLerpUnclamped(0, terrainSize.x, Scale.x),
                1,
                GUtilities.InverseLerpUnclamped(0, terrainSize.z, Scale.z));
            Matrix4x4 matrix = Matrix4x4.TRS(
                t.WorldPointToNormalized(Position),
                Rotation,
                scale);

            int index = -1;
            int instanceCount = 0;
            int attempt = 0;
            int maxAttempt = layer.InstanceCount * 100;
            RaycastHit hit;

#if UNITY_EDITOR
            string title = "Stamping on " + t.name;
            string info = string.Format("Layer: {0}", !string.IsNullOrEmpty(layer.Name) ? layer.Name : layerIndex.ToString());
            int currentPercent = 0;
            int attemptPercent = 0;
            int instancePercent = 0;
            GCommonGUI.CancelableProgressBar(title, info, 0);
#endif

            while (instanceCount < layer.InstanceCount && attempt <= maxAttempt)
            {
                attempt += 1;

#if UNITY_EDITOR
                attemptPercent = (int)(attempt * 100.0f / maxAttempt);
                instancePercent = (int)(instanceCount * 100.0f / layer.InstanceCount);
                if (currentPercent != Mathf.Max(attemptPercent, instancePercent))
                {
                    currentPercent = Mathf.Max(attemptPercent, instancePercent);
                    GCommonGUI.CancelableProgressBar(title, string.Format("{0} ... {1}%", info, currentPercent), currentPercent / 100.0f);
                }
#endif

                index = layer.PrototypeIndices[Random.Range(0, layer.PrototypeIndices.Count)];
                if (index < 0 || index >= layer.Prototypes.Count)
                    continue;
                GameObject g = layer.Prototypes[index];
                if (g == null)
                    continue;

                centerPos.Set(Random.value - 0.5f, 0, Random.value - 0.5f);
                samplePos = matrix.MultiplyPoint(centerPos);
                if (samplePos.x < 0 || samplePos.x > 1 ||
                    samplePos.z < 0 || samplePos.z > 1)
                    continue;
                uv.Set(samplePos.x, samplePos.z);
                maskValue = GUtilities.GetColorBilinear(maskData, MaskResolution, MaskResolution, uv).r;
                if (Random.value > maskValue)
                    continue;

                if (t.Raycast(samplePos, out hit))
                {
                    GameObject instance = GSpawner.Spawn(t, g, hit.point);
                    instance.transform.rotation = Quaternion.Euler(0, Random.Range(layer.MinRotation, layer.MaxRotation), 0);
                    instance.transform.localScale = Vector3.Lerp(layer.MinScale, layer.MaxScale, Random.value);
                    if (layer.AlignToSurface)
                    {
                        instance.transform.up = hit.normal;
                    }

                    instanceCount += 1;
                }
            }
#if UNITY_EDITOR
            GCommonGUI.ClearProgressBar();
#endif
        }

        public void Internal_RenderBrushes(RenderTexture[] brushes, GStylizedTerrain t, Vector2[] uvPoints)
        {
            for (int i = 0; i < brushes.Length; ++i)
            {
                GStampLayerMaskRenderer.Render(
                    brushes[i],
                    Layers[i],
                    t,
                    Matrix4x4.TRS(Position, Rotation, Scale),
                    Mask,
                    falloffTexture,
                    uvPoints, 
                    EnableTerrainMask);
            }
        }

        public void Internal_UpdateFalloffTexture()
        {
            if (falloffTexture != null)
                GUtilities.DestroyObject(falloffTexture);
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }

        public void Internal_UpdateLayerTransitionTextures()
        {
            for (int i = 0; i < Layers.Count; ++i)
            {
                Layers[i].UpdateCurveTextures();
            }
        }

        public Vector3[] GetQuad()
        {
            Matrix4x4 matrix = Matrix4x4.TRS(Position, Rotation, Scale);
            Vector3[] quad = new Vector3[4]
            {
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0.5f, 0, -0.5f))
            };

            return quad;
        }

        public void GetQuad(Vector3[] quad)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(Position, Rotation, Scale);
            quad[0] = matrix.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f));
            quad[1] = matrix.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f));
            quad[2] = matrix.MultiplyPoint(new Vector3(0.5f, 0, 0.5f));
            quad[3] = matrix.MultiplyPoint(new Vector3(0.5f, 0, -0.5f));
        }

        private void GetUvPoints(GStylizedTerrain t, Vector3[] worldPoint, Vector2[] uvPoint)
        {
            for (int i = 0; i < uvPoints.Length; ++i)
            {
                uvPoints[i] = t.WorldPointToUV(worldPoints[i]);
            }
        }

        public void GetBox(Vector3[] box)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(Position, Rotation, Scale);
            box[0] = matrix.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f));
            box[1] = matrix.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f));
            box[2] = matrix.MultiplyPoint(new Vector3(0.5f, 0, 0.5f));
            box[3] = matrix.MultiplyPoint(new Vector3(0.5f, 0, -0.5f));
            box[4] = matrix.MultiplyPoint(new Vector3(-0.5f, 1, -0.5f));
            box[5] = matrix.MultiplyPoint(new Vector3(-0.5f, 1, 0.5f));
            box[6] = matrix.MultiplyPoint(new Vector3(0.5f, 1, 0.5f));
            box[7] = matrix.MultiplyPoint(new Vector3(0.5f, 1, -0.5f));
        }

        private RenderTexture GetRenderTexture(string key)
        {
            int resolution = MaskResolution;
            if (!TempRt.ContainsKey(key) ||
                TempRt[key] == null)
            {
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.wrapMode = TextureWrapMode.Clamp;
                TempRt[key] = rt;
            }
            else if (TempRt[key].width != resolution || TempRt[key].height != resolution)
            {
                TempRt[key].Release();
                Object.DestroyImmediate(TempRt[key]);
                RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.wrapMode = TextureWrapMode.Clamp;
                TempRt[key] = rt;
            }

            return TempRt[key];
        }

        public void ClearObjects()
        {
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (groupId < 0 ||
                    (groupId >= 0 && groupId == t.GroupId))
                {
                    ClearObjects(t);
                }
            }
        }

        private void ClearObjects(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;
            Vector3 terrainSize = new Vector3(
                t.TerrainData.Geometry.Width,
                t.TerrainData.Geometry.Height,
                t.TerrainData.Geometry.Length);
            Vector3 scale = new Vector3(
                GUtilities.InverseLerpUnclamped(0, terrainSize.x, Scale.x),
                GUtilities.InverseLerpUnclamped(0, terrainSize.y, Scale.y),
                GUtilities.InverseLerpUnclamped(0, terrainSize.z, Scale.z));
            Matrix4x4 matrix = Matrix4x4.TRS(
                t.WorldPointToNormalized(Position),
                Rotation,
                scale);
            Matrix4x4 normalizeToStamp = matrix.inverse;

            GSpawner.DestroyIf(t, (g) =>
            {
                Vector3 normalizePos = t.WorldPointToNormalized(g.transform.position);
                Vector3 stampSpacePos = normalizeToStamp.MultiplyPoint(normalizePos);
                return
                    stampSpacePos.x >= -0.5f && stampSpacePos.x <= 0.5f &&
                    stampSpacePos.y >= 0f && stampSpacePos.y <= 1f &&
                    stampSpacePos.z >= -0.5f && stampSpacePos.z <= 0.5f;
            });
        }
    }
}