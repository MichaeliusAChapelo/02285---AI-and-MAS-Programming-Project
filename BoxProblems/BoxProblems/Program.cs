using System;
using System.Text;

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

    internal class Level
    {
        public readonly bool[,] Walls;
        public readonly Goal[] Goals;
        public readonly State InitialState;
        public readonly int Width;
        public readonly int Height;
        public readonly int AgentCount;
        public readonly int BoxCount;

        public Level(bool[,] walls, Goal[] goals, State initial, int width, int height, int agentCount, int boxCount)
        {
            this.Walls = walls;
            this.Goals = goals;
            this.InitialState = initial;
            this.Width = width;
            this.Height = height;
            this.AgentCount = agentCount;
            this.BoxCount = boxCount;
        }

        public Span<Entity> GetAgents()
        {
            return InitialState.GetAgents(this);
        }

        public Span<Entity> GetBoxes()
        {
            return InitialState.GetBoxes(this);
        }

        public int PosToIndex(Point pos)
        {
            return pos.X + pos.Y * Width;
        }

        public string StateToString(State state)
        {
            char[][] world = new char[Height][];
            for (int i = 0; i < Height; i++)
            {
                world[i] = new char[Width];
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Walls[x, y])
                    {
                        world[y][x] = '#';
                    }
                    else
                    {
                        world[y][x] = ' ';
                    }
                }
            }

            foreach (var goal in Goals)
            {
                int x = goal.Pos.X;
                int y = goal.Pos.Y;
                int type = goal.Type;

                world[y][x] = (char)('a' + type);
            }

            foreach (var agent in state.GetAgents(this))
            {
                int x = agent.Pos.X;
                int y = agent.Pos.Y;
                int type = agent.Type;

                world[y][x] = (char)('0' + type);
            }

            foreach (var box in state.GetBoxes(this))
            {
                int x = box.Pos.X;
                int y = box.Pos.Y;
                int type = box.Type;

                world[y][x] = (char)('A' + type);
            }

            StringBuilder sBuilder = new StringBuilder();
            foreach (var worldRow in world)
            {
                sBuilder.Append(worldRow);
                sBuilder.Append(Environment.NewLine);
            }

            return sBuilder.ToString();
        }

        public override string ToString()
        {
            return StateToString(InitialState);
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
        }
    }
}
