using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Graphing
{
    internal class BoxConflictNode : Node<EntityNodeInfo, EmptyEdgeInfo>
    {
        public BoxConflictNode(EntityNodeInfo value) : base(value)
        {
        }

        public override string ToString()
        {
            if (Value.EntType == EntityType.GOAL)
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

    internal class FreeSpaceNode : Node<FreeSpaceNodeInfo, EmptyEdgeInfo>
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

        internal BoxConflictGraph(State state, Level level, Entity? goal,HashSet<Entity> removedEntities)
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
            if (goal.HasValue)
            {
                AddNode(new BoxConflictNode(new EntityNodeInfo(goal.Value, EntityType.GOAL)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(this, level, EntityType.GOAL);
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

        internal INode GetNodeFromPosition(Point pos)
        {
            return PositionToNode[pos];
        }
        internal bool PositionHasNode(Point pos)
        {
            return PositionToNode.ContainsKey(pos);
        }

        internal void AddFreeNodes(Level level, Point start, Point end)
        {
            var pathsMap = Precomputer.GetPathMap(level.Walls, end, false);
            var distancesMap = Precomputer.GetDistanceMap(level.Walls, end, false);

            //
            //First of all the path from start to end and all entities need to be made into
            //walls so the only freepsace is space that won't block the path or be on
            //top of other entities.
            //

            foreach (var inode in Nodes)
            {
                if (inode is BoxConflictNode boxNode)
                {
                    level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = true;
                }
            }

            Point currentPos = start;
            for (int i = 0; i < distancesMap[start.X, start.Y]; i++)
            {
                level.Walls[currentPos.X, currentPos.Y] = true;
                currentPos = currentPos + pathsMap[currentPos.X, currentPos.Y].DirectionDelta();
            }
            level.Walls[currentPos.X, currentPos.Y] = true;


            //
            //Now go through the map and and find all empty spaces. When an empty space is found, bfs is used to
            //find all connecting spaces which makes up the free space node.
            //

            Func<Point, GraphSearcher.GoalFound<Point>> foundFreeSpace = new Func<Point, GraphSearcher.GoalFound<Point>>(x =>
            {
                return new GraphSearcher.GoalFound<Point>(x, !level.Walls[x.X, x.Y]);
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
                        var freeSpacesFound = GraphSearcher.GetReachedGoalsBFS(level, new Point(x, y), foundFreeSpace);

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
                if (inode is BoxConflictNode boxNode && boxNode.Value.EntType != EntityType.GOAL)
                {
                    level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = true;
                }
            }

            Func<Point, GraphSearcher.GoalFound<INode>> foundNode = new Func<Point, GraphSearcher.GoalFound<INode>>(x =>
            {
                if (PositionToNode.TryGetValue(x, out INode node))
                {
                    return new GraphSearcher.GoalFound<INode>(node, true);
                }
                return new GraphSearcher.GoalFound<INode>(null, false);
            });
            foreach (var freeSpaceNode in freeSpaceNodes)
            {
                var nodesFound = GraphSearcher.GetReachedGoalsBFS(level, freeSpaceNode.Value.FreeSpaces.First(), foundNode);
                foreach (var neighbour in nodesFound.ToHashSet())
                {
                    //The search may find itself and such edges are not necessary
                    if (neighbour != freeSpaceNode)
                    {
                        //Bidirectional edges
                        freeSpaceNode.AddEdge(new Edge<FreeSpaceNodeInfo, EmptyEdgeInfo>(neighbour, new EmptyEdgeInfo()));
                        if (neighbour is BoxConflictNode boxNode)
                        {
                            boxNode.AddEdge(new Edge<EntityNodeInfo, EmptyEdgeInfo>(freeSpaceNode, new EmptyEdgeInfo()));
                        }
                        else if (neighbour is FreeSpaceNode freeNode)
                        {
                            freeNode.AddEdge(new Edge<FreeSpaceNodeInfo, EmptyEdgeInfo>(freeSpaceNode, new EmptyEdgeInfo()));
                        }
                    }
                }
            }
            level.ResetWalls();
        }
    }
}
