using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal static class Precomputer
    {
        private static readonly Dictionary<Point, (short[,] distanceMap, Direction[,] pathMap)> PrecomputedDistancesAndPaths = new Dictionary<Point, (short[,] distanceMap, Direction[,] pathMap)>();

        public static short[,] GetDistanceMap(bool[,] walls, Point start, bool getFromCache = true)
        {
            if (getFromCache && PrecomputedDistancesAndPaths.TryGetValue(start, out (short[,] distanceMap, Direction[,] pathMap) data))
            {
                return data.distanceMap;
            }

            var newData = GraphSearcher.GetDistanceBFS(walls, start);
            if (getFromCache)
            {
                PrecomputedDistancesAndPaths.Add(start, newData.Value);
            }

            return newData.Value.distanceMap;
        }

        public static Direction[,] GetPathMap(bool[,] walls, Point start, bool getFromCache = true)
        {
            if (getFromCache && PrecomputedDistancesAndPaths.TryGetValue(start, out (short[,] distanceMap, Direction[,] pathMap) data))
            {
                return data.pathMap;
            }

            var newData = GraphSearcher.GetDistanceBFS(walls, start);
            if (getFromCache)
            {
                PrecomputedDistancesAndPaths.Add(start, newData.Value);
            }

            return newData.Value.pathMap;
        }
    }
}
