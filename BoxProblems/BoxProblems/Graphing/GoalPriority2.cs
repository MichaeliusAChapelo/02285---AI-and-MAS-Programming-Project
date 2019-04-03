using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    internal static class GoalPriority2
    {
        public static Dictionary<GoalNode, float> GetGoalPriority(Level level, GoalGraph goalGraph)
        {
            Dictionary<GoalNode, float> nodeCounter = new Dictionary<GoalNode, float>();
            goalGraph.Nodes.Where(x => x.Value.EntType == EntityType.GOAL)
                           .ToList()
                           .ForEach(x => nodeCounter.Add((GoalNode)x, 0));

            HashSet<GoalNode> allowedNodes = new HashSet<GoalNode>();
            List<Entity> correctBoxes = new List<Entity>();
            foreach (Entity goal in level.Goals)
            {
                GoalNode start = (GoalNode)goalGraph.Nodes.Single(x => x.Value.Ent.Pos == goal.Pos);

                allowedNodes.Clear();
                correctBoxes.Clear();
                foreach (Entity box in level.GetBoxes())
                {
                    if (goal.Type == box.Type)
                    {
                        correctBoxes.Add(box);
                    }
                }

                AddToNodeCounterForShortestPath(start, x => correctBoxes.Contains(x.Value.Ent), correctBoxes.Count, nodeCounter);
            }

            return nodeCounter;
        }

        private static void AddToNodeCounterForShortestPath(GoalNode startNode, Func<GoalNode, bool> goalCondition, int boxCount, Dictionary<GoalNode, float> nodeCounter)
        {
            int minLength = int.MaxValue;
            Queue<GoalNode> frontier = new Queue<GoalNode>();
            Dictionary<GoalNode, List<GoalNode>> childToParent = new Dictionary<GoalNode, List<GoalNode>>();
            HashSet<GoalNode> exploredSet = new HashSet<GoalNode>();
            HashSet<GoalNode> shortestPathsVisitedNodes = new HashSet<GoalNode>();
            int shortestPathsCount = 0;

            frontier.Enqueue(startNode);
            childToParent.Add(startNode, null);

            //depth implementation copied from https://github.com/TheAIBot/Bioly/blob/master/BiolyCompiler/Routing/Router.cs#L198
            int depthNodeCount = 1;
            int nextDepthNodeCount = 0;
            int depth = 0;

            while (frontier.Count > 0)
            {
                if (depthNodeCount == 0)
                {
                    depthNodeCount = nextDepthNodeCount;
                    nextDepthNodeCount = 0;
                    depth++;
                }
                depthNodeCount--;

                if (depth > minLength)
                {
                    break;
                }

                GoalNode leaf = frontier.Dequeue();
                if ( goalCondition(leaf))
                {
                    shortestPathsCount++;
                    minLength = depth;
                    Stack<GoalNode> toSee = new Stack<GoalNode>();
                    toSee.Push(leaf);

                    while (toSee.Count > 0)
                    {
                        GoalNode pathEnd = toSee.Pop();
                        List<GoalNode> parents;
                        if (!childToParent.TryGetValue(pathEnd, out parents))
                        {
                            continue;
                        }

                        //Console.WriteLine(pathEnd.ToString());
                        if (pathEnd.Value.EntType == EntityType.GOAL)
                        {
                            shortestPathsVisitedNodes.Add(pathEnd);
                        }
                        if (parents == null)
                        {
                            continue;
                        }
                        foreach (var node in parents)
                        {
                            toSee.Push(node);
                        }
                        shortestPathsCount += parents.Count - 1;
                    }

                    //Console.WriteLine();
                }

                foreach (var child in leaf.Edges)
                {
                    if (leaf.Value.EntType == EntityType.BOX && child.End.Value.EntType == EntityType.BOX)
                    {
                        continue;
                    }

                    if (exploredSet.Contains(child.End))
                    {
                        continue;
                    }

                    GoalNode goalChild = (GoalNode)child.End;
                    if (!childToParent.ContainsKey(goalChild))
                    {
                        childToParent.Add(goalChild, new List<GoalNode>());
                    }
                    else
                    {
                        childToParent[goalChild].Add(leaf);
                        continue;
                    }

                    if (childToParent[goalChild] == null)
                    {
                        continue;
                    }

                    frontier.Enqueue(goalChild);
                    if (!childToParent[goalChild].Contains(leaf))
                    {
                        childToParent[goalChild].Add(leaf);
                    }
                    nextDepthNodeCount++;
                }
                exploredSet.Add(leaf);
            }

            foreach (var pathNode in shortestPathsVisitedNodes)
            {
                nodeCounter[pathNode] = 1f / (shortestPathsCount * boxCount);
            }
        }
    }
}
