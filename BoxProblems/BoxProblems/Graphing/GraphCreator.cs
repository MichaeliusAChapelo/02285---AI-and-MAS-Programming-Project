using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal static class GraphCreator
    {
        public static void CreateGraphIgnoreEntityType(GraphSearchData gsData, Graph graph, List<INode> nodesToAdd, Level level, EntityType notAHindrance)
        {
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                if (!node.Value.EntType.EntityEquals(notAHindrance))
                {
                    level.AddWall(node.Value.Ent.Pos);
                }
            }

            Dictionary<Point, List<Node<EntityNodeInfo, DistanceEdgeInfo>>> potentialGoals = new Dictionary<Point, List<Node<EntityNodeInfo, DistanceEdgeInfo>>>();
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                if (!potentialGoals.ContainsKey(node.Value.Ent.Pos))
                {
                    potentialGoals.Add(node.Value.Ent.Pos, new List<Node<EntityNodeInfo, DistanceEdgeInfo>>());
                }
                potentialGoals[node.Value.Ent.Pos].Add(node);
            }
            var goalCondition = new Func<(Point pos, int distance), GraphSearcher.GoalFound<(List<Node<EntityNodeInfo, DistanceEdgeInfo>> nodes, int distance)>>(x =>
            {
                bool isGoal = potentialGoals.TryGetValue(x.pos, out List<Node<EntityNodeInfo, DistanceEdgeInfo>> value);
                return new GraphSearcher.GoalFound<(List<Node<EntityNodeInfo, DistanceEdgeInfo>>, int)>((value, x.distance), isGoal);
            });

            for (int i = 0; i < nodesToAdd.Count; i++)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)nodesToAdd[i];
                bool wasWall = level.IsWall(node.Value.Ent.Pos);
                level.RemoveWall(node.Value.Ent.Pos);
                var storedNodes = potentialGoals[node.Value.Ent.Pos];
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(gsData, level, node.Value.Ent.Pos, goalCondition);

                foreach (var reachedList in reachedGoals)
                {
                    foreach (var reached in reachedList.nodes)
                    {
                        if (node.Value.EntType.EntityEquals(notAHindrance) &&
                            reached.Value.EntType.EntityEquals(notAHindrance))
                        {
                            continue;
                        }
                        node.AddEdge(new Edge<DistanceEdgeInfo>(reached, new DistanceEdgeInfo(reachedList.distance)));
                        if (nodesToAdd != graph.Nodes && !reached.Edges.Any(x => x.End == node))
                        {
                            reached.AddEdge(new Edge<DistanceEdgeInfo>(node, new DistanceEdgeInfo(reachedList.distance)));
                        }
                    }
                }

                potentialGoals.Add(node.Value.Ent.Pos, storedNodes);
                if (wasWall)
                {
                    level.AddWall(node.Value.Ent.Pos);
                }
            }

            level.ResetWalls();
        }
    }
}
