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
            return Value.Ent.Type.ToString();
        }
    }

    internal class GoalEdge : Edge<EntityNodeInfo, EmptyEdgeInfo>
    {
        public GoalEdge(Node<EntityNodeInfo, EmptyEdgeInfo> end, EmptyEdgeInfo value) : base(end, value)
        {
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal class GoalGraph : Graph<EntityNodeInfo, EmptyEdgeInfo>
    {
        public GoalGraph(State state, Level level)
        {
            foreach (var box in state.GetBoxes(level))
            {
                Nodes.Add(new GoalNode(new EntityNodeInfo(box, EntityType.BOX)));
            }
            foreach (var goal in level.Goals)
            {
                Nodes.Add(new GoalNode(new EntityNodeInfo(goal, EntityType.GOAL)));
                level.Walls[goal.Pos.X, goal.Pos.Y] = true;
            }

            List<Point> potentialGoals = new List<Point>();
            foreach (var node in Nodes)
            {
                potentialGoals.Add(node.Value.Ent.Pos);
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                GoalNode node = (GoalNode)Nodes[i];
                level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = false;
                potentialGoals.Remove(node.Value.Ent.Pos);

                var reachedGoals = GraphSearcher.GetReachedGoalsBFS(level, node.Value.Ent.Pos, potentialGoals);

                List<GoalEdge> edges = new List<GoalEdge>();
                foreach (Point reached in reachedGoals)
                {
                    GoalNode target = (GoalNode)Nodes.Single(x => x.Value.Ent.Pos == reached);
                    if (node.Value.EntType == EntityType.BOX && 
                        target.Value.EntType == EntityType.BOX)
                    {
                        continue;
                    }
                    node.AddEdge(new GoalEdge(target, new EmptyEdgeInfo()));
                }

                potentialGoals.Add(node.Value.Ent.Pos);
                if (node.Value.EntType != EntityType.BOX)
                {
                    level.Walls[node.Value.Ent.Pos.X, node.Value.Ent.Pos.Y] = true;
                }
            }

            foreach (var goal in level.Goals)
            {
                level.Walls[goal.Pos.X, goal.Pos.Y] = false;
            }
        }
    }
}
