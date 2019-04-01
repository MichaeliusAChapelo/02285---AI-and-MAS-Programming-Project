using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal class Node<N, E>
    {
        public readonly N Value;
        public readonly List<Edge<N, E>> Edges = new List<Edge<N, E>>();

        public Node(N value)
        {
            this.Value = value;
        }

        public void AddEdge(Edge<N, E> edge)
        {
            Edges.Add(edge);
        }
    }
}
