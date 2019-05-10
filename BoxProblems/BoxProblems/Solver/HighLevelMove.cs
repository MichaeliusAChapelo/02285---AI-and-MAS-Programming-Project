using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Solver
{
    public class HighlevelMove
    {
        internal State CurrentState;
        internal Entity MoveThis;
        internal Point ToHere;
        internal Entity? UsingThisAgent;
        internal Point? AgentFinalPos;
        internal int MoveNumber;

        internal HighlevelMove(State state, Entity moveThis, Point toHere, Entity? usingThisAgent, Point? agentFinalPos, int moveNumber)
        {
            this.CurrentState = state;
            this.MoveThis = moveThis;
            this.ToHere = toHere;
            this.UsingThisAgent = usingThisAgent;
            this.AgentFinalPos = agentFinalPos;
            this.MoveNumber = moveNumber;
        }

        public override string ToString()
        {
            return $"{MoveThis} -> {ToHere} " + (UsingThisAgent.HasValue ? $"Using {UsingThisAgent}" : string.Empty);
        }
    }
}
