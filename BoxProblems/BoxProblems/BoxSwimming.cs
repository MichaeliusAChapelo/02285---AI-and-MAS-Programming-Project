using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    public static class BoxSwimming
    {
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

        private static Direction Clockwise(Direction d)
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

        private static Direction CounterClockwise(Direction d)
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

        private static Direction Opposite(Direction d)
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

        private static Direction StringToDir(char c)
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

    }
}
