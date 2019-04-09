using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    internal class Graph<N, E>
    {
        public readonly List<INode> Nodes = new List<INode>();
        private static int asdsa = 0;

        protected void AddNode(Node<N, E> node)
        {
            Nodes.Add(node);
        }

        public (string nodes, string edges) ToCytoscapeString()
        {
            StringBuilder nodesBuilder = new StringBuilder();
            StringBuilder edgesBuilder = new StringBuilder();
            int nodes = 0;
            int edges = 0;

            HashSet<Node<N, E>> foundNodes = new HashSet<Node<N, E>>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                nodesBuilder.Append($"{{ data: {{ id: '{asdsa + i}', label: '{Nodes[i]}' }} }},");
                nodes++;
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = (Node<N, E>)Nodes[i];
                foreach (var edge in node.Edges)
                {
                    if (foundNodes.Contains(edge.End))
                    {
                        continue;
                    }
                    foundNodes.Add(node);
                    edgesBuilder.Append($"{{ data: {{ source: '{asdsa + i}', target: '{asdsa + Nodes.IndexOf(edge.End)}' }} }},");
                    edges++;
                }
            }
            asdsa += Nodes.Count;
            Console.WriteLine($"Nodes: {nodes}");
            Console.WriteLine($"Edges: {edges}");
            return (nodesBuilder.ToString(), edgesBuilder.ToString());
        }

        public static Graph<NodeGroup<N, E>, E> CreateSimplifiedGraph<N, E>(Graph<N, E> graph) where E : new()
        {
            Graph<NodeGroup<N, E>, E> groupedGraph = new Graph<NodeGroup<N, E>, E>();
            Dictionary<Node<N, E>, Node<NodeGroup<N, E>, E>> nodeToGroupNode = new Dictionary<Node<N, E>, Node<NodeGroup<N, E>, E>>(); 
            foreach (var inode in graph.Nodes)
            {
                var node = (Node<N, E>)inode;
                var equalGroup = groupedGraph.Nodes.Cast<Node<NodeGroup<N, E>, E>>().SingleOrDefault(x => node.Edges.Count == x.Value.EdgesTo.Count - 1 && node.Edges.All(y => x.Value.EdgesTo.Contains(y.End)));
                if (equalGroup == default(Node<NodeGroup<N, E>, E>))
                {
                    equalGroup = new Node<NodeGroup<N, E>, E>(new NodeGroup<N, E>(true));
                    groupedGraph.AddNode(equalGroup);

                    equalGroup.Value.EdgesTo.UnionWith(node.Edges.Select(x => (Node<N, E>)x.End));
                    equalGroup.Value.EdgesTo.Add(node);
                }

                equalGroup.Value.Nodes.Add(node);
                nodeToGroupNode.Add(node, equalGroup);
            }

            HashSet<Node<NodeGroup<N, E>, E>> alreadyCreatedEdges = new HashSet<Node<NodeGroup<N, E>, E>>();
            foreach (var igroupNode in groupedGraph.Nodes)
            {
                var groupNode = (Node<NodeGroup<N, E>, E>)igroupNode;
                alreadyCreatedEdges.Clear();
                foreach (var edgeNode in groupNode.Value.EdgesTo)
                {
                    var edgeGroupNode = nodeToGroupNode[edgeNode];
                    if (!alreadyCreatedEdges.Contains(edgeGroupNode) && groupNode != edgeGroupNode)
                    {
                        groupNode.AddEdge(new Edge<NodeGroup<N, E>, E>(edgeGroupNode, new E()));
                        alreadyCreatedEdges.Add(edgeGroupNode);
                    }
                }
            }

            return groupedGraph;
        }
    }

    internal readonly struct NodeGroup<N, E>
    {
        public readonly List<Node<N, E>> Nodes;
        public readonly HashSet<Node<N, E>> EdgesTo;

        public NodeGroup(bool _)
        {
            this.Nodes = new List<Node<N, E>>();
            this.EdgesTo = new HashSet<Node<N, E>>();
        }

        public override string ToString()
        {
            return string.Join(", ", Nodes);
        }
    }
}
