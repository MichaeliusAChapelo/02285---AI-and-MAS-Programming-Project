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
            HashSet<GoalNode> exploredSet = new HashSet<GoalNode>();

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
                if (leaf == goalNode)
                {
                    minLength = depth;
                    Stack<GoalNode> toSee = new Stack<GoalNode>();
                    toSee.Push(leaf);

                    while (toSee.Count > 0)
                    {
                        GoalNode pathEnd = toSee.Pop();
                        List<GoalNode> parent;
                        if (!childToParent.TryGetValue(pathEnd, out parent))
                        {
                            continue;
                        }

                        Console.WriteLine(pathEnd.ToString());
                        nodeCounter[pathEnd] = nodeCounter[pathEnd] + 1;
                        if (parent == null)
                        {
                            continue;
                        }
                        foreach (var node in parent)
                        {
                            toSee.Push(node);
                        }
                    }

                    Console.WriteLine();
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
        }
    }
}
