using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    public class Graph
    {
        internal readonly List<INode> Nodes = new List<INode>();
        private static int asdsa = 0;

        internal void AddNode(INode node)
        {
            Nodes.Add(node);
        }

        public (string nodes, string edges) ToCytoscapeString()
        {
            StringBuilder nodesBuilder = new StringBuilder();
            StringBuilder edgesBuilder = new StringBuilder();
            int nodes = 0;
            int edges = 0;

            HashSet<INode> foundNodes = new HashSet<INode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                nodesBuilder.Append($"{{ data: {{ id: '{asdsa + i}', label: '{Nodes[i]}' }} }},");
                nodes++;
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                foreach (var edge in Nodes[i].GetNodeEnds())
                {
                    if (foundNodes.Contains(edge))
                    {
                        continue;
                    }
                    foundNodes.Add(Nodes[i]);
                    edgesBuilder.Append($"{{ data: {{ source: '{asdsa + i}', target: '{asdsa + Nodes.IndexOf(edge)}' }} }},");
                    edges++;
                }
            }
            asdsa += Nodes.Count;
            //Console.WriteLine($"Nodes: {nodes}");
            //Console.WriteLine($"Edges: {edges}");
            return (nodesBuilder.ToString(), edgesBuilder.ToString());
        }

        public static Graph CreateSimplifiedGraph<E>(Graph graph) where E : new()
        {
            Graph groupedGraph = new Graph();
            Dictionary<INode, Node<NodeGroup, E>> nodeToGroupNode = new Dictionary<INode, Node<NodeGroup, E>>(); 
            foreach (var inode in graph.Nodes)
            {
                var node = inode;
                var equalGroup = groupedGraph.Nodes.Cast<Node<NodeGroup, E>>().SingleOrDefault(x => node.GetNodeEnds().Count() == x.Value.EdgesTo.Count - 1 && node.GetNodeEnds().All(y => x.Value.EdgesTo.Contains(y)));
                if (equalGroup == default(Node<NodeGroup, E>))
                {
                    equalGroup = new Node<NodeGroup, E>(new NodeGroup(true));
                    groupedGraph.AddNode(equalGroup);

                    equalGroup.Value.EdgesTo.UnionWith(node.GetNodeEnds());
                    equalGroup.Value.EdgesTo.Add(node);
                }

                equalGroup.Value.Nodes.Add(node);
                nodeToGroupNode.Add(node, equalGroup);
            }

            HashSet<Node<NodeGroup, E>> alreadyCreatedEdges = new HashSet<Node<NodeGroup, E>>();
            foreach (var igroupNode in groupedGraph.Nodes)
            {
                var groupNode = (Node<NodeGroup, E>)igroupNode;
                alreadyCreatedEdges.Clear();
                foreach (var edgeNode in groupNode.Value.EdgesTo)
                {
                    var edgeGroupNode = nodeToGroupNode[edgeNode];
                    if (!alreadyCreatedEdges.Contains(edgeGroupNode) && groupNode != edgeGroupNode)
                    {
                        groupNode.AddEdge(new Edge<NodeGroup, E>(edgeGroupNode, new E()));
                        alreadyCreatedEdges.Add(edgeGroupNode);
                    }
                }
            }

            return groupedGraph;
        }
    }

    internal readonly struct NodeGroup
    {
        public readonly List<INode> Nodes;
        public readonly HashSet<INode> EdgesTo;

        public NodeGroup(bool _)
        {
            this.Nodes = new List<INode>();
            this.EdgesTo = new HashSet<INode>();
        }

        public override string ToString()
        {
            return string.Join(", ", Nodes);
        }
    }
}
