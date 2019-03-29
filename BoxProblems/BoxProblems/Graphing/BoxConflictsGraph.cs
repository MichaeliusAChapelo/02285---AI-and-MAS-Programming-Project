using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    //internal readonly struct NodeInfo
    //{
    //    public readonly char Representation;
    //    public readonly Point Pos;
    //    public readonly bool IsGoal;
    //    public readonly bool IsBox;

    //    public GoalNodeInfo(char rep, Point pos, bool isGoal, bool isBox)
    //    {
    //        this.Representation = rep;
    //        this.Pos = pos;
    //        this.IsGoal = isGoal;
    //        this.IsBox = isBox;
    //    }
    //}

    //internal readonly struct GoalEdgeInfo
    //{

    //}

    //internal class GoalNode : Node<GoalNodeInfo, GoalEdgeInfo>
    //{
    //    public GoalNode(GoalNodeInfo value) : base(value)
    //    {
    //    }

    //    public override string ToString()
    //    {
    //        return Value.Representation.ToString();
    //    }
    //}

    //internal class GoalEdge : Edge<GoalNodeInfo, GoalEdgeInfo>
    //{
    //    public GoalEdge(Node<GoalNodeInfo, GoalEdgeInfo> end, GoalEdgeInfo value) : base(end, value)
    //    {
    //    }

    //    public override string ToString()
    //    {
    //        return string.Empty;
    //    }
    //}

    //internal class GoalGraph : Graph<GoalNodeInfo, GoalEdgeInfo>
    //{
    //    public GoalGraph(State state, Level level)
    //    {
    //        foreach (var box in state.GetBoxes(level))
    //        {
    //            Nodes.Add(new GoalNode(new GoalNodeInfo(box.Type, box.Pos, false, true)));
    //        }
    //        foreach (var goal in level.Goals)
    //        {
    //            Nodes.Add(new GoalNode(new GoalNodeInfo(char.ToLower(goal.Type), goal.Pos, true, false)));
    //            level.Walls[goal.Pos.X, goal.Pos.Y] = true;
    //        }

    //        List<Point> potentialGoals = new List<Point>();
    //        foreach (var node in Nodes)
    //        {
    //            potentialGoals.Add(node.Value.Pos);
    //        }
    //        for (int i = 0; i < Nodes.Count; i++)
    //        {
    //            GoalNode node = (GoalNode)Nodes[i];
    //            level.Walls[node.Value.Pos.X, node.Value.Pos.Y] = false;
    //            potentialGoals.Remove(node.Value.Pos);

    //            var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Pos, potentialGoals);

    //            List<GoalEdge> edges = new List<GoalEdge>();
    //            foreach (Point reached in reachedGoals)
    //            {
    //                GoalNode target = (GoalNode)Nodes.Single(x => x.Value.Pos == reached);
    //                if (node.Value.IsBox && target.Value.IsBox)
    //                {
    //                    continue;
    //                }
    //                node.AddEdge(new GoalEdge(target, new GoalEdgeInfo()));
    //            }

    //            potentialGoals.Add(node.Value.Pos);
    //            if (!node.Value.IsBox)
    //            {
    //                level.Walls[node.Value.Pos.X, node.Value.Pos.Y] = true;
    //            }
    //        }

    //        foreach (var goal in level.Goals)
    //        {
    //            level.Walls[goal.Pos.X, goal.Pos.Y] = false;
    //        }
    //    }
    //}
}
