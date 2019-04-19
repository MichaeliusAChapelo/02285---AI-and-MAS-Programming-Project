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
            Dictionary<GoalNode, Dictionary<GoalNode, List<GoalNode>>> nodeGraphs = new Dictionary<GoalNode, Dictionary<GoalNode, List<GoalNode>>>();
            foreach (var goal in level.Goals)
            {
                GoalNode node = goalGraph.GetNodeFromPosition(goal.Pos);
                nodeGraphs.Add(node, CreateDirectedEdgesToStart(node));
            }

            HashSet<Entity> toIgnore = new HashSet<Entity>();
            while (toIgnore.Count < level.Goals.Length)
            {
                Dictionary<GoalNode, float> nodeCounter = new Dictionary<GoalNode, float>();
                foreach (var inode in goalGraph.Nodes)
                {
                    var node = (GoalNode)inode;
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

                    GoalNode start = goalGraph.GetNodeFromPosition(goal.Pos);

                    Dictionary<GoalNode, int> shortestPathsVisitedNodesCount = new Dictionary<GoalNode, int>();
                    int pathsCount = 0;
                    int boxCount = 0;
                    foreach (Entity box in level.GetBoxes())
                    {
                        if (box.Type == goal.Type)
                        {
                            GoalNode goalNode = goalGraph.GetNodeFromPosition(box.Pos);
                            pathsCount += GetShortestPathsData(goalNode, nodeGraphs[start], shortestPathsVisitedNodesCount, toIgnore);
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

        private Dictionary<GoalNode, List<GoalNode>> CreateDirectedEdgesToStart(GoalNode startNode)
        {
            Queue<GoalNode> frontier = new Queue<GoalNode>();
            Dictionary<GoalNode, List<GoalNode>> childToParent = new Dictionary<GoalNode, List<GoalNode>>();
            Dictionary<GoalNode, int> nodeDepths = new Dictionary<GoalNode, int>();

            frontier.Enqueue(startNode);
            childToParent.Add(startNode, null);
            nodeDepths.Add(startNode, 0);

            while (frontier.Count > 0)
            {
                GoalNode leaf = frontier.Dequeue();
                int depth = nodeDepths[leaf];

                if (leaf.Value.EntType == EntityType.BOX)
                {
                    continue;
                }

                foreach (var child in leaf.Edges)
                {
                    GoalNode goalChild = (GoalNode)child.End;
                    if (nodeDepths.ContainsKey(goalChild) && nodeDepths[goalChild] <= depth)
                    {
                        continue;
                    }

                    if (!childToParent.ContainsKey(goalChild))
                    {
                        childToParent.Add(goalChild, new List<GoalNode>() { leaf });
                        frontier.Enqueue(goalChild);
                        nodeDepths.Add(goalChild, depth + 1);
                    }
                    else
                    {
                        childToParent[goalChild].Add(leaf);
                    }
                }
            }

            return childToParent;
        }

        private int GetShortestPathsData(GoalNode goal, Dictionary<GoalNode, List<GoalNode>> childToParent, Dictionary<GoalNode, int> shortestPathsVisitedNodes, HashSet<Entity> toIgnore)
        {
            int shortestPathsCount = 1;
            Stack<GoalNode> backtrackPaths = new Stack<GoalNode>();
            backtrackPaths.Push(goal);

            while (backtrackPaths.Count > 0)
            {
                GoalNode pathEnd = backtrackPaths.Pop();

                List<GoalNode> parents = childToParent[pathEnd];
                int validParentsCount = 0;
                if (parents != null)
                {
                    foreach (var node in parents)
                    {
                        if (!toIgnore.Contains(node.Value.Ent))
                        {
                            backtrackPaths.Push(node);
                            validParentsCount++;
                        }
                    }
                    shortestPathsCount += validParentsCount - 1;
                }
                else
                {
                    validParentsCount = 1;
                }

                if (pathEnd.Value.EntType == EntityType.GOAL)
                {
                    shortestPathsVisitedNodes.TryAdd(pathEnd, 0);
                    shortestPathsVisitedNodes[pathEnd] += validParentsCount;
                }
            }

            return shortestPathsCount;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, PriorityLayers.Select(x => string.Join(' ', (object[])x)));
        }
    }
}
