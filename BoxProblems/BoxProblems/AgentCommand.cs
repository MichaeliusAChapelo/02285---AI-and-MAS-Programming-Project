using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal enum CommandType : byte
    {
        MOVE,
        PULL,
        PUSH
    }

    internal readonly struct AgentCommand
    {
        public readonly CommandType CType;
        public readonly Direction AgentDir;
        public readonly Direction BoxDir;

        public AgentCommand(CommandType cType, Direction agentDir, Direction boxDir)
        {
            this.CType = cType;
            this.AgentDir = agentDir;
            this.BoxDir = boxDir;
        }

        public static AgentCommand CreateMove(Direction agentDir)
        {
            return new AgentCommand(CommandType.MOVE, agentDir, Direction.NONE);
        }

        public static AgentCommand CreatePull(Direction agentDir, Direction boxDir)
        {
            return new AgentCommand(CommandType.PULL, agentDir, boxDir);
        }

        public static AgentCommand CreatePush(Direction agentDir, Direction boxDir)
        {
            return new AgentCommand(CommandType.PUSH, agentDir, boxDir);
        }

        public Point GetNextAgentPos(Point agentPos)
        {
            return agentPos + AgentDir.DirectionDelta();
        }

        public Point GetBoxPos(Point agentPos)
        {
            if (CType == CommandType.PULL)
            {
                return agentPos + BoxDir.DirectionDelta();
            }
            else if (CType == CommandType.PUSH)
            {
                return agentPos + AgentDir.DirectionDelta();
            }
            else
            {
                throw new Exception("Move command does not have a box attached to it.");
            }
        }

        public Point GetNextBoxPos(Point boxPos)
        {
            if (CType == CommandType.PULL)
            {
                return boxPos + BoxDir.Opposite().DirectionDelta();
            }
            else if (CType == CommandType.PUSH)
            {
                return boxPos + BoxDir.DirectionDelta();
            }
            else
            {
                throw new Exception("Move command does not have a box attached to it.");
            }
        }

        public override string ToString()
        {
            switch (CType)
            {
                case CommandType.MOVE:
                    return $"Move({AgentDir})";
                case CommandType.PULL:
                    return $"Pull({AgentDir},{BoxDir})";
                case CommandType.PUSH:
                    return $"Push({AgentDir},{BoxDir})";
                default:
                    throw new Exception($"Command type not recognized: {CType}");
            }
        }

        internal static string NoOp() { return "NoOp"; }
    }
}
