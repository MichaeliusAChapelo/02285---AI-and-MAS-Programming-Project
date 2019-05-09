using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal readonly struct Edge<E>
    {
        public readonly INode End;
        public readonly E Value;

        public Edge(INode end, E value)
        {
            this.End = end;
            this.Value = value;
        }
    }
}
