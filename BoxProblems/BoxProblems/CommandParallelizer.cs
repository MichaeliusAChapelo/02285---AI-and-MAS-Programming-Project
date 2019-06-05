using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems
{
    public static class CommandParallelizer
    {
        private readonly struct CommandParInfo
        {
            public readonly int StartTime;
            public int MoveTime { get { return EndTime - 1; } }
            public readonly int EndTime;

            public CommandParInfo(int start, int end)
            {
                this.StartTime = start;
                this.EndTime = end;
            }
        }

        public static string[] Parallelize2(List<AgentCommands> allCommands, Span<Entity> agents, Span<Entity> boxes, Level level = null)
        {
            List<string[]> parallelizedCommands = new List<string[]>();
            List<HashSet<Point>> notAllowedAtThesePos = new List<HashSet<Point>>();
            Point[] agentPositions = new Point[agents.Length];
            int[] agentFreeTime = new int[agents.Length];

            for (int i = 0; i < agents.Length; i++)
            {
                agentPositions[i] = agents[i].Pos;
            }

            List<HashSet<Point>> boxPositions = new List<HashSet<Point>>();
            boxPositions.Add(new HashSet<Point>());
            for (int i = 0; i < boxes.Length; i++)
            {
                boxPositions[0].Add(boxes[i].Pos);
            }

            var commandTimingsBuffer = new CommandParInfo[20_000];
            foreach (AgentCommands agentCommands in allCommands)
            {
                if (agentCommands.Commands.Count == 0)
                {
                    continue;
                }

                int startStep = agentFreeTime[agentCommands.AgentIndex];
                Span<CommandParInfo> commandTimings = commandTimingsBuffer.AsSpan(0, agentCommands.Commands.Count);


                var mergeResult = MergeCommands(agentCommands, 0, commandTimings, boxPositions, notAllowedAtThesePos, agentPositions[agentCommands.AgentIndex], agentFreeTime[agentCommands.AgentIndex], false);
                if (!mergeResult.valid)
                {
                    throw new Exception("Failed to merge command even though it should always be possible.");
                }

                for (int i = parallelizedCommands.Count; i <= commandTimings[commandTimings.Length - 1].MoveTime; i++)
                {
                    parallelizedCommands.Add(new string[agents.Length]);
                }

                MergeddCommands(agentCommands, commandTimings, parallelizedCommands, agentPositions, agentFreeTime, notAllowedAtThesePos, agentPositions[agentCommands.AgentIndex]);
                //foreach (var timeStep in notAllowedAtThesePos.Skip(startStep))
                //{
                //    LevelVisualizer.PrintPath(level, level.InitialState, timeStep.ToList());
                //    Console.ReadLine();
                //}
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

        private static (bool valid, int invalidTime) MergeCommands(AgentCommands agentCommands, int commandIndex, Span<CommandParInfo> commandTimings, List<HashSet<Point>> boxPositions, List<HashSet<Point>> notAllowedAtThesePos, Point agentPos, int agentTime, bool startedMovingBox)
        {
            int startTime = agentTime;
            while (true)
            {
                if (agentTime > 20_000)
                {
                    throw new Exception("command parallelizer infinite loop.");
                }

                int time = agentTime;
                AgentCommand command = agentCommands.Commands[commandIndex];

                Point newAgentPos = command.GetNextAgentPos(agentPos);
                int newTime = GetNextAvailableTime(notAllowedAtThesePos, time, newAgentPos);

                Point? boxPos = null;
                Point? nextBoxPos = null;
                if (command.CType != CommandType.MOVE)
                {
                    boxPos = command.GetBoxPos(agentPos);
                    nextBoxPos = command.GetNextBoxPos(boxPos.Value);

                    newTime = Math.Max(newTime, GetNextAvailableTime(notAllowedAtThesePos, time, boxPos.Value));
                    newTime = Math.Max(newTime, GetNextAvailableTime(notAllowedAtThesePos, time, nextBoxPos.Value));

                    //If this is the first push/pull then make sure that it's moving the correct box.
                    //This is done by assuming that the box is correct if no other things interact with this 
                    //position in the future as that means no other boxes will be moved to this location.
                    if (!startedMovingBox)
                    {
                        startedMovingBox = true;
                        bool prevWasBox = boxPositions[newTime - 1].Contains(boxPos.Value);
                        for (int i = newTime; i < boxPositions.Count; i++)
                        {
                            if (!prevWasBox && boxPositions[i].Contains(boxPos.Value))
                            {
                                return (false, i);
                            }
                            prevWasBox = boxPositions[i].Contains(boxPos.Value);
                        }


                        //var isMostRecentBoxMovement = GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(newTime, boxPositions.Count - 1, boxPositions, agentPos, newAgentPos, boxPos, nextBoxPos);
                        //if (!isMostRecentBoxMovement.valid)
                        //{
                        //    return isMostRecentBoxMovement;
                        //}
                    }
                }
                else
                {
                    bool reset = false;
                    int nextBestTime = -1;
                    for (int i = time; i < newTime; i++)
                    {
                        if (boxPositions[i].Contains(newAgentPos))
                        {
                            nextBestTime = GetNextAvailableTime(boxPositions, i, newAgentPos);
                            if (nextBestTime == boxPositions.Count)
                            {
                                return (false, nextBestTime);
                            }
                            reset = true;
                        }
                    }
                    if (reset)
                    {
                        agentTime = nextBestTime;
                        continue;
                    }
                    //var isMostRecentBoxMovement = GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(time, newTime, boxPositions, agentPos, newAgentPos, boxPos, nextBoxPos);
                    //if (!isMostRecentBoxMovement.valid)
                    //{
                    //    return isMostRecentBoxMovement;
                    //}
                }

                //Check that the agent(and box if any) can stay at its current positions until it has to move
                var canStayHere = GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(time, newTime, notAllowedAtThesePos, agentPos, newAgentPos, boxPos, nextBoxPos);
                if (!canStayHere.valid)
                {
                    return canStayHere;
                }

                int maxTime = newTime;
                if (commandIndex < agentCommands.Commands.Count - 1)
                {
                    for (int i = boxPositions.Count; i <= newTime; i++)
                    {
                        boxPositions.Add(new HashSet<Point>(boxPositions[time]));
                    }
                    if (boxPos != null)
                    {
                        boxPositions[newTime].Remove(boxPos.Value);
                        boxPositions[newTime].Add(nextBoxPos.Value);
                    }
                    var mergeResult = MergeCommands(agentCommands, commandIndex + 1, commandTimings, boxPositions, notAllowedAtThesePos, newAgentPos, newTime, startedMovingBox);
                    if (boxPos != null)
                    {
                        boxPositions[newTime].Remove(nextBoxPos.Value);
                        boxPositions[newTime].Add(boxPos.Value);
                    }
                    if (!mergeResult.valid)
                    {
                        var canStillStayHere = GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(newTime, mergeResult.invalidTime, notAllowedAtThesePos, agentPos, newAgentPos, boxPos, nextBoxPos);
                        if (!canStillStayHere.valid)
                        {
                            return canStillStayHere;
                        }

                        agentTime = newTime;
                        continue;
                    }
                    maxTime = Math.Max(maxTime, mergeResult.invalidTime);
                }

                if (commandIndex == agentCommands.Commands.Count - 1)
                {
                    var canStayAtLastPos = GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(newTime, notAllowedAtThesePos.Count - 1, notAllowedAtThesePos, newAgentPos, newAgentPos, nextBoxPos, nextBoxPos);
                    if (!canStayAtLastPos.valid)
                    {
                        return canStayAtLastPos;
                    }
                }

                for (int i = boxPositions.Count; i <= newTime; i++)
                {
                    boxPositions.Add(new HashSet<Point>(boxPositions[time]));
                }
                if (boxPos != null)
                {
                    for (int i = newTime; i < boxPositions.Count; i++)
                    {
                        if (boxPositions[i].Contains(boxPos.Value))
                        {
                            boxPositions[i].Remove(boxPos.Value);
                        }
                        else
                        {
                            break;
                        }
                    }
                    boxPositions[newTime].Add(nextBoxPos.Value);
                }
                commandTimings[commandIndex] = new CommandParInfo(startTime, newTime);
                return (true, maxTime);
            }
        }

        private static void MergeddCommands(AgentCommands agentCommands, Span<CommandParInfo> commandTimings, List<string[]> parallelizedCommands, Point[] agentPositions, int[] agentFreeTime, List<HashSet<Point>> notAllowedAtThesePos, Point agentPos)
        {
            for (int i = 0; i < agentCommands.Commands.Count; i++)
            {
                AgentCommand command = agentCommands.Commands[i];

                Point newAgentPos = command.GetNextAgentPos(agentPos);

                Point? boxPos = null;
                Point? nextBoxPos = null;
                if (command.CType != CommandType.MOVE)
                {
                    boxPos = command.GetBoxPos(agentPos);
                    nextBoxPos = command.GetNextBoxPos(boxPos.Value);
                }
                var timings = commandTimings[i];
                parallelizedCommands[timings.MoveTime][agentCommands.AgentIndex] = command.ToString();

                if (command.CType != CommandType.MOVE)
                {
                    SetNotAllowedAtPosInTimePeriod(notAllowedAtThesePos, timings.StartTime, timings.MoveTime, boxPos.Value);
                    if (notAllowedAtThesePos.Count == timings.EndTime)
                    {
                        notAllowedAtThesePos.Add(new HashSet<Point>());
                    }
                    notAllowedAtThesePos[timings.EndTime].Add(nextBoxPos.Value);
                    //notAllowedAtThesePos[newTime - 1].Add(nextBoxPos.Value);
                }

                SetNotAllowedAtPosInTimePeriod(notAllowedAtThesePos, timings.StartTime, timings.MoveTime, agentPos);
                if (notAllowedAtThesePos.Count == timings.EndTime)
                {
                    notAllowedAtThesePos.Add(new HashSet<Point>());
                }
                notAllowedAtThesePos[timings.EndTime].Add(newAgentPos);
                //notAllowedAtThesePos[newTime - 1].Add(newAgentPos);
                if (i == agentCommands.Commands.Count - 1)
                {
                    notAllowedAtThesePos[timings.EndTime].Remove(newAgentPos);
                    if (nextBoxPos.HasValue)
                    {
                        notAllowedAtThesePos[timings.EndTime].Remove(nextBoxPos.Value);
                    }
                    agentPositions[agentCommands.AgentIndex] = newAgentPos;
                    agentFreeTime[agentCommands.AgentIndex] = timings.EndTime;
                }

                agentPos = newAgentPos;
            }
        }

        private static (bool valid, int invalidTime) GetIfCanStayAtPosAndIfNotThenAtWhatTimeItCanStayAtThePos(int start, int end, List<HashSet<Point>> notAllowedAtThesePos, Point agentPos, Point nextAgentPos, Point? boxPos, Point? nextBoxPos)
        {
            for (int i = start; i <= end; i++)
            {
                if (notAllowedAtThesePos.Count > i && (notAllowedAtThesePos[i].Contains(agentPos) || (boxPos != null && notAllowedAtThesePos[i].Contains(boxPos.Value))))
                {
                    return (false, Math.Max(GetNextAvailableTime(notAllowedAtThesePos, i, agentPos),
                        Math.Max(GetNextAvailableTime(notAllowedAtThesePos, i, nextAgentPos),
                        Math.Max(boxPos.HasValue ? GetNextAvailableTime(notAllowedAtThesePos, i, boxPos.Value) : -1,
                        nextBoxPos.HasValue ? GetNextAvailableTime(notAllowedAtThesePos, i, nextBoxPos.Value) : -1))));
                }
            }

            return (true, -1);
        }

        private static int GetNextAvailableTime(List<HashSet<Point>> notAllowedAtThesePos, int time, Point pos)
        {
            int newTime = time + 1;
            bool prevWasAvailable = notAllowedAtThesePos.Count <= time || !notAllowedAtThesePos[time].Contains(pos);
            while (notAllowedAtThesePos.Count > newTime)
            {
                bool isThisAvailable = !notAllowedAtThesePos[newTime].Contains(pos);
                if (prevWasAvailable)
                {
                    return newTime;
                }
                prevWasAvailable = isThisAvailable;
                newTime++;
            }

            return newTime;
        }

        private static void SetNotAllowedAtPosInTimePeriod(List<HashSet<Point>> notAllowedAtThesePos, int start, int end, Point notAllowedPos)
        {
            for (int i = notAllowedAtThesePos.Count; i <= end; i++)
            {
                if (notAllowedAtThesePos.Count == i)
                {
                    notAllowedAtThesePos.Add(new HashSet<Point>());
                }
            }

            for (int i = start; i <= end; i++)
            {
                notAllowedAtThesePos[i].Add(notAllowedPos);
            }
        }

        public static string[] Parallelize(List<AgentCommands> allCommands, Level level)
        {
            int[,] world = new int[level.Width, level.Height];
            Point[] agentPositions = new Point[level.AgentCount];
            List<string[]> parallelizedCommands = new List<string[]>();

            Span<Entity> initialAgents = level.GetAgents();
            for (int i = 0; i < initialAgents.Length; i++)
            {
                agentPositions[i] = initialAgents[i].Pos;
            }

            foreach (AgentCommands agentCommands in allCommands)
            {
                foreach (AgentCommand command in agentCommands.Commands)
                {
                    Point agentPos = agentPositions[agentCommands.AgentIndex];
                    int time = world[agentPos.X, agentPos.Y];

                    Point newAgentPos = command.GetNextAgentPos(agentPos);
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
    }
}
