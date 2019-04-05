using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal static class GraphSearcher
    {
        public readonly struct GoalFound<T>
        {
            public readonly T Value;
            public readonly bool IsGoal;

            public GoalFound(T value, bool isGoal)
            {
                this.Value = value;
                this.IsGoal = isGoal;
            }

        }

        public static List<T> GetReachedGoalsBFS<T>(Level level, Point start, Func<Point, GoalFound<T>> goalCondition)
        {
            Direction[] world = new Direction[level.Width * level.Height];
            Array.Fill(world, Direction.NONE);
            Queue<Point> frontier = new Queue<Point>();
            frontier.Enqueue(start);
            List<T> reachedGoals = new List<T>();

            int depthNodeCount = 1;
            int nextDepthNodeCount = 0;
            int depth = 0;

            while (frontier.Count > 0)
            {
                Point leafNode = frontier.Dequeue();

                if (depthNodeCount == 0)
                {
                    depthNodeCount = nextDepthNodeCount;
                    nextDepthNodeCount = 0;
                    depth++;
                }
                depthNodeCount--;

                var foundGoalInfo = goalCondition(leafNode);
                if (foundGoalInfo.IsGoal)
                {
                    reachedGoals.Add(foundGoalInfo.Value);
                }

                if (level.Walls[leafNode.X, leafNode.Y])
                {
                    continue;
                }

                //Add children
                Point north = leafNode + Direction.N.DirectionDelta();
                Point east = leafNode + Direction.E.DirectionDelta();
                Point south = leafNode + Direction.S.DirectionDelta();
                Point west = leafNode + Direction.W.DirectionDelta();

                int northIndex = level.PosToIndex(north);;
                int eastIndex = level.PosToIndex(east);
                int southIndex = level.PosToIndex(south);
                int westIndex = level.PosToIndex(west);

                if (world[northIndex] == Direction.NONE)
                {
                    world[northIndex] = Direction.S;
                    frontier.Enqueue(north);
                    nextDepthNodeCount++;
                }
                if (world[eastIndex] == Direction.NONE)
                {
                    world[eastIndex] = Direction.W;
                    frontier.Enqueue(east);
                    nextDepthNodeCount++;
                }
                if (world[southIndex] == Direction.NONE)
                {
                    world[southIndex] = Direction.N;
                    frontier.Enqueue(south);
                    nextDepthNodeCount++;
                }
                if (world[westIndex] == Direction.NONE)
                {
                    world[westIndex] = Direction.E;
                    frontier.Enqueue(west);
                    nextDepthNodeCount++;
                }
            }

            return reachedGoals;
        }

        public static (short[,] distanceMap, Direction[,] pathMap)? GetDistanceBFS(bool[,] walls, Point start)
        {
            int width = walls.GetLength(0);
            int height = walls.GetLength(1);
            short[,] distances = new short[width, height];
            Direction[,] world = new Direction[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    world[x, y] = Direction.NONE;
                }
            }

            Queue<Point> frontier = new Queue<Point>();
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                Point leafNode = frontier.Dequeue();

                //can't search outside the walls of the level so if it did anyway then that means
                //that the start point is outside the walls of the level and therefore this
                //distance map is useless.
                if (leafNode.X == 0 || leafNode.Y == 0 || leafNode.X == width - 1 || leafNode.Y == height - 1)
                {
                    return null;
                }

                //Add children
                Point north = leafNode + Direction.N.DirectionDelta();
                Point east = leafNode + Direction.E.DirectionDelta();
                Point south = leafNode + Direction.S.DirectionDelta();
                Point west = leafNode + Direction.W.DirectionDelta();

                if (!walls[north.X, north.Y] &&
                    world[north.X, north.Y] == Direction.NONE)
                {
                    world[north.X, north.Y] = Direction.S;
                    distances[north.X, north.Y] = (short)(distances[leafNode.X, leafNode.Y] + 1);
                    frontier.Enqueue(north);
                }
                if (!walls[east.X, east.Y] &&
                    world[east.X, east.Y] == Direction.NONE)
                {
                    world[east.X, east.Y] = Direction.W;
                    distances[east.X, east.Y] = (short)(distances[leafNode.X, leafNode.Y] + 1);
                    frontier.Enqueue(east);
                }
                if (!walls[south.X, south.Y] &&
                    world[south.X, south.Y] == Direction.NONE)
                {
                    world[south.X, south.Y] = Direction.N;
                    distances[south.X, south.Y] = (short)(distances[leafNode.X, leafNode.Y] + 1);
                    frontier.Enqueue(south);
                }
                if (!walls[west.X, west.Y] &&
                    world[west.X, west.Y] == Direction.NONE)
                {
                    world[west.X, west.Y] = Direction.E;
                    distances[west.X, west.Y] = (short)(distances[leafNode.X, leafNode.Y] + 1);
                    frontier.Enqueue(west);
                }
            }

            return (distances, world);
        }
    }
}
