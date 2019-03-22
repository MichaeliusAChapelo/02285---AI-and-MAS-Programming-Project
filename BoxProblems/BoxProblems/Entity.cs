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

        public override string ToString()
        {
            return $"[{Pos.X}, {Pos.Y}] Color: {Color}, Type: {Type}";
        }
    }

    internal class EntityComparar : IComparer<Entity>
    {
        public static readonly EntityComparar Comparer = new EntityComparar();

        public int Compare(Entity x, Entity y)
        {
            return ((x.Pos.X + x.Pos.Y * 1000) + x.Color * 1000000) - ((y.Pos.X + y.Pos.Y * 1000) + y.Color * 1000000);
        }
    }
}
