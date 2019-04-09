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
                return Value.Ent.Type.ToString();
            }
        }
    }

    internal class BoxConflictEdge : Edge<EntityNodeInfo, EmptyEdgeInfo>
    {
        public BoxConflictEdge(BoxConflictNode end, EmptyEdgeInfo value) : base(end, value)
        {
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal sealed class BoxConflictGraph : Graph<EntityNodeInfo, EmptyEdgeInfo>
    {
        private readonly Dictionary<Point, BoxConflictNode> PositionToNode = new Dictionary<Point, BoxConflictNode>();

        public BoxConflictGraph(State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                AddNode(new BoxConflictNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var box in state.GetAgents(level))
            {
                AddNode(new BoxConflictNode(new EntityNodeInfo(box, EntityType.AGENT)));
            }
            foreach (var goal in level.Goals)
            {
                AddNode(new BoxConflictNode(new EntityNodeInfo(goal, EntityType.GOAL)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(this, level, EntityType.GOAL);
        }

        public void AddNode(BoxConflictNode node)
        {
            base.AddNode(node);
            PositionToNode.Add(node.Value.Ent.Pos, node);
        }

        public BoxConflictNode GetNodeFromPosition(Point pos)
        {
            return PositionToNode[pos];
        }

        public void AddFreeNodes(Level level, Point start, Point end)
        {
            foreach (var inode in Nodes)
            {
                if (inode is BoxConflictNode boxNode)
                {
                    if (boxNode.Value.EntType == EntityType.AGENT || boxNode.Value.EntType == EntityType.BOX)
                    {
                        level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = true;
                    }
                }
            }

            level.Walls[start.X, start.Y] = false;
            level.Walls[end.X, end.Y] = false;
            var pathsMap = Precomputer.GetPathMap(level.Walls, start, false);
            Point currentPos = start;
            while (pathsMap[currentPos.X, currentPos.Y] != Direction.NONE)
            {
                level.Walls[currentPos.X, currentPos.Y] = true;
                currentPos += pathsMap[currentPos.X, currentPos.Y].DirectionDelta();
            }


            HashSet<Point> alreadySeenSpaces = new HashSet<Point>();



            currentPos = start;
            while (pathsMap[currentPos.X, currentPos.Y] != Direction.NONE)
            {
                level.Walls[currentPos.X, currentPos.Y] = false;
                currentPos += pathsMap[currentPos.X, currentPos.Y].DirectionDelta();
            }

            foreach (var inode in Nodes)
            {
                if (inode is BoxConflictNode boxNode)
                {
                    if (boxNode.Value.EntType == EntityType.AGENT || boxNode.Value.EntType == EntityType.BOX)
                    {
                        level.Walls[boxNode.Value.Ent.Pos.X, boxNode.Value.Ent.Pos.Y] = false;
                    }
                }
            }
        }
    }
}
