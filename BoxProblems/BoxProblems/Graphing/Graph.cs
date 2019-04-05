using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal class Graph<N, E>
    {
        public readonly List<Node<N, E>> Nodes = new List<Node<N, E>>();

        protected void AddNode(Node<N, E> node)
        {
            Nodes.Add(node);
        }

        public (string nodes, string edges) ToCytoscapeString()
        {
            StringBuilder nodesBuilder = new StringBuilder();
            StringBuilder edgesBuilder = new StringBuilder();

            HashSet<Node<N, E>> foundNodes = new HashSet<Node<N, E>>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                nodesBuilder.Append($"{{ data: {{ id: '{i}', label: '{Nodes[i]}' }} }},");
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                foreach (var edge in Nodes[i].Edges)
                {
                    if (foundNodes.Contains(edge.End))
                    {
                        continue;
                    }
                    foundNodes.Add(Nodes[i]);
                    edgesBuilder.Append($"{{ data: {{ source: '{i}', target: '{Nodes.IndexOf(edge.End)}' }} }},");
                }
            }

            return ($"[{nodesBuilder.ToString()}]", $"[{edgesBuilder.ToString()}]");
        }
    }
}
