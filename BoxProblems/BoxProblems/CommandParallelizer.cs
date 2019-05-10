using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    public static class CommandParallelizer
    {
        private readonly struct FreeTime
        {
            public readonly int StartTime;
            public readonly int EndTime;

            public FreeTime(int start, int end)
            {
                this.StartTime = start;
                this.EndTime = end;
            }
        }

        public static string[] Parallelize(List<AgentCommands> allCommands, Level level)
        {
            List<FreeTime>[,] world = new List<FreeTime>[level.Width, level.Height];
            Point[] agentPositions = new Point[level.AgentCount];
            List<string[]> parallelizedCommands = new List<string[]>();

            Span<Entity> initialAgents = level.GetAgents();
            for (int i = 0; i < initialAgents.Length; i++)
            {
                agentPositions[i] = initialAgents[i].Pos;
            }

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    world[x, y] = new List<FreeTime>();
                    world[x, y].Add(new FreeTime(0, int.MaxValue));
                }
            }

            foreach (AgentCommands agentCommands in allCommands)
            {
                foreach (AgentCommand command in agentCommands.Commands)
                {
                    Point agentPos = agentPositions[agentCommands.AgentIndex];
                    Point newAgentPos = command.GetNextAgentPos(agentPos);

                    int time = world[agentPos.X, agentPos.Y];
                    int newTime = Math.Max(world[agentPos.X, agentPos.Y], world[newAgentPos.X, newAgentPos.Y]);

                    if (command.CType != CommandType.MOVE)
                    {
                        Point boxPos = command.GetBoxPos(agentPos);
                        Point nextBoxPos = command.GetNextBoxPos(boxPos);

                        newTime = Math.Max(newTime, world[boxPos.X, boxPos.Y]);
                        newTime = Math.Max(newTime, world[nextBoxPos.X, nextBoxPos.Y]);

                        world[boxPos.X, boxPos.Y] = newTime + 1;
                        world[nextBoxPos.X, nextBoxPos.Y] = newTime + 1;
                    }

                    for (int i = parallelizedCommands.Count; i <= newTime; i++)
                    {
                        parallelizedCommands.Add(new string[level.AgentCount]);
                    }
                    parallelizedCommands[newTime][agentCommands.AgentIndex] = command.ToString();

                    world[agentPos.X, agentPos.Y] = newTime + 1;
                    world[newAgentPos.X, newAgentPos.Y] = newTime + 1;
                    agentPositions[agentCommands.AgentIndex] = newAgentPos;
                }
            }

            string[] finishedCommands = new string[parallelizedCommands.Count];
            int index = 0;
            foreach (var commands in parallelizedCommands)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    if (commands[i] == null)
                    {
                        commands[i] = AgentCommand.NoOp();
                    }
                }

                finishedCommands[index++] = ServerCommunicator.CreateCommand(commands);
            }
            
            return finishedCommands;
        }

        private static bool FindTimeRoute(List<AgentCommand> commands, int commandIndex, Point agentPos, int startTime, int currentEndTime)
        {
            AgentCommand command = commands[commandIndex];

            Point newAgentPos = command.GetNextAgentPos(agentPos);
        }
    }
}
