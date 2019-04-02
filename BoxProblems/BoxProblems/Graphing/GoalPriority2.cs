using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    internal class GoalPriority2
    {
        public GoalPriority2(Level level, GoalGraph goalGraph)
        {
            Dictionary<GoalNode, int> nodeCounter = new Dictionary<GoalNode, int>();
            goalGraph.Nodes.ForEach(x => nodeCounter.Add((GoalNode)x, 0));

            foreach (Entity goal in level.Goals)
            {
                HashSet<GoalNode> allowedNodes = new HashSet<GoalNode>();
                foreach (Entity box in level.GetBoxes())
                {
                    if (goal.Type == box.Type)
                    {
                        GoalNode start = (GoalNode)goalGraph.Nodes.Single(x => x.Value.Ent.Pos == goal.Pos);
                        GoalNode end = (GoalNode)goalGraph.Nodes.Single(x => x.Value.Ent.Pos == box.Pos);

                        AddToNodeCounterForShortestPath(start, end, nodeCounter);
                    }
                }
            }

            foreach (var keyValuePair in nodeCounter)
            {
                if (keyValuePair.Key.Value.EntType == EntityType.GOAL)
                {
                    Console.WriteLine(keyValuePair.Key.ToString() + ": " + keyValuePair.Value);
                }
            }
        }

        private void AddToNodeCounterForShortestPath(GoalNode startNode, GoalNode goalNode, Dictionary<GoalNode, int> nodeCounter)
        {
            int minLength = int.MaxValue;
            Queue<GoalNode> frontier = new Queue<GoalNode>();
            Dictionary<GoalNode, List<GoalNode>> childToParent = new Dictionary<GoalNode, List<GoalNode>>();

            frontier.Enqueue(startNode);
            childToParent.Add(startNode, null);

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
                if (leaf == goalNode)
                {
                    minLength = depth;
                    Stack<GoalNode> toSee = new Stack<GoalNode>();
                    toSee.Push(leaf);

                    while (toSee.Count > 0)
                    {
                        GoalNode pathEnd = toSee.Pop();
                        while (childToParent.TryGetValue(pathEnd, out List<GoalNode> parent))
                        {
                            Console.WriteLine(pathEnd.ToString());
                            nodeCounter[pathEnd] = nodeCounter[pathEnd] + 1;
                            if (parent == null)
                            {
                                break;
                            }
                            if (parent.Count > 1)
                            {
                                foreach (var node in parent)
                                {
                                    toSee.Push(node);
                                }
                                break;
                            }
                            pathEnd = parent.First();
                        }
                    }

                    Console.WriteLine();
                }

                foreach (var child in leaf.Edges)
                {
                    GoalNode goalChild = (GoalNode)child.End;
                    if (!childToParent.ContainsKey(goalChild))
                    {
                        childToParent.Add(goalChild, new List<GoalNode>());
                    }

                    frontier.Enqueue(goalChild);
                    childToParent[goalChild].Add(leaf);
                    nextDepthNodeCount++;
                }
            }
        }
    }
}
