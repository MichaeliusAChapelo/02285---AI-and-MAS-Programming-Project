using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    public static class BoxSwimming
    {
        internal static bool CanLeftHandBoxSwim(Direction d, Point agentPos, State state, Level level)
        {
            bool agentFrontBox = false, agentFrontLeftBox = false, agentLeftBox = false, agentBackLeftBox = false;

            Point back = agentPos + Opposite(d).DirectionDelta();
            Point front = agentPos + d.DirectionDelta();
            Point left = agentPos + CounterClockwise(d).DirectionDelta();
            Point backLeft = left + Opposite(d).DirectionDelta();
            Point frontLeft = left + d.DirectionDelta();

            foreach (Entity box in state.GetBoxes(level))
                if (box.Pos == agentPos) throw new Exception("Agent is on a box???");
                else if (box.Pos == back) return false;  // Space behind needs be free.
                else if (box.Pos == front) agentFrontBox = true;
                else if (box.Pos == left) agentLeftBox = true;
                else if (box.Pos == frontLeft) agentFrontLeftBox = true;
                else if (box.Pos == backLeft) agentBackLeftBox = true;
                else if (agentFrontBox && agentFrontLeftBox && agentLeftBox && agentBackLeftBox) break;

            return agentFrontBox && agentFrontLeftBox && agentLeftBox && agentBackLeftBox;
        }

        internal static bool CanRightHandBoxSwim(Direction d, Point agentPos, State state, Level level)
        {
            bool agentFrontBox = false, agentFrontRightBox = false, agentRightBox = false, agentBackRightBox = false;

            Point back = agentPos + Opposite(d).DirectionDelta();
            Point front = agentPos + d.DirectionDelta();
            Point Right = agentPos + Clockwise(d).DirectionDelta();
            Point backRight = Right + Opposite(d).DirectionDelta();
            Point frontRight = Right + d.DirectionDelta();

            foreach (Entity box in state.GetBoxes(level))
                if (box.Pos == agentPos) throw new Exception("Agent is on a box???");
                else if (box.Pos == back) return false;  // Space behind needs be free.
                else if (box.Pos == front) agentFrontBox = true;
                else if (box.Pos == Right) agentRightBox = true;
                else if (box.Pos == frontRight) agentFrontRightBox = true;
                else if (box.Pos == backRight) agentBackRightBox = true;
                else if (agentFrontBox && agentFrontRightBox && agentRightBox && agentBackRightBox) break;

            return agentFrontBox && agentFrontRightBox && agentRightBox && agentBackRightBox;
        }

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
            return entityCount / spaceCount;
        }




    }
}
