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

            Dictionary<Point, Node<EntityNodeInfo, DistanceEdgeInfo>> potentialGoals = new Dictionary<Point, Node<EntityNodeInfo, DistanceEdgeInfo>>();
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<EntityNodeInfo, DistanceEdgeInfo>)inode;
                potentialGoals.Add(node.Value.Ent.Pos, node);
            }
            var goalCondition = new Func<(Point pos, int distance), GraphSearcher.GoalFound<Node<EntityNodeInfo, DistanceEdgeInfo>>>(x =>
            {
                bool isGoal = potentialGoals.TryGetValue(x.pos, out Node<EntityNodeInfo, DistanceEdgeInfo> value);
                return new GraphSearcher.GoalFound<Node<EntityNodeInfo, DistanceEdgeInfo>>(value, isGoal);
            });

            HashSet<(INode, INode)> seenConnections = new HashSet<(INode, INode)>();
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    Point pos = new Point(x, y);

                    //always search from a free space
                    if (potentialGoals.ContainsKey(pos) || level.IsWall(pos))
                    {
                        continue;
                    }

                    var reached = GraphSearcher.GetReachedGoalsBFS(gsData, level, pos, goalCondition);
                    //fullyConnectedGroups.Add(reached);

                    for (int z = 0; z < reached.Count; z++)
                    {
                        for (int q = z; q < reached.Count; q++)
                        {
                            var startNode = reached[z];
                            var endNode = reached[q];

                            if (seenConnections.Contains((startNode, endNode)))
                            {
                                continue;
                            }

                            startNode.AddEdge(new Edge<DistanceEdgeInfo>(endNode, new DistanceEdgeInfo()));
                            endNode.AddEdge(new Edge<DistanceEdgeInfo>(startNode, new DistanceEdgeInfo()));

                            seenConnections.Add((startNode, endNode));
                            seenConnections.Add((endNode, startNode));
                        }
                    }
                }
            }
            level.ResetWalls();

            foreach (var iNode in graph.Nodes)
            {
                var startNode = (Node<EntityNodeInfo, DistanceEdgeInfo>)iNode;

                short[,] distancesMap = Precomputer.GetDistanceMap(level.Walls, startNode.Value.Ent.Pos, false);

                for (int i = 0; i < startNode.Edges.Count; i++)
                {
                    var endNode = (Node<EntityNodeInfo, DistanceEdgeInfo>)startNode.Edges[i].End;
                    int distance = distancesMap[endNode.Value.Ent.Pos.X, endNode.Value.Ent.Pos.Y];
                    startNode.Edges[i] = new Edge<DistanceEdgeInfo>(endNode, new DistanceEdgeInfo(distance));
                }
            }
        }
    }
}
