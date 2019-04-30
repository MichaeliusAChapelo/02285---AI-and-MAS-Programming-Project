using BoxProblems.Solver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoxProblems
{
    public class ServerCommunicator
    {
        const string strategy = "-astar";
        public static bool SkipConsoleRead = false;

        public void StartServer(string levelPath)
        {
            System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {levelPath} -c 'dotnet BoxRunner.dll {strategy}' -g 150 -t 300; Read-Host");
        }

        public void NonAsyncSolve(List<HighlevelLevelSolution> levelSolutions)
        {
            foreach (HighlevelLevelSolution list in levelSolutions)
            {
                var solver = new LessNaiveSolver(list.Level, list.SolutionMovesParts);
                solver.Solve(); // A most convenient function.
            }
        }

        public void AsyncSolve(string levelPath)
        {
            Level level = Level.ReadLevel(File.ReadAllLines(levelPath));
            List<Level> levels = LevelSplitter.SplitLevel(level);
            NaiveSolver.totalAgentCount = level.AgentCount;

            // This is the most disgusting data structure I've ever had the honour of writing.
            var allResults = new ConcurrentBag<List<string[]>>();

            Parallel.ForEach(levels, (currentLevel) =>
            {
                var solver = new NaiveSolver(currentLevel);
                allResults.Add(solver.AsyncSolve());
            });

            AssembleCommands(level.AgentCount, allResults.ToList());
        }

        // Iterates over each solved level, picks out first command, assembles those commands and sends to server. Repeat until fully solved.
        public void AssembleCommands(int agentCount, List<List<string[]>> results)
        {
            var commands = new string[agentCount];
            while (results.Count != 0)
            {
                for (int i = 0; i < commands.Length; ++i) commands[i] = NoOp(); // Default
                for (int i = 0; i < results.Count; ++i)
                {
                    var result = results[i];
                    if (result.Count == 0)
                    {
                        results.Remove(result);
                        i--;
                    }
                    else
                    {
                        for (int j = 0; j < agentCount; ++j)
                            if (result[0][j] != null)
                                commands[j] = result[0][j];
                        result.RemoveAt(0);
                    }
                }
                if (results.Count != 0)
                    Command(commands);
            }
        }

        public static void PrintMap()
        {
            Console.Error.WriteLine("C# Client initialized.");
            Console.WriteLine(""); // Input group name.

            string line;
            while ((line = Console.ReadLine()) != "#end")
            //    Console.Error.WriteLine(line); // Print map input.
            //Console.Error.WriteLine(line + "\n End of map file. \n");
        }

        public void ExampleCommands()
        {
            for (int i = 0; i < 7; ++i)
                Command(new string[] { Move(Direction.W), Move(Direction.E) }); // Accepts string arrays, functions with enum
            for (int i = 0; i < 2; ++i)
                Command(new List<string> { Move('S'), Move('N') }); // Accepts string lists, functions with chars
            for (int i = 0; i < 7; ++i)
                Command("Move(E);Move(W)"); // Accepts direct string commands.

            Command("NoOp;" + NoOp());
        }

        public static string Command(string command)
        {
            Console.WriteLine(command);
            if (SkipConsoleRead) return string.Empty;
            string response = Console.ReadLine();

            Console.Error.WriteLine("COMMAND: " + command + "\nRESPONSE: " + response);
            return response;
        }

        internal static string Command(string[] commands) { return Command(String.Join(';', commands)); }
        internal static string Command(List<string> commands) { return Command(String.Join(';', commands)); }

        internal static string NoOp() { return "NoOp"; }
        internal static string Move(Direction agentDirection) { return "Move(" + agentDirection.ToString() + ")"; }
        internal static string Push(Direction agentDirection, Direction boxDirection) { return "Push(" + agentDirection.ToString() + "," + boxDirection.ToString() + ")"; }
        internal static string Pull(Direction agentDirection, Direction boxDirection) { return "Pull(" + agentDirection.ToString() + "," + boxDirection.ToString() + ")"; }

        internal static string Move(char agentDirection) { return "Move(" + agentDirection + ")"; }
        internal static string Push(char agentDirection, char boxDirection) { return "Push(" + agentDirection + "," + boxDirection + ")"; }
        internal static string Pull(char agentDirection, char boxDirection) { return "Pull(" + agentDirection + "," + boxDirection + ")"; }

    }
}