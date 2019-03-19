using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal readonly struct Goal
    {
        public readonly Point Pos;
        public readonly char Type;

        public Goal(Point Pos, char Type)
        {
            this.Pos = Pos;
            this.Type = Type;
        }
    }
}
