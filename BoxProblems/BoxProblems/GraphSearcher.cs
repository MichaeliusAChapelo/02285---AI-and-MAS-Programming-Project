using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal static class GraphSearcher
    {
        public static List<Point> GetReachedGoalsBFS(Level level, Point start, List<Point> goals)
        {
            Direction[] world = new Direction[level.Width * level.Height];
            Array.Fill(world, Direction.NONE);
            Queue<Point> frontier = new Queue<Point>();
            frontier.Enqueue(start);
            List<Point> reachedGoals = new List<Point>();

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

                for (int i = 0; i < goals.Count; i++)
                {
                    if (leafNode == goals[i])
                    {
                        reachedGoals.Add(leafNode);
                        break;
                    }
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
    }
}
