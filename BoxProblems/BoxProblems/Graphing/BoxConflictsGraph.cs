using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal readonly struct DistanceEdgeInfo
    {
        public readonly int Distance;

        public DistanceEdgeInfo(int distance)
        {
            this.Distance = distance;
        }
    }

    internal class BoxConflictNode : Node<EntityNodeInfo, DistanceEdgeInfo>
    {
        public BoxConflictNode(EntityNodeInfo value) : base(value)
        {
        }

        public override string ToString()
        {
            if (Value.EntType.IsGoal())
            {
                return char.ToLower(Value.Ent.Type).ToString();
            }
            else
            {
                return Value.Ent.Pos.ToString() + " " + Value.Ent.Type.ToString();
            }
        }
    }

    internal readonly struct FreeSpaceNodeInfo
    {
        public readonly HashSet<Point> FreeSpaces;

        public FreeSpaceNodeInfo(List<Point> freeSpaces)
        {
            this.FreeSpaces = new HashSet<Point>(freeSpaces);
        }
    }

    internal class FreeSpaceNode : Node<FreeSpaceNodeInfo, DistanceEdgeInfo>
    {
        public FreeSpaceNode(FreeSpaceNodeInfo value) : base(value)
        {
        }

        public override string ToString()
        {
            return $"Free spaces: {Value.FreeSpaces.Count}";
        }
    }

    public sealed class BoxConflictGraph : Graph
    {
        private readonly Dictionary<Point, INode> PositionToNode = new Dictionary<Point, INode>();
        internal readonly State CreatedFromThisState;

        internal BoxConflictGraph(GraphSearchData gsData, State state, Level level, HashSet<Entity> removedEntities)
        {
            CreatedFromThisState = state;

            foreach (var box in state.GetBoxes(level))
            {
                if (removedEntities.Contains(box))
                {
                    continue;
                }
                AddNode(new BoxConflictNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var agent in state.GetAgents(level))
            {
                if (removedEntities.Contains(agent))
                {
                    continue;
                }
                AddNode(new BoxConflictNode(new EntityNodeInfo(agent, EntityType.AGENT)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(gsData, this, level, EntityType.GOAL);
        }

        internal Dictionary<Point, INode> getPositionToNode()
        {
            return PositionToNode;
        }

        internal void AddNode(BoxConflictNode node)
        {
            base.AddNode(node);
            PositionToNode.Add(node.Value.Ent.Pos, node);
        }

        internal void AddNode(FreeSpaceNode node)
        {
            base.AddNode(node);
            foreach (var nodePos in node.Value.FreeSpaces)
            {
                PositionToNode.Add(nodePos, node);
            }
        }

        internal void AddGoalNodes(GraphSearchData gsData, Level level, Entity exceptThisGoal, HashSet<Goal> solvedGoals)
        {
            foreach (var node in Nodes)
            {
                if (node is BoxConflictNode boxNode)
                {
                    level.AddWall(boxNode.Value.Ent.Pos);
                }
            }

            var goalCondition = new Func<(Point pos, int distance), GraphSearcher.GoalFound<(Point pos, int distance)>>(x =>
            {
                return new GraphSearcher.GoalFound<(Point, int)>(x, PositionHasNode(x.pos));
            });
            foreach (var goal in level.Goals)
            {
                if (solvedGoals.Contains(goal))
                {
                    continue;
                }
                if (goal.Ent == exceptThisGoal)
                {
                    continue;
                }

                BoxConflictNode node = new BoxConflictNode(new EntityNodeInfo(goal.Ent, goal.EntType));

                List<(Point pos, int distance)> edges = GraphSearcher.GetReachedGoalsBFS(gsData, level, goal.Ent.Pos, goalCondition);
                foreach (var edge in edges.Distinct())
                {
                    if (GetNodeFromPosition(edge.pos) is BoxConflictNode boxEnd)
                    {
                        node.AddEdge(new Edge<DistanceEdgeInfo>(boxEnd, new DistanceEdgeInfo(edge.distance)));
                        boxEnd.AddEdge(new Edge<DistanceEdgeInfo>(node, new DistanceEdgeInfo(edge.distance)));
                    }
                    else if (GetNodeFromPosition(edge.pos) is FreeSpaceNode freeEnd)
                    {
                        node.AddEdge(new Edge<DistanceEdgeInfo>(freeEnd, new DistanceEdgeInfo()));
                        freeEnd.AddEdge(new Edge<DistanceEdgeInfo>(node, new DistanceEdgeInfo()));
                    }

                }

                Nodes.Add(node);
            }

            level.ResetWalls();
        }

        internal void RemoveGoalNodes()
        {
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                INode node = Nodes[i];
                if (node is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal())
                {
                    Nodes.RemoveAt(i);
                    node.RemoveNode();
                }
            }
        }

        internal INode GetNodeFromPosition(Point pos)
        {
            return PositionToNode[pos];
        }
        internal bool PositionHasNode(Point pos)
        {
            return PositionToNode.ContainsKey(pos);
        }

        internal void AddFreeSpaceNodes(GraphSearchData gsData, Level level)
        {
            //
            //All entities need to be made into walls so the only freepsace is space 
            //that won't block the path or be on top of other entities.
            //

            HashSet<Point> agentPositions = new HashSet<Point>();
            foreach (var inode in Nodes)
            {
                if (inode is BoxConflictNode boxNode)
                {
                    level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = true;
                    if (boxNode.Value.EntType == EntityType.AGENT)
                    {
                        agentPositions.Add(boxNode.Value.Ent.Pos);
                    }
                }
            }


            //
            //Now go through the map and and find all empty spaces. When an empty space is found, bfs is used to
            //find all connecting spaces which makes up the free space node.
            //

            var foundFreeSpace = new Func<(Point pos, int distance), GraphSearcher.GoalFound<Point>>(x =>
            {
                return new GraphSearcher.GoalFound<Point>(x.pos, !level.IsWall(x.pos));
            });
            HashSet<Point> alreadySeenSpaces = new HashSet<Point>();
            List<FreeSpaceNode> freeSpaceNodes = new List<FreeSpaceNode>();
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (!level.Walls[x, y] && !alreadySeenSpaces.Contains(new Point(x, y)))
                    {
                        //There is an issue here.
                        //The list has a duplicate in it.
                        //It's currently handled by inserting it
                        //into a hashset.
                        var freeSpacesFound = GraphSearcher.GetReachedGoalsBFS(gsData, level, new Point(x, y), foundFreeSpace);

                        //A single free space can't be part of multiple nodes
                        alreadySeenSpaces.UnionWith(freeSpacesFound);

                        //Create node and add it to the graph.
                        //keep a list of the freespace nodes so edges can be added later
                        var newFreeSpaceNode = new FreeSpaceNode(new FreeSpaceNodeInfo(freeSpacesFound));
                        AddNode(newFreeSpaceNode);
                        freeSpaceNodes.Add(newFreeSpaceNode);
                    }
                }
            }


            //
            //Now it's time to add edges between the free spaces nodes and the rest of the graph.
            //To do that the original map has to be restored as the path may block pathways from 
            //a freespace to any other node on the map. Agents and boxes still have to be walls
            //as there is no direct path through them. This excludes goals as only goals with
            //boxes on them should be walls and the boxes on top of them will makes them walls.
            //Go through all free space nodes and use bfs to check which other nodes it can reach,
            //then add edges to those nodes.
            //

            level.ResetWalls();
            foreach (var inode in Nodes)
            {
                if (inode is BoxConflictNode boxNode && !boxNode.Value.EntType.IsGoal())
                {
                    level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = true;
                }
            }

            var foundNode = new Func<(Point pos, int distance), GraphSearcher.GoalFound<INode>>(x =>
            {
                if (PositionToNode.TryGetValue(x.pos, out INode node))
                {
                    return new GraphSearcher.GoalFound<INode>(node, true);
                }
                return new GraphSearcher.GoalFound<INode>(null, false);
            });
            foreach (var freeSpaceNode in freeSpaceNodes)
            {
                var nodesFound = GraphSearcher.GetReachedGoalsBFS(gsData, level, freeSpaceNode.Value.FreeSpaces.First(), foundNode);
                foreach (var neighbour in nodesFound.ToHashSet())
                {
                    //The search may find itself and such edges are not necessary
                    if (neighbour != freeSpaceNode)
                    {
                        //Bidirectional edges
                        freeSpaceNode.AddEdge(new Edge<DistanceEdgeInfo>(neighbour, new DistanceEdgeInfo()));
                        if (neighbour is BoxConflictNode boxNode)
                        {
                            boxNode.AddEdge(new Edge<DistanceEdgeInfo>(freeSpaceNode, new DistanceEdgeInfo()));
                        }
                        else if (neighbour is FreeSpaceNode freeNode)
                        {
                            freeNode.AddEdge(new Edge<DistanceEdgeInfo>(freeSpaceNode, new DistanceEdgeInfo()));
                        }
                    }
                }
            }
            level.ResetWalls();

            foreach (var agentPos in agentPositions)
            {
                foreach (var dirDelta in Direction.NONE.DirectionDeltas())
                {
                    Point nextToAgent = agentPos + dirDelta;
                    if (PositionHasNode(nextToAgent))
                    {
                        INode node = GetNodeFromPosition(nextToAgent);
                        if (node is FreeSpaceNode freeSpaceNode)
                        {
                            freeSpaceNode.Value.FreeSpaces.Add(agentPos);
                            continue;
                        }
                    }
                }
            }
        }
    }
}
