using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BoxProblems
{
    internal readonly struct Goal
    {
        public readonly Point Pos;
        public readonly char Type;

        public Goal(Point Pos, char Type)
        {
            this.Pos = Pos;
            this.Type = Type;
        }
    }

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

    internal enum Direction : byte
    {
        N = 0,
        W = 1,
        E = 2,
        S = 3,
        NONE = 4,
    }

    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                string strategy = "-astar";
                string level = "MAExample.lvl";
                System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {level} -c 'dotnet BoxProblems.dll {strategy}' -g 150 -t 300");
            }

            Level leavel = Level.ReadLevel("MAExample.lvl");
            Console.WriteLine(leavel);
            Console.WriteLine("Hello World!");
            Console.Read();
        }
    }
}
