using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class GSplineCreator : MonoBehaviour
    {
        public delegate void SplineChangedHandler(GSplineCreator sender);
        public static event SplineChangedHandler Editor_SplineChanged;

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
        private Vector3 positionOffset;
        public Vector3 PositionOffset
        {
            get
            {
                return positionOffset;
            }
            set
            {
                positionOffset = value;
            }
        }

        [SerializeField]
        private Quaternion initialRotation;
        public Quaternion InitialRotation
        {
            get
            {
                return initialRotation;
            }
            set
            {
                initialRotation = value;
            }
        }

        [SerializeField]
        private Vector3 initialScale;
        public Vector3 InitialScale
        {
            get
            {
                return initialScale;
            }
            set
            {
                initialScale = value;
            }
        }

        [SerializeField]
        private int smoothness;
        public int Smoothness
        {
            get
            {
                return smoothness;
            }
            set
            {
                smoothness = Mathf.Max(2, value);
            }
        }

        [SerializeField]
        private float width;
        public float Width
        {
            get
            {
                return width;
            }
            set
            {
                width = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private float falloffWidth;
        public float FalloffWidth
        {
            get
            {
                return falloffWidth;
            }
            set
            {
                falloffWidth = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private GSpline spline;
        public GSpline Spline
        {
            get
            {
                if (spline == null)
                {
                    spline = new GSpline();
                }
                return spline;
            }
            set
            {
                spline = value;
            }
        }

#if UNITY_EDITOR
        private List<Vector4> vertices;
        public List<Vector4> Editor_Vertices
        {
            get
            {
                if (vertices == null)
                    vertices = new List<Vector4>();
                return vertices;
            }
            set
            {
                vertices = value;
            }
        }
#endif

        public void Reset()
        {
            PositionOffset = Vector3.zero;
            InitialRotation = Quaternion.identity;
            InitialScale = Vector3.one;
            Smoothness = 20;
            Width = 1;
            FalloffWidth = 1;
        }

        public List<Vector4> GenerateVerticesWithFalloff()
        {
            List<Vector4> vertices = new List<Vector4>();
            List<GSplineAnchor> anchors = Spline.Anchors;
            List<GSplineSegment> segments = Spline.Segments;

            for (int sIndex = 0; sIndex < segments.Count; ++sIndex)
            {
                float tStep = 1f / (Smoothness - 1);
                for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
                {
                    float t0 = tIndex * tStep;
                    Vector3 translation0 = Spline.EvaluatePosition(sIndex, t0);
                    Quaternion rotation0 = Spline.EvaluateRotation(sIndex, t0);
                    Vector3 scale0 = Spline.EvaluateScale(sIndex, t0);

                    float t1 = (tIndex + 1) * tStep;
                    Vector3 translation1 = Spline.EvaluatePosition(sIndex, t1);
                    Quaternion rotation1 = Spline.EvaluateRotation(sIndex, t1);
                    Vector3 scale1 = Spline.EvaluateScale(sIndex, t1);

                    Matrix4x4 matrix0 = transform.localToWorldMatrix * Matrix4x4.TRS(translation0, rotation0, scale0);
                    Matrix4x4 matrix1 = transform.localToWorldMatrix * Matrix4x4.TRS(translation1, rotation1, scale1);

                    Vector3 bl, tl, tr, br;
                    float halfWidth = Width * 0.5f;

                    if (FalloffWidth > 0)
                    {
                        //Left falloff
                        bl = matrix0.MultiplyPoint(new Vector3(-halfWidth - FalloffWidth, 0, 0));
                        tl = matrix1.MultiplyPoint(new Vector3(-halfWidth - FalloffWidth, 0, 0));
                        tr = matrix1.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                        br = matrix0.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 0));
                        vertices.Add(new Vector4(tl.x, tl.y, tl.z, 0));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 1));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 0));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 1));
                        vertices.Add(new Vector4(br.x, br.y, br.z, 1));
                    }

                    if (Width > 0)
                    {
                        //Center
                        bl = matrix0.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                        tl = matrix1.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                        tr = matrix1.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                        br = matrix0.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 1));
                        vertices.Add(new Vector4(tl.x, tl.y, tl.z, 1));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 1));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 1));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 1));
                        vertices.Add(new Vector4(br.x, br.y, br.z, 1));
                    }

                    if (FalloffWidth > 0)
                    {
                        //Right falloff
                        bl = matrix0.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                        tl = matrix1.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                        tr = matrix1.MultiplyPoint(new Vector3(halfWidth + FalloffWidth, 0, 0));
                        br = matrix0.MultiplyPoint(new Vector3(halfWidth + FalloffWidth, 0, 0));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 1));
                        vertices.Add(new Vector4(tl.x, tl.y, tl.z, 1));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 0));
                        vertices.Add(new Vector4(bl.x, bl.y, bl.z, 1));
                        vertices.Add(new Vector4(tr.x, tr.y, tr.z, 0));
                        vertices.Add(new Vector4(br.x, br.y, br.z, 0));
                    }
                }
            }

            return vertices;
        }

        public IEnumerable<Rect> SweepDirtyRect(GStylizedTerrain terrain)
        {
            if (terrain.TerrainData == null)
                return new List<Rect>();
            int gridSize = terrain.TerrainData.Geometry.ChunkGridSize;
            List<Rect> uvRects = new List<Rect>();
            for (int x = 0; x < gridSize; ++x)
            {
                for (int z = 0; z < gridSize; ++z)
                {
                    uvRects.Add(GCommon.GetUvRange(gridSize, x, z));
                }
            }

            HashSet<Rect> dirtyRects = new HashSet<Rect>();
            Vector3 terrainSize = new Vector3(
                terrain.TerrainData.Geometry.Width,
                terrain.TerrainData.Geometry.Height,
                terrain.TerrainData.Geometry.Length);
            float splineSize = Mathf.Max(1, Width + FalloffWidth * 2);
            Vector2 sweepRectSize = new Vector2(
                Mathf.InverseLerp(0, terrainSize.x, splineSize),
                Mathf.InverseLerp(0, terrainSize.z, splineSize));
            Rect sweepRect = new Rect();
            sweepRect.size = sweepRectSize;

            int segmentCount = Spline.Segments.Count;
            for (int sIndex = 0; sIndex < segmentCount; ++sIndex)
            {
                float tStep = 1f / (Smoothness - 1);
                for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
                {
                    float t = tIndex * tStep;
                    Vector3 worldPos = transform.TransformPoint(Spline.EvaluatePosition(sIndex, t));
                    Vector3 scale = transform.TransformVector(Spline.EvaluateScale(sIndex, t));
                    float maxScaleComponent = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    Vector3 normalizedPos = terrain.WorldPointToNormalized(worldPos);
                    sweepRect.center = new Vector2(normalizedPos.x, normalizedPos.z);
                    sweepRect.size = sweepRectSize * maxScaleComponent;
                    for (int rIndex = 0; rIndex < uvRects.Count; ++rIndex)
                    {
                        if (uvRects[rIndex].Overlaps(sweepRect))
                        {
                            dirtyRects.Add(uvRects[rIndex]);
                        }
                    }
                }
            }

            return dirtyRects;
        }

        public bool OverlapTest(GStylizedTerrain terrain)
        {
            Rect terrainRect = terrain.Rect;
            float splineSize = Mathf.Max(1, Width + FalloffWidth * 2);
            Vector2 sweepRectSize = Vector2.one * splineSize;
            Rect sweepRect = new Rect();
            sweepRect.size = sweepRectSize;

            int segmentCount = Spline.Segments.Count;
            for (int sIndex = 0; sIndex < segmentCount; ++sIndex)
            {
                float tStep = 1f / (Smoothness - 1);
                for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
                {
                    float t = tIndex * tStep;
                    Vector3 worldPos = Spline.EvaluatePosition(sIndex, t);
                    Vector3 scale = Spline.EvaluateScale(sIndex, t);
                    float maxScaleComponent = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    sweepRect.center = new Vector2(worldPos.x, worldPos.z);
                    sweepRect.size = sweepRectSize * maxScaleComponent;
                    if (sweepRect.Overlaps(terrainRect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void MarkSplineChanged(GSplineCreator sender)
        {
            if (Editor_SplineChanged != null)
            {
                Editor_SplineChanged.Invoke(sender);
            }
        }
    }
}
