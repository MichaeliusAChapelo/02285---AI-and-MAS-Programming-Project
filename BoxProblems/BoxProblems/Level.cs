using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BoxProblems
{

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


        public static Level ReadLevel(string path)
        {
            List<Entity> agents = new List<Entity>();
            List<Entity> boxes = new List<Entity>();
            List<Goal> goals = new List<Goal>();
            
            // Keep all lines to ensure we only need read the level once.
            string[] lines = File.ReadAllLines(path);

            // Find indexes for levels and goals.
            int initialLevelIndex = 0, goalLevelIndex = 0;
            for (int i = 0; i < lines.Length; ++i)
            {
                if (lines[i] == "#initial")
                    initialLevelIndex = i + 1;
                else if (lines[i] == "#goal")
                {
                    goalLevelIndex = i + 1;
                    break;
                }
            }

            // Define width/height of wall map.
            int width = lines[initialLevelIndex].Length;
            int height = goalLevelIndex - initialLevelIndex - 1;
            bool[,] walls = new bool[width, height];


            // Fix 'em colours (yes, elegant, European, superior and majestic British spelling)

            int[] agentColours = new int[10];
            int[] boxColours = new int[30];
            for (int i = 5; i < initialLevelIndex - 1; ++i)
            {
                string[] contents = lines[i].Split(", ");
                contents[0] = (contents[0]).Substring(contents[0].Length - 1);

                for (int j = 0; j < contents.Length; ++j)
                {
                    if (Regex.IsMatch(contents[j], @"[0-9]"))
                    {
                        int num = int.Parse(contents[j]);
                        agentColours[num] = i - 5; // Index of colour in text file.
                    }

                    else if (Regex.IsMatch(contents[j], @"[A-Z]"))
                    {
                        boxColours[char.Parse(contents[j]) - 'A'] = i - 5; // Index of colour in text file.
                    }
                }
            }
            // Read each line and each character.
            for (int y = 0; y < goalLevelIndex - initialLevelIndex - 1; ++y)
                for (int x = 0; x < lines[initialLevelIndex + y].Length; ++x)
                {
                    char c = (lines[initialLevelIndex + y])[x]; // Initial state character.

                    if (c == '+') // Wall
                        walls[x, y] = true;

                    // Goals
                    else if ((lines[goalLevelIndex + y])[x] != c
                        && Regex.IsMatch((lines[goalLevelIndex + y])[x].ToString(), @"[A-Z]"))
                        goals.Add(new Goal(new Point(x, y), (lines[goalLevelIndex + y])[x]));

                    // Efficiency!
                    else if (c == ' ')
                        continue;

                    // Agent SPOTTED
                    else if (Regex.IsMatch(c.ToString(), @"[0-9]"))
                        agents.Add(new Entity(new Point(x, y), agentColours[c - '0'], c)); // Fix me l8 m8 gr8 1 m8

                    // And another one of them boxes givin' us me problema.
                    else if (Regex.IsMatch(c.ToString(), @"[A-Z]"))
                        boxes.Add(new Entity(new Point(x, y), boxColours[c - 'A'], c)); // Fix me l8

                    else throw new Exception("bruh i ain't findin no suitable charz brah");
                }

            agents.Sort((x, y) => x.Type.CompareTo(y.Type));

            State state = new State(null, agents.Concat(boxes).ToArray(), 0);
            return new Level(walls, goals.ToArray(), state, width, height, agents.Count, boxes.Count);
        }

        public string StateToString(State state)
        {
            char[][] world = GetWallsAsWorld();
            foreach (var goal in Goals)
            {
                int x = goal.Pos.X;
                int y = goal.Pos.Y;
                char type = goal.Type;

                world[y][x] = char.ToLower(type);
            }

            foreach (var agent in state.GetAgents(this))
            {
                int x = agent.Pos.X;
                int y = agent.Pos.Y;
                char type = agent.Type;

                world[y][x] = type;
            }

            foreach (var box in state.GetBoxes(this))
            {
                int x = box.Pos.X;
                int y = box.Pos.Y;
                char type = box.Type;

                world[y][x] = type;
            }

            StringBuilder sBuilder = new StringBuilder();
            foreach (var worldRow in world)
            {
                sBuilder.Append(worldRow);
                sBuilder.Append(Environment.NewLine);
            }

            return sBuilder.ToString();
        }

        public char[][] GetWallsAsWorld()
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

            return world;
        }

        public string WorldToString(char[][] world)
        {
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

}
