using System.Collections.Generic;

//#nullable enable

namespace BoxProblems
{
    public readonly struct AgentCommands
    {
        internal readonly List<AgentCommand> Commands;
        internal readonly int AgentIndex;

        internal AgentCommands(List<AgentCommand> commands, int agentIndex)
        {
            this.Commands = commands;
            this.AgentIndex = agentIndex;
        }
    }
}