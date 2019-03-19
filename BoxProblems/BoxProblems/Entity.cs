using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal readonly struct Entity
    {
        public readonly Point Pos;
        public readonly int Color;
        public readonly char Type;

        public Entity(Point Pos, int Color, char Type)
        {
            this.Pos = Pos;
            this.Color = Color;
            this.Type = Type;
        }
    }

    internal class EntityComparar : IComparer<Entity>
    {
        public static readonly EntityComparar Comparer = new EntityComparar();

        public int Compare(Entity x, Entity y)
        {
            return ((x.GetX() + x.GetY() * 1000) + x.GetColor() * 1000000) - ((y.GetX() + y.GetY() * 1000) + y.GetColor() * 1000000);
        }
    }
}
