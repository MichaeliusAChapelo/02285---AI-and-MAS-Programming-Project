using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal static class GraphCreator
    {
        public static void CreateGraphIgnoreEntityType(Graph<EntityNodeInfo, EmptyEdgeInfo> graph, Level level, EntityType notAHindrance)
        {
            List<Point> potentialGoals = new List<Point>();
            foreach (var node in graph.Nodes)
            {
                potentialGoals.Add(node.Value.Ent.Pos);
            }
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = false;
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Ent.Pos, potentialGoals);

                var edges = new List<Node<EntityNodeInfo, EmptyEdgeInfo>>();
                foreach (Point reached in reachedGoals)
                {
                    var target = graph.Nodes.Single(x => x.Value.Ent.Pos == reached);
                    if (node.Value.EntType == notAHindrance &&
                        target.Value.EntType == notAHindrance)
                    {
                        continue;
                    }
                    node.AddEdge(new Edge<EntityNodeInfo, EmptyEdgeInfo>(target, new EmptyEdgeInfo()));
                }

                potentialGoals.Add(node.Value.Ent.Pos);
                if (node.Value.EntType != notAHindrance)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }
        }
    }
}
