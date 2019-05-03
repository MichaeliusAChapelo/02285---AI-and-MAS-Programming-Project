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

        public Entity(int x, int y, int color, char type)
        {
            this.Pos = new Point(x, y);
            this.Color = color;
            this.Type = type;
        }

        public Entity(Point pos, int color, char type)
        {
            this.Pos = pos;
            this.Color = color;
            this.Type = type;
        }

        public Entity Move(Point pos)
        {
            return new Entity(pos, Color, Type);
        }

        public static bool operator==(Entity a, Entity b)
        {
            return a.Pos == b.Pos && a.Color == b.Color && a.Type == b.Type;
        }

        public static bool operator !=(Entity a, Entity b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"[{Pos.X}, {Pos.Y}] Color: {Color}, Type: {Type}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Entity entity)
            {
                return Pos == entity.Pos &&
                       Color == entity.Color &&
                       Type == entity.Type;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Pos, Color, Type);
        }
    }

    internal class EntityComparar : IComparer<Entity>
    {
        public static readonly EntityComparar Comparer = new EntityComparar();

        public int Compare(Entity x, Entity y)
        {
            return x.Type - y.Type;
            //return ((x.Pos.X + x.Pos.Y * 1000) + x.Color * 1000000) - ((y.Pos.X + y.Pos.Y * 1000) + y.Color * 1000000);
        }
    }
}
