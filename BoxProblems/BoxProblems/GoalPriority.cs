using System;
using System.Collections.Generic;
using System.Linq;
using BoxProblems.Graphing;

namespace BoxProblems
{
    class GoalPriority
    {
        //internal readonly struct PriorityGoal
        //{
        //    public readonly Point Pos;
        //    public readonly char Type;
        //    public readonly int Priority;

        //    public PriorityGoal(GoalNode goal, int priority)
        //    {
        //        this.Pos = goal.Value.Pos;
        //        this.Type = goal.Value.Representation;
        //        this.Priority = priority;
        //    }
        //}

        //public List<PriorityGoal> GetGoalPrioity(GoalGraph graph)
        //{ 
        //    var priorityGoals = new List<PriorityGoal>();
        //    var (goals, boxes) = SplitGraph(graph);
        //    foreach (GoalNode curGoal in goals)
        //    {
        //        int priority = 0;
        //        var priorities = new List<int>();
        //        var allPaths = FindPath(graph, curGoal, boxes);
        //        foreach (List<GoalNode> curPath in allPaths)
        //        {
        //            priority = 0;
        //            priorities = new List<int>(); 
        //            foreach (Node<GoalNodeInfo, GoalEdgeInfo> curPos in curPath)
        //            {
        //                if (!BoxOnGoal(curGoal, curPos) && curPos.Value.IsGoal)
        //                {
        //                    priority++;
        //                }
        //            }
        //            priorities.Add(priority);
        //        }
        //        priorityGoals.Add(new PriorityGoal(curGoal, priorities.Min()));
        //    }


        //    return priorityGoals.OrderByDescending(x => x.Priority).ToList(); 
        //}

        //public Boolean BoxOnGoal(Node<GoalNodeInfo, GoalEdgeInfo> goal, Node<GoalNodeInfo, GoalEdgeInfo> curPos)
        //{
        //    return goal.Equals(curPos);
        //}  

        //public (List<GoalNode> goals, List<GoalNode> boxes) SplitGraph(GoalGraph graph)
        //{
        //    var goals = new List<GoalNode>(); 
        //    var boxes = new List<GoalNode>();
        //    foreach (Node<GoalNodeInfo, GoalEdgeInfo> node in graph.Nodes)
        //    {
        //        if (node.Value.IsGoal)
        //        {
        //            goals.Add((GoalNode)node); 
        //        } else
        //        {
        //            boxes.Add((GoalNode)node); 
        //        }
        //    }
        //    return (goals, boxes); 
        //}

        //public List<List<GoalNode>> FindPath(GoalGraph graph, GoalNode goal, List<GoalNode> boxes)
        //{
        //    //filter boxes to only be boxes that can go on current goal
        //    var goalBoxes = boxes.Where(x => x.Value.Representation == char.ToUpper(goal.Value.Representation)).ToList();
        //    var paths = new List<List<GoalNode>>();

        //    foreach (GoalNode box in goalBoxes)
        //    {
        //        var s = new Queue<GoalNode>();
        //        var path = new List<GoalNode>(); 
        //        //Explored node, parent
        //        var exploredNodes = new List<(GoalNode, GoalNode)>();
                
        //        s.Enqueue(box);
        //        while (s.Count > 0)
        //        {
        //            var v = s.Dequeue(); 
        //            if (v == goal)
        //            {
        //                break;
        //            } 
        //            foreach (Edge<GoalNodeInfo, GoalEdgeInfo> edges in v.Edges)
        //            {
        //                if (!exploredNodes.Contains(((GoalNode)edges.End, v)))
        //                {
        //                    s.Enqueue((GoalNode)edges.End);
        //                    exploredNodes.Add(((GoalNode)edges.End,v));
        //                }
        //            }
        //        }

        //        paths.Add(TraversePath(box, exploredNodes));
        //    }

        //    return paths;
        //}
        
        //public List<GoalNode> TraversePath(GoalNode start, List<(GoalNode, GoalNode)> exploredNodes)
        //{

        //    var path = new List<GoalNode>();

        //    var (child, parent) = exploredNodes.Last();

        //    while (!child.Equals(start))
        //    {
        //        path.Add(child);
                
        //        (child, parent) = exploredNodes.FirstOrDefault(eri => eri.Item1.Equals(parent));  
        //        if (child == null)
        //        {
        //            break;
        //        }

        //    }
        //    path.Add(start);
        //    return path; 
        //}
    }
}
