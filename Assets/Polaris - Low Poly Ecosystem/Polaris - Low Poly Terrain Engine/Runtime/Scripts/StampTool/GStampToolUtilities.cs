using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin.StampTool
{
    public static class GStampToolUtilities
    {
        public static List<GStylizedTerrain> CheckOverlap(int groupId, Rect stamperRect)
        {
            List<GStylizedTerrain> terrains = new List<GStylizedTerrain>();
            GCommon.ForEachTerrain(groupId, (t) =>
            {
                if (stamperRect.Overlaps(t.Rect))
                {
                    terrains.Add(t);
                }
            });
            return terrains;
        }
    }
}
