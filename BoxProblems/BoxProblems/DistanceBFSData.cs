using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal class DistanceBFSData
    {
        public readonly Direction[] World;
        public readonly Queue<Point> Frontier;
        public readonly int Width;
        public readonly int Height;

        public DistanceBFSData()
        {
        }

        public DistanceBFSData(int width, int height)
        {
            this.World = new Direction[width * height];
            this.Frontier = new Queue<Point>();
            this.Width = width;
            this.Height = height;
        }

        public void Reset()
        {
            Array.Fill(World, Direction.NONE);

            Frontier.Clear();
        }

        public int GetIndexFromPoint(Point pos)
        {
            return pos.X + pos.Y * Width;
        }

        public bool IsPositionFree(Point pos)
        {
            return World[pos.X + pos.Y * Width] == Direction.NONE;
        }
    }
}
