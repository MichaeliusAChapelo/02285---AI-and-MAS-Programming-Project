using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Solver
{
    public class HighlevelMove
    {
        internal State CurrentState;
        internal int MoveThisIndex;
        internal Point ToHere;
        internal int? UsingThisAgentIndex;
        internal int MoveNumber;

        internal HighlevelMove(State state, int moveThisIndex, Point toHere, int? usingThisAgentIndex, int moveNumber)
        {
            this.CurrentState = state;
            this.MoveThisIndex = moveThisIndex;
            this.ToHere = toHere;
            this.UsingThisAgentIndex = usingThisAgentIndex;
            this.MoveNumber = moveNumber;
        }

        public override string ToString()
        {
            return $"{MoveThisIndex} -> {ToHere} " + (UsingThisAgentIndex.HasValue ? $"Using {UsingThisAgentIndex}" : string.Empty);
        }
    }
}
