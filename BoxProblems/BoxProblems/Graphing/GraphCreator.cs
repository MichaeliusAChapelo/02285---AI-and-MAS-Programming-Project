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
     
            Dictionary<Point, Node<EntityNodeInfo, EmptyEdgeInfo>> potentialGoals = new Dictionary<Point, Node<EntityNodeInfo, EmptyEdgeInfo>>();
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, EmptyEdgeInfo>)inode;
                potentialGoals.Add(node.Value.Ent.Pos, node);
            }
            Func<Point, GraphSearcher.GoalFound<Node<EntityNodeInfo, EmptyEdgeInfo>>> goalCondition = new Func<Point, GraphSearcher.GoalFound<Node<EntityNodeInfo, EmptyEdgeInfo>>>(x =>
                {
                    bool isGoal = potentialGoals.TryGetValue(x, out Node<EntityNodeInfo, EmptyEdgeInfo> value);
                    return new GraphSearcher.GoalFound<Node<EntityNodeInfo, EmptyEdgeInfo>>(value, isGoal);
                });

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = (Node<EntityNodeInfo, EmptyEdgeInfo>)graph.Nodes[i];
                level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = false;
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Ent.Pos, goalCondition);

                foreach (var reached in reachedGoals)
                {
                    if (node.Value.EntType == notAHindrance &&
                        reached.Value.EntType == notAHindrance)
                    {
                        continue;
                    }
                    node.AddEdge(new Edge<EmptyEdgeInfo>(reached, new EmptyEdgeInfo()));
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
