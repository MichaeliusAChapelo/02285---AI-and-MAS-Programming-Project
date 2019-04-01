using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxProblems
{
    class GoalPriority
    {
        internal readonly struct PriorityGoal
        {
            public readonly Point Pos;
            public readonly char Type;
            public readonly int ThroughGoalPriority;
            public readonly GoalNode LargerThanPriority;

            public PriorityGoal(GoalNode goal, int throughGoalPriority, GoalNode largerThanPriority)
            {
                this.Pos = goal.Value.Ent.Pos;
                this.Type = goal.Value.Ent.Type;
                this.ThroughGoalPriority = throughGoalPriority;
                this.LargerThanPriority = largerThanPriority;
            }
        }

        public List<PriorityGoal> GetGoalPrioity(GoalGraph graph)
        {
            var priorityGoals = new List<PriorityGoal>();
            var (goals, boxes) = SplitGraph(graph);
            foreach (GoalNode curGoal in goals)
            {
                int priority = 0;
                var priorities = new List<int>();
                var allPaths = FindPath(graph, curGoal, boxes);
                var goalBoxes = boxes.Where(x => x.Value.Ent.Type == char.ToUpper(curGoal.Value.Ent.Type)).ToList();
                priorities = new List<int>();
                foreach (List<GoalNode> curPath in allPaths)
                {
                    priority = 0;
                    foreach (GoalNode curPos in curPath)
                    {
                        if (!goalBoxes.Contains(curPos) && curPos != curGoal && curPos.Value.EntType == EntityType.GOAL)
                        {
                            priority++;
                        }
                    }
                    priorities.Add(priority);
                }

                var firstElm = allPaths.First()[1];
                GoalNode largerThanPriority = curGoal;
                if (allPaths.All(x => x[1] == firstElm))
                {
                    largerThanPriority = firstElm;
                }

                priorityGoals.Add(new PriorityGoal(curGoal, priorities.Min(), largerThanPriority));
            }
            return priorityGoals.OrderByDescending(x => x.ThroughGoalPriority).ToList();
        }

        public List<PriorityGoal> OrderByPriority(List<PriorityGoal> priorityGoals)
        {
            var newPriorityGoals = new List<PriorityGoal>();
            //foreach (PriorityGoal priorityGoal in priorityGoals)
            //{
            //    if (priorityGoal.LargerThanPriority.Value.Pos != priorityGoal.Pos)
            //    {
            //        newPriorityGoals.AddGoal(priorityGoal); 
            //    }
            //}
            return priorityGoals;
        }

        //public List<PriorityGoal> AddGoal(List<PriorityGoal> priorityGoals, GoalNode newGoal)
        //{

        //}

        public Boolean BoxOnGoal(Node<EntityNodeInfo, EmptyEdgeInfo> goal, Node<EntityNodeInfo, EmptyEdgeInfo> curPos)
        {
            return goal.Equals(curPos);
        }

        public (List<GoalNode> goals, List<GoalNode> boxes) SplitGraph(GoalGraph graph)
        {
            var goals = new List<GoalNode>();
            var boxes = new List<GoalNode>();
            foreach (Node<EntityNodeInfo, EmptyEdgeInfo> node in graph.Nodes)
            {
                if (node.Value.EntType == EntityType.GOAL)
                {
                    goals.Add((GoalNode)node);
                }
                else
                {
                    boxes.Add((GoalNode)node);
                }
            }
            return (goals, boxes);
        }

        public List<List<GoalNode>> FindPath(GoalGraph graph, GoalNode goal, List<GoalNode> boxes)
        {
            var allPaths = new List<List<GoalNode>>();

            //goal to boxes
            var goalBoxes = boxes.Where(x => x.Value.Ent.Type == char.ToUpper(goal.Value.Ent.Type)).ToList();
            var s = new Queue<List<GoalNode>>();

            s.Enqueue(new List<GoalNode>() { goal });

            while (s.Count > 0)
            {
                var currentPath = s.Dequeue();
                var currentNode = currentPath.Last();

                if (goalBoxes.Contains(currentNode))
                {
                    allPaths.Add(currentPath);
                    //Console.Write("Goal: " + goal.Value.Representation + ", ");
                    //foreach (GoalNode gn in currentPath)
                    //{
                    //    Console.Write(gn.Value.Representation + " ");
                    //}
                    //Console.WriteLine("Done");
                    continue;
                }

                if (currentNode.Value.EntType == EntityType.BOX)
                {
                    continue;
                }

                foreach (Edge<EntityNodeInfo, EmptyEdgeInfo> edge in currentNode.Edges)
                {
                    //if (edge.end.value.isbox && edge.end.value.representation != char.toupper(goal.value.representation))
                    //{
                    //    continue;
                    //}

                    if (!currentPath.Contains(edge.End))
                    {
                        List<GoalNode> currentCurrentPath = new List<GoalNode>();
                        currentCurrentPath.AddRange(currentPath);
                        currentCurrentPath.Add((GoalNode)edge.End);
                        s.Enqueue((currentCurrentPath));
                    }
                }
            }
            return allPaths;
        }
        public List<List<GoalNode>> FindPath2(GoalGraph graph, GoalNode goal, List<GoalNode> boxes)
        {
            //filter boxes to only be boxes that can go on current goal
            var goalBoxes = boxes.Where(x => x.Value.Ent.Type == char.ToUpper(goal.Value.Ent.Type)).ToList();
            var paths = new List<List<GoalNode>>();

            foreach (GoalNode box in goalBoxes)
            {
                var s = new Queue<GoalNode>();
                //var path = new List<GoalNode>();

                //Explored node, parent
                var exploredNodes = new List<(GoalNode, GoalNode)>();

                s.Enqueue(box);
                while (s.Count > 0)
                {
                    var v = s.Dequeue();
                    if (v == goal)
                    {
                        //exploredNodes.Add((GoalNode)v);
                        break;
                    }

                    foreach (Edge<EntityNodeInfo, EmptyEdgeInfo> edges in v.Edges)
                    {
                        if (!exploredNodes.Contains(((GoalNode)edges.End, v)))
                        {
                            s.Enqueue((GoalNode)edges.End);
                            exploredNodes.Add(((GoalNode)edges.End, v));
                        }
                    }
                }
                paths.Add(TraversePath(box, exploredNodes));
            }
            return paths;
        }

        public List<GoalNode> TraversePath(GoalNode start, List<(GoalNode, GoalNode)> exploredNodes)
        {

            var path = new List<GoalNode>();

            var (child, parent) = exploredNodes.Last();

            while (!child.Equals(start))
            {
                path.Add(child);

                (child, parent) = exploredNodes.FirstOrDefault(eri => eri.Item1.Equals(parent));
                if (child == null)
                {
                    break;
                }

            }
            path.Add(start);

            foreach (GoalNode gn in path)
            {
                Console.Write(gn.Value.Ent.Type + " ");
                Console.WriteLine("Done");
            }
            return path;
        }
    }
}