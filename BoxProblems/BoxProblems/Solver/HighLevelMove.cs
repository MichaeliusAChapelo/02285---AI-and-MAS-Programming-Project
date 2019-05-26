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
        internal Point? UTurnPos;


        internal HighlevelMove(State state, Entity moveThis, Point toHere, Entity? usingThisAgent, Point? agentFinalPos, Point? uTurnPos)
        {
            this.CurrentState = state;
            this.MoveThis = moveThis;
            this.ToHere = toHere;
            this.UsingThisAgent = usingThisAgent;
            this.AgentFinalPos = agentFinalPos;
            this.UTurnPos = uTurnPos;
        }

        public override string ToString()
        {
            return $"{MoveThis} -> {ToHere} " + (UsingThisAgent.HasValue ? $"Using {UsingThisAgent} to {AgentFinalPos}" : string.Empty);
        }
    }
}
