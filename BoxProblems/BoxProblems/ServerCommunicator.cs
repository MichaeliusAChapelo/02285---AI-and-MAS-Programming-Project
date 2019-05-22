using BoxProblems.Solver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxProblems
{
    public class ServerCommunicator
    {
        const string strategy = "-astar";
        public static bool SkipServerLaunch = false;

        public void StartServer(string levelPath)
        {
            System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {levelPath} -c 'dotnet BoxRunner.dll {strategy}' -g 150 -t 300 -s 50; Read-Host");
        }

        public List<AgentCommands> NonAsyncSolve(Level level, List<HighlevelLevelSolution> levelSolutions)
        {
            List<AgentCommands> commands = new List<AgentCommands>();
            foreach (HighlevelLevelSolution list in levelSolutions)
            {
                var solver = new LessNaiveSolver(level, list.Level, list.SolutionMovesParts);
                commands.AddRange(solver.Solve()); // A most convenient function.
            }

            return commands;
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

        public void SendCommands(string[] commands)
        {
            StringBuilder sBuilder = new StringBuilder();
            const int maxSize = 5_000;
            int charsUsed = 0;
            int lines = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                if (charsUsed + commands[i].Length + 3 > maxSize)
                {
                    SendBatchCommands(sBuilder, lines);

                    sBuilder.Clear();
                    charsUsed = 0;
                    lines = 0;
                }

                sBuilder.AppendLine(commands[i]);
                charsUsed += commands[i].Length;
                lines++;
            }

            SendBatchCommands(sBuilder, lines);
        }

        private void SendBatchCommands(StringBuilder sBuilder, int lines)
        {
            Console.Write(sBuilder);
            //Console.Error.WriteLine(sBuilder);

            if (!SkipServerLaunch)
            {
                for (int z = 0; z < lines; z++)
                {
                    string response = Console.ReadLine();
                    //if (response.Contains("false"))
                    //{
                    //    throw new Exception("Sent illegal command to server.");
                    //}
                }
            }
        }

        public static void SendCommand(string command)
        {
            Console.WriteLine(command);
            Console.Error.WriteLine("Debug: " + command);
            if (SkipServerLaunch) return;
            string response = Console.ReadLine();

            //Console.Error.WriteLine("COMMAND: " + command + "\nRESPONSE: " + response);
            if (response.Contains("false"))
            {
                throw new Exception("Sent illegal command to server.");
            }
        }

        public void SendCommandsSequentially(List<AgentCommands> commands, Level level)
        {
            string[] output = new string[level.AgentCount];

            foreach (AgentCommands agentCommands in commands)
            {
                foreach (var command in agentCommands.Commands)
                {
                    Array.Fill(output, AgentCommand.NoOp());
                    output[agentCommands.AgentIndex] = command.ToString();

                    SendCommand(CreateCommand(output));
                }
            }
        }

        internal static string CreateCommand(string[] commands) => String.Join(';', commands);
    }
}