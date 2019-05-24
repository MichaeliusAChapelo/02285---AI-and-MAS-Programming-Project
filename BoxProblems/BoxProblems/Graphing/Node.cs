using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal class Node<N, E> : INode
    {
        public readonly N Value;
        public readonly List<Edge<E>> Edges = new List<Edge<E>>();

        public Node(N value)
        {
            this.Value = value;
        }

        public void AddEdge(Edge<E> edge)
        {
            Edges.Add(edge);
        }

        public IEnumerable<INode> GetNodeEnds()
        {
            foreach (var edge in Edges)
            {
                yield return edge.End;
            }
        }

        public void RemoveBiDirectionalEdge(INode toRemove)
        {
            RemoveEdge(toRemove);
            toRemove.RemoveEdge(toRemove);
        }

        public void RemoveEdge(INode toRemove)
        {
            for (int i = Edges.Count - 1; i >= 0; i--)
            {
                if (Edges[i].End == toRemove)
                {
                    Edges.RemoveAt(i);
                    break;
                }
            }
        }

        public void DisconnectNode()
        {
            foreach (var edge in Edges)
            {
                edge.End.RemoveEdge(this);
            }
            Edges.Clear();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
