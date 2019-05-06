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
                var node = (Node<EntityNodeInfo, EmptyEdgeInfo>)inode;
                if (node.Value.EntType != notAHindrance)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }
            //Console.WriteLine(level.WorldToString(level.GetWallsAsWorld()));
     
            Dictionary<Point, List<Node<EntityNodeInfo, EmptyEdgeInfo>>> potentialGoals = new Dictionary<Point, List<Node<EntityNodeInfo, EmptyEdgeInfo>>>();
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, EmptyEdgeInfo>)inode;
                if (!potentialGoals.ContainsKey(node.Value.Ent.Pos))
                {
                    potentialGoals.Add(node.Value.Ent.Pos, new List<Node<EntityNodeInfo, EmptyEdgeInfo>>());
                }
                potentialGoals[node.Value.Ent.Pos].Add(node);
            }
            var goalCondition = new Func<Point, GraphSearcher.GoalFound<List<Node<EntityNodeInfo, EmptyEdgeInfo>>>>(x =>
                {
                    bool isGoal = potentialGoals.TryGetValue(x, out List<Node<EntityNodeInfo, EmptyEdgeInfo>> value);
                    return new GraphSearcher.GoalFound<List<Node<EntityNodeInfo, EmptyEdgeInfo>>>(value, isGoal);
                });

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = (Node<EntityNodeInfo, EmptyEdgeInfo>)graph.Nodes[i];
                level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = false;
                var storedNodes = potentialGoals[node.Value.Ent.Pos];
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Ent.Pos, goalCondition);

                foreach (var reachedList in reachedGoals)
                {
                    foreach (var reached in reachedList)
                    {
                        if (node.Value.EntType == notAHindrance &&
                            reached.Value.EntType == notAHindrance)
                        {
                            continue;
                        }
                        node.AddEdge(new Edge<EmptyEdgeInfo>(reached, new EmptyEdgeInfo()));
                    }
                }

                potentialGoals.Add(node.Value.Ent.Pos, storedNodes);
                if (node.Value.EntType != notAHindrance)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }

            level.ResetWalls();
        }
    }
}
