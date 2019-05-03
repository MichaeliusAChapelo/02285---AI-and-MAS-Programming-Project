﻿using BoxProblems.Solver;
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
            System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {levelPath} -c 'dotnet BoxRunner.dll {strategy}' -g 150 -t 300 -s 25; Read-Host");
        }

        public void NonAsyncSolve(Level level, List<HighlevelLevelSolution> levelSolutions)
        {
            foreach (HighlevelLevelSolution list in levelSolutions)
            {
                var solver = new LessNaiveSolver(level, list.Level, list.SolutionMovesParts);
                solver.Solve(); // A most convenient function.
            }
        }

        public static void GiveGroupNameToServer()
        {
            Console.WriteLine("VisualKei");
        }

        public static Level GetLevelFromServer()
        {
            List<string> levelStrings = new List<string>();

            string line;
            do
            {
                line = Console.ReadLine();
                levelStrings.Add(line);
            } while (line != "#end");

            return Level.ReadLevel(levelStrings.ToArray());
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
            Console.Error.WriteLine(command);
            if (SkipConsoleRead) return string.Empty;
            string response = Console.ReadLine();

            //Console.Error.WriteLine("COMMAND: " + command + "\nRESPONSE: " + response);
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