using System;
using System.Collections.Generic;
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

        public static Level ReadOldFormatLevel(string[] lines, string levelName)
        {
            List<string> colorLines = new List<string>();
            List<string> levelNoColors = new List<string>();
            bool isColor = true;
            foreach (string line in lines)
            {
                if (isColor && char.IsLetter(line.First()))
                {
                    colorLines.Add(line);
                }
                else
                {
                    isColor = false;
                    levelNoColors.Add(line);
                }
            }

            List<string> levelWithoutGoals = new List<string>();
            foreach (string line in levelNoColors)
            {
                string correctedLine = "";
                foreach (char c in line)
                {
                    if (char.IsLetter(c) && char.IsLower(c))
                    {
                        correctedLine += ' ';
                    }
                    else
                    {
                        correctedLine += c;
                    }
                }
                levelWithoutGoals.Add(correctedLine);
            }
            List<string> levelWithOnlyGoals = new List<string>();
            foreach (string line in levelNoColors)
            {
                string correctedLine = "";
                foreach (char c in line)
                {
                    if (char.IsDigit(c) || (char.IsLetter(c) && char.IsUpper(c)))
                    {
                        correctedLine += ' ';
                    }
                    else
                    {
                        correctedLine += char.ToUpper(c);
                    }
                }
                levelWithOnlyGoals.Add(correctedLine);
            }

            List<string> newFormat = new List<string>()
            {
                "#domain",
                "hospital",
                "#levelname",
                levelName,
                "#colors",
            };
            newFormat.AddRange(colorLines);
            newFormat.Add("#initial");
            newFormat.AddRange(levelWithoutGoals);
            newFormat.Add("#goal");
            newFormat.AddRange(levelWithOnlyGoals);
            newFormat.Add("#end");

            return ReadLevel(newFormat.ToArray());
        }

        public static Level ReadLevel(string[] lines)
        {
            List<Entity> agents = new List<Entity>();
            List<Entity> boxes = new List<Entity>();
            List<Goal> goals = new List<Goal>();

            //Trim level lines because some levels
            // starts or ends with spaces which is invalid
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }

            int initialLevelIndex = Array.IndexOf(lines, "#initial") + 1;
            int goalLevelIndex = Array.IndexOf(lines, "#goal") + 1;

            Span<string> initialLevel = new Span<string>(lines, 0, goalLevelIndex - initialLevelIndex - 1);
            Span<string> goalLevel = new Span<string>(lines, goalLevelIndex, lines.Length - goalLevelIndex - 1);

            int width = initialLevel.Length;
            int height = initialLevel.Max(x => x.Length);
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
