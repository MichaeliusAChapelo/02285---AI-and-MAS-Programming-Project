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

    internal class BoxConflictGraph : Graph<EntityNodeInfo, EmptyEdgeInfo>
    {
        public BoxConflictGraph(State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                Nodes.Add(new BoxConflictNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var box in state.GetAgents(level))
            {
                Nodes.Add(new BoxConflictNode(new EntityNodeInfo(box, EntityType.AGENT)));
            }
            foreach (var goal in level.Goals)
            {
                Nodes.Add(new BoxConflictNode(new EntityNodeInfo(goal, EntityType.GOAL)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(this, level, EntityType.GOAL);
        }
    }
}
