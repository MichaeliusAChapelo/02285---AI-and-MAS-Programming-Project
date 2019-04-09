using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

namespace BoxProblems.Graphing
{
    internal class GoalPriority
    {
        public readonly List<GoalNode[]> PriorityLayers = new List<GoalNode[]>();

        public GoalPriority(Level level, GoalGraph goalGraph)
        {
            CreateGoalPriority(level, goalGraph);
        }

        private void CreateGoalPriority(Level level, GoalGraph goalGraph)
        {
            HashSet<Entity> toIgnore = new HashSet<Entity>();
            while (toIgnore.Count < level.Goals.Length)
            {
                Dictionary<GoalNode, float> nodeCounter = new Dictionary<GoalNode, float>();
                foreach (var node in goalGraph.Nodes)
                {
                    if (node.Value.EntType == EntityType.GOAL && !toIgnore.Contains(node.Value.Ent))
                    {
                        nodeCounter.Add((GoalNode)node, 0);
                    }
                }

                foreach (Entity goal in level.Goals)
                {
                    if (toIgnore.Contains(goal))
                    {
                        continue;
                    }

                    GoalNode start = (GoalNode)goalGraph.GetNodeFromPosition(goal.Pos);

                    Dictionary<GoalNode, int> shortestPathsVisitedNodesCount = new Dictionary<GoalNode, int>();
                    int pathsCount = 0;
                    int boxCount = 0;
                    foreach (Entity box in level.GetBoxes())
                    {
                        if (box.Type == goal.Type)
                        {
                            pathsCount += AddToNodeCounterForShortestPath(start, box, nodeCounter, shortestPathsVisitedNodesCount, toIgnore);
                            boxCount++;
                        }
                    }

                    foreach (var pathNode in shortestPathsVisitedNodesCount)
                    {
                        nodeCounter[pathNode.Key] += (1f / (pathsCount)) * pathNode.Value;
                    }
                    
                }

                GoalNode[] newPriorityGroup = nodeCounter.GroupBy(x => x.Value).OrderBy(x => x.First().Value).First().Select(x => x.Key).ToArray();
                PriorityLayers.Add(newPriorityGroup);
                foreach (var priorityNode in newPriorityGroup)
                {
                    toIgnore.Add(priorityNode.Value.Ent);
                }
            }
        }

        private int AddToNodeCounterForShortestPath(GoalNode startNode, Entity goal, Dictionary<GoalNode, float> nodeCounter, Dictionary<GoalNode, int> shortestPathsVisitedNodes, HashSet<Entity> toIgnore)
        {
            int minLength = int.MaxValue;
            Queue<GoalNode> frontier = new Queue<GoalNode>();
            Dictionary<GoalNode, List<GoalNode>> childToParent = new Dictionary<GoalNode, List<GoalNode>>();
            HashSet<GoalNode> exploredSet = new HashSet<GoalNode>();
            Stack<GoalNode> backtrackPaths = new Stack<GoalNode>();
            int shortestPathsCount = 0;

            frontier.Enqueue(startNode);
            childToParent.Add(startNode, null);

            //depth implementation copied from https://github.com/TheAIBot/Bioly/blob/master/BiolyCompiler/Routing/Router.cs#L198
            int depthNodeCount = 1;
            int nextDepthNodeCount = 0;
            int depth = 0;

            while (frontier.Count > 0)
            {
                //Handles setting the depth of the bfs search.
                if (depthNodeCount == 0)
                {
                    depthNodeCount = nextDepthNodeCount;
                    nextDepthNodeCount = 0;
                    depth++;
                }
                depthNodeCount--;

                //Only the shortests paths are allowed
                //If the depth is higher than the shortest path then
                //all shortest paths has been found
                if (depth > minLength)
                {
                    break;
                }

                GoalNode leaf = frontier.Dequeue();

                bool foundGoal = false;
                foreach (var edge in leaf.Edges)
                {
                    if (edge.End.Value.Ent == goal)
                    {
                        foundGoal = true;
                    }
                }
                if (foundGoal)
                {
                    shortestPathsCount++;
                    minLength = depth;

                    backtrackPaths.Clear();
                    backtrackPaths.Push(leaf);

                    while (backtrackPaths.Count > 0)
                    {
                        GoalNode pathEnd = backtrackPaths.Pop();
                        //Console.WriteLine(pathEnd);

                        if (childToParent.TryGetValue(pathEnd, out List<GoalNode> parents))
                        {
                            if (parents != null)
                            {
                                foreach (var node in parents)
                                {
                                    backtrackPaths.Push(node);
                                }
                                shortestPathsCount += parents.Count - 1;

                                if (pathEnd.Value.EntType == EntityType.GOAL)
                                {
                                    if (!shortestPathsVisitedNodes.ContainsKey(pathEnd))
                                    {
                                        shortestPathsVisitedNodes.Add(pathEnd, 0);
                                    }
                                    shortestPathsVisitedNodes[pathEnd] += parents.Count;
                                }
                            }
                            else
                            {
                                if (!shortestPathsVisitedNodes.ContainsKey(pathEnd))
                                {
                                    shortestPathsVisitedNodes.Add(pathEnd, 0);
                                }
                                shortestPathsVisitedNodes[pathEnd] += 1;
                            }
                        }
                    }
                    continue;
                    //Console.WriteLine();
                }

                if (leaf.Value.EntType == EntityType.BOX)
                {
                    continue;
                }

                foreach (var child in leaf.Edges)
                {
                    if (exploredSet.Contains(child.End) || toIgnore.Contains(child.End.Value.Ent))
                    {
                        continue;
                    }
                    
                    GoalNode goalChild = (GoalNode)child.End;
                    if (!childToParent.ContainsKey(goalChild))
                    {
                        childToParent.Add(goalChild, new List<GoalNode>() { leaf });
                        frontier.Enqueue(goalChild);
                        nextDepthNodeCount++;
                    }
                    else
                    {
                        childToParent[goalChild].Add(leaf);
                    }
                }
                exploredSet.Add(leaf);
            }

            return shortestPathsCount;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, PriorityLayers.Select(x => string.Join(' ', (object[])x)));
        }
    }
}
