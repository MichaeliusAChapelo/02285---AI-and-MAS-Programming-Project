using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal class Edge<N, E>
    {
        public readonly Node<N, E> End;
        public readonly E Value;

        public Edge(Node<N, E> end, E value)
        {
            this.End = end;
            this.Value = value;
        }
    }
}
