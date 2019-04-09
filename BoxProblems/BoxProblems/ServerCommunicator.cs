using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoxProblems
{
    internal class ServerCommunicator
    {
        const string strategy = "-astar";
        const string levelPath = "MAFiveWalls.lvl";
        //const string levelPath = "Levels\\MABahaMAS.lvl";

        public void Run(string[] args)
        {
            if (args.Length == 0)
                System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {levelPath} -c 'dotnet BoxProblems.dll {strategy}' -g 150 -t 300");
            else
            {
                PrintMap(); // Definitely not necessary.

                // Pick one!
                //NonAsyncSolve();
                AsyncSolve();
            }
        }

        public void NonAsyncSolve()
        {
            // Ideally, you should input a solution here.
            var solver = new NaiveSolver(Level.ReadLevel(File.ReadAllLines(levelPath)));
            solver.Solve(); // A most convenient function.
        }

        public void AsyncSolve()
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

        public void PrintMap()
        {
            Console.Error.WriteLine("C# Client initialized.");
            Console.WriteLine(); // Input to trigger Java client to respond.

            string line;
            while ((line = Console.ReadLine()) != "#end")
                Console.Error.WriteLine(line); // Print map input.
            Console.Error.WriteLine(line + "\n End of map file. \n");
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
            string response = Console.ReadLine();
            Console.Error.WriteLine(command + "\n" + response);
            return response;
        }

        public static string Command(string[] commands) { return Command(String.Join(';', commands)); }
        public static string Command(List<string> commands) { return Command(String.Join(';', commands)); }

        public static string NoOp() { return "NoOp"; }
        public static string Move(Direction agentDirection) { return "Move(" + agentDirection.ToString() + ")"; }
        public static string Push(Direction agentDirection, Direction boxDirection) { return "Push(" + agentDirection.ToString() + "," + boxDirection.ToString() + ")"; }
        public static string Pull(Direction agentDirection, Direction boxDirection) { return "Pull(" + agentDirection.ToString() + "," + boxDirection.ToString() + ")"; }

        public static string Move(char agentDirection) { return "Move(" + agentDirection + ")"; }
        public static string Push(char agentDirection, char boxDirection) { return "Push(" + agentDirection + "," + boxDirection + ")"; }
        public static string Pull(char agentDirection, char boxDirection) { return "Pull(" + agentDirection + "," + boxDirection + ")"; }

    }
}