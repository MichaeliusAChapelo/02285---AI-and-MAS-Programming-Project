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
            public readonly int Priority;

            public PriorityGoal(Node<GoalNodeInfo, GoalEdgeInfo> goal, int priority)
            {
                this.Pos = goal.Value.Pos;
                this.Type = goal.Value.Representation;
                this.Priority = priority;
            }
        }

        public List<PriorityGoal> GetGoalPrioity(GoalGraph graph)
        { 
            var priorityGoals = new List<PriorityGoal>();
            var (goals, boxes) = SplitGraph(graph);
            foreach (Node<GoalNodeInfo, GoalEdgeInfo> curGoal in goals)
            {
                int priority = 0;
                var priorities = new List<int>();
                var allPaths = FindPath(graph, curGoal, boxes);
                foreach (List<Node<GoalNodeInfo, GoalEdgeInfo>> curPath in allPaths)
                {
                    priority = 0;
                    priorities = new List<int>(); 
                    foreach (Node<GoalNodeInfo, GoalEdgeInfo> curPos in curPath)
                    {
                        if (!BoxOnGoal(curGoal, curPos) && curPos.Value.IsGoal)
                        {
                            priority++;
                        }
                    }
                    priorities.Add(priority);
                }
                priorityGoals.Add(new PriorityGoal(curGoal, priorities.Min()));
            }
            return priorityGoals.OrderBy(x => x.Priority).ToList(); 
        }

        public Boolean BoxOnGoal(Node<GoalNodeInfo, GoalEdgeInfo> goal, Node<GoalNodeInfo, GoalEdgeInfo> curPos)
        {
            return goal.Equals(curPos);
        }  

        public Tuple<List<Node<GoalNodeInfo, GoalEdgeInfo>>, List<Node<GoalNodeInfo, GoalEdgeInfo>>> SplitGraph(GoalGraph graph)
        {
            var goals = new List<Node<GoalNodeInfo,GoalEdgeInfo>>(); 
            var boxes = new List<Node<GoalNodeInfo, GoalEdgeInfo>>();
            foreach (Node<GoalNodeInfo, GoalEdgeInfo> node in graph.Nodes)
            {
                if (node.Value.IsGoal)
                {
                    goals.Add(node); 
                } else
                {
                    boxes.Add(node); 
                }
            }
            return new Tuple<List<Node<GoalNodeInfo, GoalEdgeInfo>>, List<Node<GoalNodeInfo, GoalEdgeInfo>>>(goals, boxes); 
        }

        public List<List<Node<GoalNodeInfo, GoalEdgeInfo>>> FindPath(GoalGraph graph, Node<GoalNodeInfo, GoalEdgeInfo> goal, List<Node<GoalNodeInfo, GoalEdgeInfo>> boxes)
        {
            //filter boxes to only be boxes that can go on current goal
            var goalBoxes = boxes.Where(x => x.Value.Representation == char.ToUpper(goal.Value.Representation)).ToList();
            var paths = new List<List<Node<GoalNodeInfo, GoalEdgeInfo>>>();

            foreach (Node<GoalNodeInfo, GoalEdgeInfo> box in goalBoxes)
            {
                var s = new Queue<Node<GoalNodeInfo, GoalEdgeInfo>>();
                var path = new List<Node<GoalNodeInfo, GoalEdgeInfo>>(); 
                //Explored node, parent
                var exploredNodes = new List<(Node<GoalNodeInfo, GoalEdgeInfo>, Node<GoalNodeInfo, GoalEdgeInfo>)>();
                
                s.Enqueue(box);
                while (s.Count > 0)
                {
                    var v = s.Dequeue(); 
                    if (v.Equals(goal))
                    {
                        break;
                    } 
                    foreach (Edge<GoalNodeInfo, GoalEdgeInfo> edges in v.Edges)
                    {
                        if (!exploredNodes.Contains((edges.End,v)))
                        {
                            s.Enqueue(edges.End);
                            exploredNodes.Add((edges.End,v));
                        }
                    }
                }

                paths.Add(TraversePath(box, exploredNodes));
            }

            return paths;
        }
        
        public List<Node<GoalNodeInfo,GoalEdgeInfo>> TraversePath(Node<GoalNodeInfo,GoalEdgeInfo> start, List<(Node<GoalNodeInfo, GoalEdgeInfo>, Node<GoalNodeInfo, GoalEdgeInfo>)> exploredNodes)
        {

            var path = new List<Node<GoalNodeInfo, GoalEdgeInfo>>();

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
            return path; 
        }
    }
}
