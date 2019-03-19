using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal static class Extensions
    {
        private static readonly Direction[] OppositeDirections = new Direction[] { Direction.S, Direction.E, Direction.W, Direction.N };
        private static readonly Point[] DirectionMovement = new Point[] { new Point(0, -1), new Point(-1, 0), new Point(1, 0), new Point(0, 1) };

        public static Direction Opposite(this Direction dir)
        {
            return OppositeDirections[(int)dir];
        }

        public static Point DirectionDelta(this Direction dir)
        {
            return DirectionMovement[(int)dir];
        }
    }
}
