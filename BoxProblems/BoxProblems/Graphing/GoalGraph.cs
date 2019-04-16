using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    internal readonly struct EntityNodeInfo
    {
        public readonly Entity Ent;
        public readonly EntityType EntType;

        public EntityNodeInfo(Entity ent, EntityType entType)
        {
            this.Ent = ent;
            this.EntType = entType;
        }
    }

    internal readonly struct EmptyEdgeInfo
    {

    }

    internal class GoalNode : Node<EntityNodeInfo, EmptyEdgeInfo>
    {
        public GoalNode(EntityNodeInfo value) : base(value)
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

    internal class GoalEdge : Edge<EntityNodeInfo, EmptyEdgeInfo>
    {
        public GoalEdge(GoalNode end, EmptyEdgeInfo value) : base(end, value)
        {
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal sealed class GoalGraph : Graph
    {
        private readonly Dictionary<Point, GoalNode> PositionToNode = new Dictionary<Point, GoalNode>();

        public GoalGraph(State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                AddNode(new GoalNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var goal in level.Goals)
            {
                AddNode(new GoalNode(new EntityNodeInfo(goal, EntityType.GOAL)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(this, level, EntityType.BOX);
        }

        public void AddNode(GoalNode node)
        {
            base.AddNode(node);
            PositionToNode.Add(node.Value.Ent.Pos, node);
        }

        public GoalNode GetNodeFromPosition(Point pos)
        {
            return PositionToNode[pos];
        }
    }
}
