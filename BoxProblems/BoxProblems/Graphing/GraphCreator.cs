using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal static class GraphCreator
    {
        public static void CreateGraphIgnoreEntityType(GraphSearchData gsData, Graph graph, Level level, EntityType notAHindrance)
        {
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                if (node.Value.EntType != notAHindrance)
                {
                    level.AddWall(node.Value.Ent.Pos);
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
                level.RemoveWall(node.Value.Ent.Pos);
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(gsData, level, node.Value.Ent.Pos, goalCondition);

                foreach (var reached in reachedGoals)
                {
                    if (node.Value.EntType == notAHindrance &&
                        reached.node.Value.EntType == notAHindrance)
                    {
                        continue;
                    }
                    node.AddEdge(new Edge<DistanceEdgeInfo>(reached.node, new DistanceEdgeInfo(reached.distance)));
                    reached.node.AddEdge(new Edge<DistanceEdgeInfo>(node, new DistanceEdgeInfo(reached.distance)));
                }
                
                if (node.Value.EntType != notAHindrance)
                {
                    level.AddWall(node.Value.Ent.Pos);
                }
            }

            level.ResetWalls();
        }
    }
}
