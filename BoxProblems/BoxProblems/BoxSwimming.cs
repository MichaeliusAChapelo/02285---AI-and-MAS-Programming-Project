using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems
{
    public class BoxSwimmingSolver
    { // Specially hand-crafted and designed for SALeo and SALeo only!

        private readonly Level Level;
        private Entity Agent;
        //private Entity[] Boxes;
        private readonly Point End;

        public BoxSwimmingSolver(Level level)
        {
            this.Level = level;
            this.Agent = level.GetAgents()[0];
            //this.Boxes = level.GetBoxes().ToArray();
            this.End = new Point(13, 35); // Should be set in goals or something, but whatever.
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

                    //else break;
                    else throw new Exception("How?");

                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else if (BoxSwimming.CounterClockwise(nextDir) == freeSpotDir)
                {
                    if (BoxSwimming.CanLeftHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.SwimLeft(BoxSwimming.Opposite(freeSpotDir)));
                    else
                    {

                    }
                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else if (BoxSwimming.Clockwise(nextDir) == freeSpotDir)
                {
                    if (BoxSwimming.CanRightHandBoxSwim(nextDir, Agent.Pos, Level))
                        solution.AddRange(BoxSwimming.SwimRight(BoxSwimming.Opposite(freeSpotDir)));
                    else
                    {
                        // He's got gotten!

                        solution.Add(AgentCommand.CreateMove(freeSpotDir));
                        //solution.Add(AgentCommand.CreatePull)

                        // Considering that FORWARD right NOW is the opposite of freeSpot
                        /* Move into freeSpot
                         * Pull left-back box into free spot
                         * Push left-box into left-back
                         * Move nextDir
                         * if agent isn't at nextPoint, then L/R BoxSwim in nextDir
                        */

                    }
                    freeSpot = Agent.Pos;
                    Agent = Agent.Move(nextPoint);
                    continue;
                }

                else
                    break;
            }


            return new List<AgentCommands>() { new AgentCommands(solution, 0) };
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

            //bool agentFrontBox = false, agentFrontRightBox = false, agentRightBox = false, agentBackRightBox = false;

            //Point back = agentPos + Opposite(d).DirectionDelta();
            //Point front = agentPos + d.DirectionDelta();
            //Point Right = agentPos + Clockwise(d).DirectionDelta();
            //Point backRight = Right + Opposite(d).DirectionDelta();
            //Point frontRight = Right + d.DirectionDelta();

            //foreach (Entity box in state.GetBoxes(level))
            //    if (box.Pos == agentPos) throw new Exception("Agent is on a box???");
            //    else if (box.Pos == back) return false;  // Space behind needs be free.
            //    else if (box.Pos == front) agentFrontBox = true;
            //    else if (box.Pos == Right) agentRightBox = true;
            //    else if (box.Pos == frontRight) agentFrontRightBox = true;
            //    else if (box.Pos == backRight) agentBackRightBox = true;
            //    else if (agentFrontBox && agentFrontRightBox && agentRightBox && agentBackRightBox) break;

            //return agentFrontBox && agentFrontRightBox && agentRightBox && agentBackRightBox;
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

        #region Garbage
        public static string[] SwimLeft(char c)
        {
            var d = StringToDir(c);

            return new string[3] {
                "Move(" + Opposite(d) + ")",
                "Pull(" + d + "," + CounterClockwise(d)  + ")",
                "Push(" + CounterClockwise(d) + "," + Opposite(d)  + ")",
            };
        }

        public static string[] LeftHandBoxSwimming(char c)
        {
            var d = StringToDir(c);

            return new string[5] {
                "Move(" + Opposite(d) + ")",
                "Pull(" + d + "," + CounterClockwise(d)  + ")",
                "Push(" + CounterClockwise(d) + "," + Opposite(d)  + ")",
                "Pull(" + Clockwise(d) + "," + d  + ")",
                "Push(" + d + "," + CounterClockwise(d)  + ")",
            };
        }

        public static string[] SwimRight(char c)
        {
            var d = StringToDir(c);
            return new string[3] {
                "Move(" + Opposite(d) + ")",
                "Pull(" + d + "," + Clockwise(d)  + ")",
                "Push(" + Clockwise(d) + "," + Opposite(d)  + ")",
            };
        }

        public static string[] RightHandBoxSwimming(char c)
        {
            var d = StringToDir(c);

            return new string[5] {
                "Move(" + Opposite(d) + ")",
                "Pull(" + d + "," + Clockwise(d)  + ")",
                "Push(" + Clockwise(d) + "," + Opposite(d)  + ")",
                "Pull(" + CounterClockwise(d) + "," + d  + ")",
                "Push(" + d + "," + Clockwise(d)  + ")",
            };
        }
        #endregion

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

        internal static Direction StringToDir(char c)
        {
            switch (c)
            {
                case 'N':
                    return Direction.N;

                case 'S':
                    return Direction.S;

                case 'E':
                    return Direction.E;

                case 'W':
                    return Direction.W;
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
