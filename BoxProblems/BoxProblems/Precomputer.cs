using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal static class Precomputer
    {
        private static readonly Dictionary<Point, (short[,] distanceMap, Direction[,] pathMap)> PrecomputedDistancesAndPaths = new Dictionary<Point, (short[,] distanceMap, Direction[,] pathMap)>();

        public static short[,] GetDistanceMap(bool[,] walls, Point start)
        {
            if (PrecomputedDistancesAndPaths.TryGetValue(start, out (short[,] distanceMap, Direction[,] pathMap) data))
            {
                return data.distanceMap;
            }

            var newData = GraphSearcher.GetDistanceBFS(walls, start);
            PrecomputedDistancesAndPaths.Add(start, data);

            return newData.Value.distanceMap;
        }

        public static Direction[,] GetPathMap(bool[,] walls, Point start)
        {
            if (PrecomputedDistancesAndPaths.TryGetValue(start, out (short[,] distanceMap, Direction[,] pathMap) data))
            {
                return data.pathMap;
            }

            var newData = GraphSearcher.GetDistanceBFS(walls, start);
            PrecomputedDistancesAndPaths.Add(start, data);

            return newData.Value.pathMap;
        }
    }
}
