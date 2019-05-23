using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal readonly struct Goal
    {
        public readonly Entity Ent;
        public readonly EntityType EntType;

        public Goal(Entity ent, EntityType entType)
        {
            this.Ent = ent;
            this.EntType = entType;
        }
    }
}
