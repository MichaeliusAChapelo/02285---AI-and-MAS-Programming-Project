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

        public static int Max<T>(this Span<T> array, Func<T, int> sel)
        {
            if (array.Length == 0)
            {
                throw new ArgumentOutOfRangeException("Array can't have a length of 0.");
            }

            int max = int.MinValue;
            for (int i = 0; i < array.Length; i++)
            {
                max = Math.Max(max, sel(array[i]));
            }

            return max;
        }

        public static Point[] DirectionDeltas(this Direction dir)
        {
            return DirectionMovement;
        }

        public static bool IsMoveable(this EntityType entType)
        {
            return (entType & EntityType.MOVEABLE) != 0;
        }

        public static bool IsGoal(this EntityType entType)
        {
            return (entType & EntityType.GOAL) != 0;
        }

        public static bool EntityEquals(this EntityType a, EntityType b)
        {
            if (a == b)
            {
                return true;
            }

            if (a.IsMoveable() && b.IsMoveable() &&
                (a == EntityType.MOVEABLE || b == EntityType.MOVEABLE))
            {
                return true;
            }

            if (a.IsGoal() && b.IsGoal() &&
                (a == EntityType.GOAL || b == EntityType.GOAL))
            {
                return true;
            }

            return false;
        }
    }
}
