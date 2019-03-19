using System;
using System.Drawing;

namespace BoxProblems
{
    internal readonly struct Goal
    {
        public readonly Point Pos;
        public readonly int Type;
    }

    internal readonly struct Entity
    {
        public readonly Point Pos;
        public readonly int Color;
        public readonly int Type;
    }

    internal class State
    {
        State Parent;
        Entity[] entities;
        //Command CMD;
        int G;
    }

    internal class Level
    {
        public readonly byte[,] Walls;
        public readonly Goal[] Goals;
        public readonly State InitialState;
        public readonly int Width;
        public readonly int Height;
        public readonly int BoxCount;
        public readonly int AgentCount;
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
        }
    }
}
