using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal interface INode
    {
        IEnumerable<INode> GetNodeEnds();
        void RemoveEdge(INode toRemove);
        void RemoveBiDirectionalEdge(INode toRemove);
        void DisconnectNode();
    }
}
