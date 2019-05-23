using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;
using System.Threading;

namespace BoxProblems.Graphing
{
    internal readonly struct GoalPriorityLayer
    {
        public readonly HashSet<Entity> Goals;

        public GoalPriorityLayer(HashSet<Entity> goals)
        {
            this.Goals = goals;
        }
    }

    internal class GoalPriority
    {
        public readonly List<GoalNode[]> PriorityLayers = new List<GoalNode[]>();

        public GoalPriority(Level level, GoalGraph goalGraph, CancellationToken cancel)
        {
            CreateGoalPriority(level, goalGraph, cancel);
        }

        private void CreateGoalPriority(Level level, GoalGraph goalGraph, CancellationToken cancel)
        {
            Dictionary<GoalNode, Dictionary<GoalNode, List<GoalNode>>> nodeGraphs = new Dictionary<GoalNode, Dictionary<GoalNode, List<GoalNode>>>();
            foreach (var goal in level.Goals)
            {
                GoalNode node = goalGraph.GetGoalNodeFromPosition(goal.Ent.Pos);
                nodeGraphs.Add(node, CreateDirectedEdgesToStart(node));
            }

            Graph groupedGraph = Graph.CreateSimplifiedGraph<EmptyEdgeInfo>(goalGraph);
            //GraphShower.ShowGraph(groupedGraph);

            List<List<GoalNode>> boxGroups = new List<List<GoalNode>>();
            foreach (var iNode in groupedGraph.Nodes)
            {
                var node = (Node<NodeGroup, EmptyEdgeInfo>)iNode;
                var boxes = node.Value.Nodes.Cast<GoalNode>().Where(x => x.Value.EntType == EntityType.BOX);
                if (boxes.Any())
                {
                    boxGroups.Add(boxes.ToList());
                }
            }

            HashSet<Entity> toIgnore = new HashSet<Entity>();
            HashSet<GoalNode> toIgnoreNodes = new HashSet<GoalNode>();
            Dictionary<GoalNode, (int pathsCount, Dictionary<GoalNode, int> pathNodes)> cachedPathResults = new Dictionary<GoalNode, (int pathsCount, Dictionary<GoalNode, int> pathNodes)>();
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

                foreach (Goal goal in level.Goals)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (toIgnore.Contains(goal))
                    {
                        continue;
                    }

                    GoalNode start = goalGraph.GetGoalNodeFromPosition(goal.Pos);

                    (int pathsCount, Dictionary<GoalNode, int> pathNodes) pathResult;
                    if (!cachedPathResults.TryGetValue(start, out pathResult) || toIgnoreNodes.Any(x => pathResult.pathNodes.ContainsKey(x)))
                    {
                        Dictionary<GoalNode, int> shortestPathsVisitedNodesCount = new Dictionary<GoalNode, int>();
                        int pathsCount = 0;
                        foreach (var boxGroup in boxGroups)
                        {
                            cancel.ThrowIfCancellationRequested();
                            int boxesWithSameType = 0;
                            foreach (var boxNode in boxGroup)
                            {
                                if (boxNode.Value.Ent.Type == goal.Type)
                                {
                                    boxesWithSameType++;
                                }
                            }
                            if (boxesWithSameType > 0)
                            {
                                pathsCount += GetShortestPathsData(boxGroup.First(), nodeGraphs[start], shortestPathsVisitedNodesCount, toIgnore, boxesWithSameType);
                            }
                        }

                        pathResult = (pathsCount, shortestPathsVisitedNodesCount);
                        cachedPathResults[start] = pathResult;
                    }

                    foreach (var pathNode in pathResult.pathNodes)
                    {
                        nodeCounter[pathNode.Key] += (1f / (pathResult.pathsCount)) * pathNode.Value;
                    }
                }

                GoalNode[] newPriorityGroup = nodeCounter.GroupBy(x => x.Value).OrderBy(x => x.First().Value).First().Select(x => x.Key).ToArray();
                PriorityLayers.Add(newPriorityGroup);
                foreach (var priorityNode in newPriorityGroup)
                {
                    toIgnore.Add(priorityNode.Value.Ent);
                    toIgnoreNodes.Add(goalGraph.GetGoalNodeFromPosition(priorityNode.Value.Ent.Pos));
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

        private int GetShortestPathsData(GoalNode goal, Dictionary<GoalNode, List<GoalNode>> childToParent, Dictionary<GoalNode, int> shortestPathsVisitedNodes, HashSet<Entity> toIgnore, int multiplier)
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
                    shortestPathsVisitedNodes[pathEnd] += validParentsCount * multiplier;
                }
            }

            return shortestPathsCount * multiplier;
        }

        public LinkedList<GoalPriorityLayer> GetAsLinkedLayers()
        {
            LinkedList<GoalPriorityLayer> layers = new LinkedList<GoalPriorityLayer>();
            foreach (var layer in PriorityLayers)
            {
                layers.AddLast(new GoalPriorityLayer(layer.Select(x => x.Value.Ent).ToHashSet()));
            }

            return layers;
        }

        public string ToLevelString(Level level)
        {
            List<Entity> priorityEntities = new List<Entity>();
            int priority = 1;
            for (int i = 0; i < PriorityLayers.Count; i++)
            {
                priorityEntities.AddRange(PriorityLayers[i].Select(x => new Entity(x.Value.Ent.Pos, 0, (char)(priority + '0'))));
                priority++;
            }

            State priorityState = new State(null, priorityEntities.ToArray(), 0);
            return level.StateToString(priorityState);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, PriorityLayers.Select(x => string.Join(' ', (object[])x)));
        }
    }
}
