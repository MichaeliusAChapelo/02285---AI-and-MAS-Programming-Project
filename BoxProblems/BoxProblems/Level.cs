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

            //New format requres colors for all boxes and agents.
            //If no colors have been specified then give them all
            //a red color.
            if (colorLines.Count == 0)
            {
                var entities = levelNoColors.SelectMany(x => x.Where(y => char.IsDigit(y) || (char.IsLetter(y) && char.IsUpper(y)))).ToList();
                colorLines.Add($"red: {string.Join(", ", entities)}");
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
            int colorIndex = Array.IndexOf(lines, "#colors") + 1;
            int initialLevelIndex = Array.IndexOf(lines, "#initial") + 1;
            int goalLevelIndex = Array.IndexOf(lines, "#goal") + 1;

            //Split level into its parts
            Span<string> colorLines = new Span<string>(lines, colorIndex, initialLevelIndex - colorIndex - 1);
            Span<string> initialLevel = new Span<string>(lines, initialLevelIndex, goalLevelIndex - initialLevelIndex - 1);
            Span<string> goalLevel = new Span<string>(lines, goalLevelIndex, lines.Length - goalLevelIndex - 1);

            //Parse the colors
            Dictionary<char, int> entityColors = new Dictionary<char, int>();
            for (int i = 0; i < colorLines.Length; i++)
            {
                string[] entities = colorLines[i].Split(':')[1].Replace(" ", string.Empty).Split(',');
                foreach (var entity in entities)
                {
                    entityColors.Add(entity.First(), i);
                }
            }

            List<Entity> agents = new List<Entity>();
            List<Entity> boxes = new List<Entity>();
            List<Goal> goals = new List<Goal>();

            int width = initialLevel.Max(x => x.Length);
            int height = initialLevel.Length;
            bool[,] walls = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < initialLevel[y].Length; x++)
                {
                    char c = initialLevel[y][x];

                    if (char.IsLetter(goalLevel[y][x]))
                        goals.Add(new Goal(new Point(x, y), (goalLevel[y])[x]));

                    if (c == '+')
                        walls[x, y] = true;

                    else if (c == ' ')
                        continue;

                    else if (char.IsDigit(c))
                        agents.Add(new Entity(new Point(x, y), entityColors[c], c));

                    else if (char.IsLetter(c))
                        boxes.Add(new Entity(new Point(x, y), entityColors[c], c));

                    else throw new Exception($"bruh i ain't findin no suitable charz brah.{Environment.NewLine}Position: [{x}, {y}] Character: {c}");
                }
            }

            agents.Sort(EntityComparar.Comparer);
            boxes.Sort(EntityComparar.Comparer);

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
