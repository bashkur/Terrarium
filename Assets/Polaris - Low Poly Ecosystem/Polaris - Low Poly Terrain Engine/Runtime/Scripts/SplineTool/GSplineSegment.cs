using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [System.Serializable]
    public class GSplineSegment
    {
        [SerializeField]
        private int startIndex;
        public int StartIndex
        {
            get
            {
                return startIndex;
            }
            set
            {
                startIndex = value;
            }
        }

        [SerializeField]
        private int endIndex;
        public int EndIndex
        {
            get
            {
                return endIndex;
            }
            set
            {
                endIndex = value;
            }
        }

        [SerializeField]
        private Vector3 startTangent;
        public Vector3 StartTangent
        {
            get
            {
                return startTangent;
            }
            set
            {
                startTangent = value;
            }
        }

        [SerializeField]
        private Vector3 endTangent;
        public Vector3 EndTangent
        {
            get
            {
                return endTangent;
            }
            set
            {
                endTangent = value;
            }
        }
    }
}
