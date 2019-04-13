using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Graphing
{
    internal interface INode
    {
        IEnumerable<INode> GetNodeEnds();
    }
}
