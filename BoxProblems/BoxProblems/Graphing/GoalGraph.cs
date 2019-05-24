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

    internal class GoalNode : Node<EntityNodeInfo, DistanceEdgeInfo>
    {
        public GoalNode(EntityNodeInfo value) : base(value)
        {
        }

        public override string ToString()
        {
            if (Value.EntType.IsGoal())
            {
                return char.ToLower(Value.Ent.Type).ToString() + " " + Value.Ent.Pos;
            }
            else
            {
                return Value.Ent.Type.ToString();
            }            
        }
    }

    internal sealed class GoalGraph : Graph
    {
        private readonly Dictionary<Point, GoalNode> PositionToGoalNode = new Dictionary<Point, GoalNode>();
        private readonly Dictionary<Point, GoalNode> PositionToMoveableNode = new Dictionary<Point, GoalNode>();

        public GoalGraph(GraphSearchData gsData, State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                AddNode(new GoalNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var agent in state.GetAgents(level))
            {
                AddNode(new GoalNode(new EntityNodeInfo(agent, EntityType.AGENT)));
            }
            foreach (var goal in level.Goals)
            {
                AddNode(new GoalNode(new EntityNodeInfo(goal.Ent, goal.EntType)));
            }

            GraphCreator.CreateGraphIgnoreEntityType(gsData, this, this.Nodes, level, EntityType.MOVEABLE);
        }

        public void AddNode(GoalNode node)
        {
            base.AddNode(node);
            if (node.Value.EntType.IsGoal())
            {
                PositionToGoalNode.Add(node.Value.Ent.Pos, node);
            }
            else if (node.Value.EntType.IsMoveable())
            {
                PositionToMoveableNode.Add(node.Value.Ent.Pos, node);
            }
            else
            {
                throw new Exception("GoalGraph does not support any other entity type than goal, box and agent.");
            }
        }

        public GoalNode GetGoalNodeFromPosition(Point pos)
        {
            return PositionToGoalNode[pos];
        }

        public GoalNode GetMoveableNodeFromPosition(Point pos)
        {
            return PositionToMoveableNode[pos];
        }
    }
}
