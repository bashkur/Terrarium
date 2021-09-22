using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin.SplineTool
{
    public static class GSplineToolUtilities
    {
        public static List<GStylizedTerrain> OverlapTest(int groupId, GSplineCreator spline)
        {
            List<GStylizedTerrain> terrains = new List<GStylizedTerrain>();
            GCommon.ForEachTerrain(groupId, (t) =>
            {
                if (spline.OverlapTest(t))
                {
                    terrains.Add(t);
                }
            });

            return terrains;
        }
    }
}
