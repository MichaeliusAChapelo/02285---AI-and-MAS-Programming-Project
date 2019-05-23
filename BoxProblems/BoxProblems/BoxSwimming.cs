using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems
{
    internal class BoxSwimmingSolver
    { // Specially hand-crafted and designed for SALeo and SALeo only!

        public static List<AgentCommands> EndSolution { get; private set; }
        public static Level Level { get; private set; }
        private Entity Agent;
        private readonly Point End;

        public BoxSwimmingSolver(Level level, Point end)
        {
            Level = level;
            this.Agent = level.GetAgents()[0];
            this.End = end;
            this.End = new Point(48, 19); // Should be set in goals or something, but whatever.
        }

        // GOD FUNCTION TROIS
        public List<AgentCommands> Solve()
        {
            List<AgentCommand> solution = new List<AgentCommand>();

            State currentState = Level.InitialState;
            foreach (Entity e in currentState.Entities)
                if (e.Color != Agent.Color)
                    Level.AddPermanentWalll(e.Pos);

            Point[] meisterPath = Precomputer.GetPath(Level, Agent.Pos, End, false);
            Point freeSpot = meisterPath[1];

            foreach (Point nextPoint in meisterPath)
            {
                if (nextPoint == meisterPath.First()) continue;

                Direction nextDir = LessNaiveSolver.PointsToDirection(Agent.Pos, nextPoint);
                Direction freeSpotDir = LessNaiveSolver.PointsToDirection(Agent.Pos, freeSpot);

                if (nextDir == freeSpotDir)
                {
                    solution.Add(AgentCommand.CreateMove(nextDir));
                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else if (BoxSwimming.Opposite(nextDir) == freeSpotDir)
                {
                    if (BoxSwimming.CanLeftHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.LeftHandBoxSwimming(nextDir));

                    else if (BoxSwimming.CanRightHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.RightHandBoxSwimming(nextDir));

                    else throw new Exception("Box swimming operations failed.");

                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else if (BoxSwimming.CounterClockwise(nextDir) == freeSpotDir)
                {
                    if (BoxSwimming.CanLeftHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.SwimLeft(BoxSwimming.Opposite(freeSpotDir)));
                    else throw new Exception("Box swimming operations failed.");
                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else if (BoxSwimming.Clockwise(nextDir) == freeSpotDir)
                {
                    if (BoxSwimming.CanRightHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.SwimRight(BoxSwimming.Opposite(freeSpotDir)));
                    else throw new Exception("Box swimming operations failed.");
                    //{
                    // He's got gotten!

                    //solution.Add(AgentCommand.CreateMove(freeSpotDir));
                    //solution.Add(AgentCommand.CreatePull)

                    // Considering that FORWARD right NOW is the opposite of freeSpot
                    /* Move into freeSpot
                     * Pull left-back box into free spot
                     * Push left-box into left-back
                     * Move nextDir
                     * if agent isn't at nextPoint, then L/R BoxSwim in nextDir
                    */

                    //}
                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else
                    break;
            }
            EndSolution = new List<AgentCommands>() { new AgentCommands(solution, 0) };
            return EndSolution;
        }

    }


    public static class BoxSwimming
    {
        internal static bool CanLeftHandBoxSwim(Direction d, Point agentPos, Level level)
        {
            Point left = agentPos + CounterClockwise(d).DirectionDelta();
            Point backLeft = left + Opposite(d).DirectionDelta();
            Point frontLeft = left + d.DirectionDelta();
            return !level.IsWall(left) && !level.IsWall(backLeft) && !level.IsWall(frontLeft);
        }

        internal static bool CanRightHandBoxSwim(Direction d, Point agentPos, Level level)
        {
            Point right = agentPos + Clockwise(d).DirectionDelta();
            Point backRight = right + Opposite(d).DirectionDelta();
            Point frontRight = right + d.DirectionDelta();
            return !level.IsWall(right) && !level.IsWall(backRight) && !level.IsWall(frontRight);
        }

        internal static List<AgentCommand> LeftHandBoxSwimming(Direction d)
        {
            return new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Opposite(d)),
                AgentCommand.CreatePull(d, CounterClockwise(d)),
                AgentCommand.CreatePush(CounterClockwise(d), Opposite(d)),
                AgentCommand.CreatePull(Clockwise(d), d),
                AgentCommand.CreatePush(d, CounterClockwise(d))
            };
        }

        internal static List<AgentCommand> RightHandBoxSwimming(Direction d)
        {
            return new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Opposite(d)),
                AgentCommand.CreatePull(d, Clockwise(d)),
                AgentCommand.CreatePush(Clockwise(d), Opposite(d)),
                AgentCommand.CreatePull(CounterClockwise(d), d),
                AgentCommand.CreatePush(d, Clockwise(d))
            };
        }

        internal static List<AgentCommand> SwimLeft(Direction d)
        {
            return new List<AgentCommand>(LeftHandBoxSwimming(d).Take(3));
        }

        internal static List<AgentCommand> SwimRight(Direction d)
        {
            return new List<AgentCommand>(RightHandBoxSwimming(d).Take(3));
        }

        internal static Direction Clockwise(Direction d)
        {
            switch (d)
            {
                case Direction.N:
                    return Direction.E;

                case Direction.E:
                    return Direction.S;

                case Direction.S:
                    return Direction.W;

                case Direction.W:
                    return Direction.N;
            }
            return Direction.NONE;
        }

        internal static Direction CounterClockwise(Direction d)
        {
            switch (d)
            {
                case Direction.W:
                    return Direction.S;

                case Direction.S:
                    return Direction.E;

                case Direction.E:
                    return Direction.N;

                case Direction.N:
                    return Direction.W;
            }
            return Direction.NONE;
        }

        internal static Direction Opposite(Direction d)
        {
            switch (d)
            {
                case Direction.W:
                    return Direction.E;

                case Direction.S:
                    return Direction.N;

                case Direction.E:
                    return Direction.W;

                case Direction.N:
                    return Direction.S;
            }
            return Direction.NONE;
        }

        public static float MeasureBoxDensity(Level level, bool includeAgents = true)
        {
            return MeasureBoxDensity(level, 0, level.Width, 0, level.Height, includeAgents);
        }

        public static float MeasureBoxDensity(Level level, int x1, int x2, int y1, int y2, bool includeAgents = true)
        {
            int spaceCount = 0;
            for (int x = x1; x < x2; x++)
                for (int y = y1; y < y2; y++)
                    if (!level.Walls[x, y])
                        spaceCount++;
            int entityCount = level.GetBoxes().Length;
            if (includeAgents)
                entityCount += level.GetAgents().Length;
            return entityCount / (float)spaceCount;
        }

    }
}
