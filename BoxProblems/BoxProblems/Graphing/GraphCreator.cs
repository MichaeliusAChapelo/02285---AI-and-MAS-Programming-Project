using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal static class GraphCreator
    {
        public static void CreateGraphIgnoreEntityType(Graph graph, Level level, EntityType notAHindrance)
        {
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                if (node.Value.EntType != notAHindrance)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }
            //Console.WriteLine(level.WorldToString(level.GetWallsAsWorld()));
     
            Dictionary<Point, Node<EntityNodeInfo, DistanceEdgeInfo>> potentialGoals = new Dictionary<Point, Node<EntityNodeInfo, DistanceEdgeInfo>>();
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                potentialGoals.Add(node.Value.Ent.Pos, node);
            }
            var goalCondition = new Func<(Point pos, int distance), GraphSearcher.GoalFound<(Node<EntityNodeInfo, DistanceEdgeInfo> node, int distance)>>(x =>
            {
                bool isGoal = potentialGoals.TryGetValue(x.pos, out Node<EntityNodeInfo, DistanceEdgeInfo> value);
                return new GraphSearcher.GoalFound<(Node<EntityNodeInfo, DistanceEdgeInfo>, int)>((value, x.distance), isGoal);
            });

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)graph.Nodes[i];
                level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = false;
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Ent.Pos, goalCondition);

                foreach (var reached in reachedGoals)
                {
                    if (node.Value.EntType == notAHindrance &&
                        reached.node.Value.EntType == notAHindrance)
                    {
                        continue;
                    }
                    node.AddEdge(new Edge<DistanceEdgeInfo>(reached.node, new DistanceEdgeInfo(reached.distance)));
                }

                potentialGoals.Add(node.Value.Ent.Pos, node);
                if (node.Value.EntType != notAHindrance)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }

            level.ResetWalls();
        }
    }
}
