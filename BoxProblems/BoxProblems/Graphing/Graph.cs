using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems.Graphing
{
    internal class Graph<N, E>
    {
        public readonly List<Node<N, E>> Nodes = new List<Node<N, E>>();
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
                foreach (var edge in Nodes[i].Edges)
                {
                    if (foundNodes.Contains(edge.End))
                    {
                        continue;
                    }
                    foundNodes.Add(Nodes[i]);
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
            var groupedGraph = new Graph<NodeGroup<N, E>, E>();
            List<NodeGroup<N, E>> groups = new List<NodeGroup<N, E>>();
            Dictionary<Node<N, E>, NodeGroup<N, E>> nodeToGroup = new Dictionary<Node<N, E>, NodeGroup<N, E>>(); 
            foreach (var node in graph.Nodes)
            {
                var equalGroups = groups.Where(x => node.Edges.Count == x.EdgesTo.Count - 1 && node.Edges.All(y => x.EdgesTo.Contains(y.End)));
                if (equalGroups.Count() == 0)
                {
                    var newGroup = new NodeGroup<N, E>(true);
                    groups.Add(newGroup);

                    newGroup.Nodes.Add(node);
                    newGroup.EdgesTo.AddRange(node.Edges.Select(x => x.End));
                    newGroup.EdgesTo.Add(node);
                    nodeToGroup.Add(node, newGroup);
                }
                else
                {
                    equalGroups.First().Nodes.Add(node);
                    nodeToGroup.Add(node, equalGroups.First());
                }
            }

            Dictionary<NodeGroup<N, E>, Node<NodeGroup<N, E>, E>> groupToNode = new Dictionary<NodeGroup<N, E>, Node<NodeGroup<N, E>, E>>();
            foreach (var group in groups)
            {
                var newNode = new Node<NodeGroup<N, E>, E>(group);
                groupedGraph.AddNode(newNode);
                groupToNode.Add(group, newNode);
            }

            foreach (var groupNode in groupedGraph.Nodes)
            {
                HashSet<Node<NodeGroup<N, E>, E>> alreadyCreatedEdges = new HashSet<Node<NodeGroup<N, E>, E>>();
                foreach (var edgeNode in groupNode.Value.EdgesTo)
                {
                    var edgeGroupNode = groupToNode[nodeToGroup[edgeNode]];
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
        public readonly List<Node<N, E>> EdgesTo;

        public NodeGroup(bool _)
        {
            this.Nodes = new List<Node<N, E>>();
            this.EdgesTo = new List<Node<N, E>>();
        }

        public override string ToString()
        {
            return string.Join(", ", Nodes);
        }
    }
}
