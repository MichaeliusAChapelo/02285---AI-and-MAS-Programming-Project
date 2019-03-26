using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems
{
    internal class Node<N, E>
    {
        public readonly N Value;
        public readonly List<Edge<N, E>> Edges = new List<Edge<N, E>>();

        public Node(N value)
        {
            this.Value = value;
        }

        public void AddEdge(Edge<N, E> edge)
        {
            Edges.Add(edge);
        }
    }

    internal class Edge<N, E>
    {
        public readonly Node<N, E> End;
        public readonly E Value;

        public Edge(Node<N, E> end, E value)
        {
            this.End = end;
            this.Value = value;
        }
    }



    internal class Graph<N, E>
    {
        public readonly List<Node<N, E>> Nodes = new List<Node<N, E>>();

        public void AddNode(Node<N, E> node)
        {
            Nodes.Add(node);
        }

        public (string nodes, string edges) ToCytoscapeString()
        {
            StringBuilder nodesBuilder = new StringBuilder();
            StringBuilder edgesBuilder = new StringBuilder();

            HashSet<Node<N, E>> foundNodes = new HashSet<Node<N, E>>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                nodesBuilder.Append($"{{ data: {{ id: '{i}', label: '{Nodes[i]}' }} }},");
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                foreach (var edge in Nodes[i].Edges)
                {
                    if (foundNodes.Contains(edge.End))
                    {
                        continue;
                    }
                    foundNodes.Add(Nodes[i]);
                    edgesBuilder.Append($"{{ data: {{ source: '{i}', target: '{Nodes.IndexOf(edge.End)}' }} }},");
                }
            }

            return ($"[{nodesBuilder.ToString()}]", $"[{edgesBuilder.ToString()}]");
        }
    }

    internal readonly struct GoalNodeInfo
    {
        public readonly char Representation;
        public readonly Point Pos;
        public readonly bool IsGoal;
        public readonly bool IsBox;

        public GoalNodeInfo(char rep, Point pos, bool isGoal, bool isBox)
        {
            this.Representation = rep;
            this.Pos = pos;
            this.IsGoal = isGoal;
            this.IsBox = isBox;
        }
    }

    internal readonly struct GoalEdgeInfo
    {

    }

    internal class GoalNode : Node<GoalNodeInfo, GoalEdgeInfo>
    {
        public GoalNode(GoalNodeInfo value) : base(value)
        {
        }

        public override string ToString()
        {
            return Value.Representation.ToString();
        }
    }

    internal class GoalEdge : Edge<GoalNodeInfo, GoalEdgeInfo>
    {
        public GoalEdge(Node<GoalNodeInfo, GoalEdgeInfo> end, GoalEdgeInfo value) : base(end, value)
        {
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal class GoalGraph : Graph<GoalNodeInfo, GoalEdgeInfo>
    {
        public GoalGraph(State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                Nodes.Add(new GoalNode(new GoalNodeInfo(box.Type, box.Pos, false, true)));
            }
            foreach (var goal in level.Goals)
            {
                Nodes.Add(new GoalNode(new GoalNodeInfo(char.ToLower(goal.Type), goal.Pos, true, false)));
            }

            foreach (var goal in level.Goals)
            {
                level.Walls[goal.Pos.X, goal.Pos.Y] = true;
            }

            List<Point> potentialGoals = new List<Point>();
            foreach (var node in Nodes)
            {
                potentialGoals.Add(node.Value.Pos);
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                GoalNode node = (GoalNode)Nodes[i];
                level.Walls[node.Value.Pos.X, node.Value.Pos.Y] = false;
                potentialGoals.Remove(node.Value.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Pos, potentialGoals);

                List<GoalEdge> edges = new List<GoalEdge>();
                foreach (var reached in reachedGoals)
                {
                    GoalNode target = (GoalNode)Nodes.Single(x => x.Value.Pos == reached);
                    if (node.Value.IsBox && target.Value.IsBox)
                    {
                        continue;
                    }
                    node.AddEdge(new GoalEdge(target, new GoalEdgeInfo()));
                }

                potentialGoals.Add(node.Value.Pos);
                if (!node.Value.IsBox)
                {
                    level.Walls[node.Value.Pos.X, node.Value.Pos.Y] = true;
                }
            }

            foreach (var goal in level.Goals)
            {
                level.Walls[goal.Pos.X, goal.Pos.Y] = false;
            }
        }
    }
}
