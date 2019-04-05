﻿using System;
using System.Collections.Generic;
using System.IO;

namespace BoxProblems
{
    internal class ServerCommunicator
    {
        const string strategy = "-astar";
        //const string levelPath = "MAExample.lvl";
        const string levelPath = "MAFiveWalls.lvl";

        public void Run(string[] args)
        {
            if (args.Length == 0)
                System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {levelPath} -c 'dotnet BoxProblems.dll {strategy}' -g 150 -t 300");
            else
            {
                PrintMap();

                // Ideally, you should input a solution here.
                //NaiveSolver.level = Level.ReadOldFormatLevel(File.ReadAllLines("MABahaMAS.lvl"),"asd");
                NaiveSolver.level = Level.ReadLevel(File.ReadAllLines(levelPath));
                var solver = new NaiveSolver();
                solver.Solve(); // A most convenient function.

                //ExampleCommands(); // Should be commented out.
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