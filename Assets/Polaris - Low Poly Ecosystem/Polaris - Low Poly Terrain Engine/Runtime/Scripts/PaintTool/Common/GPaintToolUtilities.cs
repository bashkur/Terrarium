using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pinwheel.Griffin.PaintTool
{
    public static class GPaintToolUtilities
    {
        public static List<GStylizedTerrain> CheckBrushOverlap(
            int groupId,
            Vector3[] worldPointCorners)
        {
            List<GStylizedTerrain> results = new List<GStylizedTerrain>();
            GCommon.ForEachTerrain(groupId, (t) =>
            {
                Rect dirtyRect = GUtilities.GetRectContainsPoints(worldPointCorners);
                if (dirtyRect.Overlaps(t.Rect))
                {
                    results.Add(t);
                }
            });

            return results;
        }

        public static void AddCustomSpawnFilter(List<Type> filters)
        {
            for (int i = 0; i < GSpawnFilter.AllFilters.Count; ++i)
            {
                Type t = GSpawnFilter.AllFilters[i];
                if (!IsBuiltinFilter(t))
                    filters.Add(t);
            }
        }

        private static bool IsBuiltinFilter(Type t)
        {
            return t == typeof(GAlignToSurfaceFilter) ||
                    t == typeof(GHeightConstraintFilter) ||
                    t == typeof(GRotationRandomizeFilter) ||
                    t == typeof(GScaleClampFilter) ||
                    t == typeof(GScaleRandomizeFilter) ||
                    t == typeof(GSlopeConstraintFilter);
        }
    }
}
