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

        public static short[,] GetDistanceHeuristicsMap(bool[,] walls, Point start, Func<Point, int> heuristic, bool getFromCache = true)
        {
            if (getFromCache && PrecomputedDistancesAndPaths.TryGetValue(start, out (short[,] distanceMap, Direction[,] pathMap) data))
            {
                return data.distanceMap;
            }

            var newData = GraphSearcher.GetDistanceHeuristicBFS(walls, start, heuristic);
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

        public static Point[] GetPath(Level level, Point start, Point end, bool getFromCache = true)
        {
            if (start == end)
            {
                return new Point[] { end };
            }

            var pathData = GraphSearcher.GetDistanceBFS(level.Walls, end).Value;
            int distance = pathData.distanceMap[start.X, start.Y];
            Point[] path = new Point[distance + 1];
            Point currentPos = start;
            for (int i = 0; i < distance; i++)
            {
                path[i] = currentPos;
                Direction dir = pathData.pathMap[currentPos.X, currentPos.Y];
                currentPos = currentPos + dir.DirectionDelta();
            }
            path[path.Length - 1] = currentPos;

            return path;
        }
    }
}
