using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal class State
    {
        State Parent;
        Entity[] Entities;
        //Command CMD;
        int G;

        public State(State parent, Entity[] entities, int g)
        {
            this.Parent = parent;
            this.Entities = entities;
            this.G = g;
        }

        public Span<Entity> GetAgents(Level level)
        {
            return new Span<Entity>(Entities, 0, level.AgentCount);
        }

        public Span<Entity> GetBoxes(Level level)
        {
            return new Span<Entity>(Entities, level.AgentCount, level.BoxCount);
        }
    }
}
