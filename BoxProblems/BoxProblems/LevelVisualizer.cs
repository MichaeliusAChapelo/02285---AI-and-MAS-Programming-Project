using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems
{
    internal static class LevelVisualizer
    {
        public static void PrintSpaceDistances(Level level, State state, int[,] spaceDistances)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            for (int y = 0; y < stateString.Length; y++)
            {
                for (int x = 0; x < stateString[y].Length; x++)
                {
                    if (spaceDistances[x, y] != 0 && spaceDistances[x, y] != int.MaxValue)
                    {
                        Console.Write(string.Format("{0,6}", spaceDistances[x, y]));
                    }
                    else
                    {
                        Console.Write(string.Format("{0,6}", stateString[y][x]));
                    }
                }
                Console.WriteLine();
            }
        }

        public static void PrintSpaceDistances(Level level, State state, short[,] spaceDistances)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            for (int y = 0; y < stateString.Length; y++)
            {
                for (int x = 0; x < stateString[y].Length; x++)
                {
                    if (spaceDistances[x, y] != 0)
                    {
                        Console.Write(string.Format("{0,6}", spaceDistances[x, y]));
                    }
                    else
                    {
                        Console.Write(string.Format("{0,6}", stateString[y][x]));
                    }
                }
                Console.WriteLine();
            }
        }

        public static void PrintPath(Level level, State state, List<Point> path)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            WriteLevelToConsole(level, state, pos =>
            {
                if (path.Contains(pos))
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            });
        }

        public static void PrintGraphGroups(Level level, State state, List<List<INode>> groups)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            WriteLevelToConsole(level, state, pos =>
            {
                bool hasGroup = false;
                foreach (var group in groups)
                {
                    foreach (var node in group)
                    {
                        if (node is FreeSpaceNode freeNode && freeNode.Value.FreeSpaces.Contains(pos))
                        {
                            hasGroup = true;
                            goto done;
                        }
                        else if (node is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == pos)
                        {
                            hasGroup = true;
                            goto done;
                        }
                    }
                }
                done:
                if (hasGroup)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            });
        }

        public static void PrintFreeSpace(Level level, State state, Dictionary<Point, int> freeSpaces)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            WriteLevelToConsole(level, state, pos =>
            {
                if (level.Goals.Any(x => x.Ent.Pos == pos) && state.GetBoxes(level).ToArray().Any(x => x.Pos == pos))
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                }
                if (freeSpaces.ContainsKey(pos))
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            });
        }

        public static void PrintLatestStateDiff(Level level, List<BoxConflictGraph> graphs)
        {
            PrintLatestStateDiff(level, graphs, graphs.Count - 1);
        }

        public static void PrintLatestStateDiff(Level level, List<BoxConflictGraph> graphs, int index)
        {
            if (index == -1)
            {
                Console.WriteLine(level.ToString());
            }
            else if (index == 0)
            {
                Console.WriteLine(level.StateToString(graphs[index].CreatedFromThisState));
            }
            else
            {
                State last = graphs[index].CreatedFromThisState;
                State sLast = graphs[index - 1].CreatedFromThisState;

                string[] lastStateStrings = level.StateToString(last).Split(Environment.NewLine);
                string[] sLastStateStrings = level.StateToString(sLast).Split(Environment.NewLine);

                WriteLevelToConsole(level, last, pos =>
                {
                    if (lastStateStrings[pos.Y][pos.X] != sLastStateStrings[pos.Y][pos.X])
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                });
            }

            Console.ReadLine();
        }

        private static void WriteLevelToConsole(Level level, State state, Action<Point> setColor)
        {
            string[] stateString = level.StateToString(state).Split(Environment.NewLine);

            for (int y = 0; y < stateString.Length; y++)
            {
                for (int x = 0; x < stateString[y].Length; x++)
                {
                    setColor(new Point(x, y));
                    Console.Write(stateString[y][x]);
                }
                Console.WriteLine();
            }
        }
    }
}
