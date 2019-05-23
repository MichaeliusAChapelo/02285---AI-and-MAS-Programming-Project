using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal readonly struct Point
    {
        public readonly int X;
        public readonly int Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Point a, Point b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point p)
            {
                return p == this;
            }
            return false;
        }

        public static int ManhattenDistance(Point a, Point b)
        {
            int x = a.X - b.X;
            int y = a.Y - b.Y;
            if (x < 0) x *= -1;
            if (y < 0) y *= -1;
            return x + y;
        }

        public static int ManhattenDistance(int x, int y, Point b)
        {
            int dx = x - b.X;
            int dy = y - b.Y;
            if (dx < 0) dx *= -1;
            if (dy < 0) dy *= -1;
            return dx + dy;
        }

        public override string ToString()
        {
            return $"[{X}, {Y}]";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
