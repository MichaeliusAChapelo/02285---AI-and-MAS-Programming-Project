using BoxProblems;
using BoxProblems.Graphing;
using BoxProblems.Solver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BoxRunner
{
    class Program
    {
        private static List<string> GetFilePathsFromFolderRecursively(string folderPath)
        {
            List<string> filepaths = new List<string>();
            filepaths.AddRange(Directory.GetFiles(folderPath));

            foreach (var direcotry in Directory.GetDirectories(folderPath))
            {
                filepaths.AddRange(GetFilePathsFromFolderRecursively(direcotry));
            }

            return filepaths;
        }

        private static string GetLevelPath(string levelFileName)
        {
            List<string> files = GetFilePathsFromFolderRecursively("Levels");
            return files.Single(x => Path.GetFileName(x) == levelFileName);
        }

        private static void ConvertFilesToCorrectFormat(string levelPath, string savePath)
        {
            string[] oldFormat = File.ReadAllLines(levelPath);
            if (Level.IsNewFormatLevel(oldFormat))
            {
                File.WriteAllText(savePath, string.Join('\n', oldFormat));
            }
            else
            {
                string[] newFormat = Level.ConvertToNewFormat(oldFormat, Path.GetFileNameWithoutExtension(levelPath));
                File.WriteAllText(savePath, string.Join('\n', newFormat));
            }
        }

        private static void InteractiveConsole()
        {
            File.WriteAllText(communicatorPath, string.Empty);
            Console.WriteLine("Type your commands here:");
            List<string> history = new List<string>();
            while (true)
            {
                var s = Console.ReadLine();

                if (s == "save")
                    File.WriteAllLines(savePath, history);
                if (s == "load")
                    File.WriteAllLines(communicatorPath, File.ReadAllLines(savePath));
                else if (s.Contains("LFront"))
                {
                    var a = BoxSwimming.LeftHandBoxSwimming(s.Last());
                    history.AddRange(a);
                    File.WriteAllLines(communicatorPath, a);
                }
                else if (s.Contains("RFront"))
                {
                    var a = BoxSwimming.RightHandBoxSwimming(s.Last());
                    history.AddRange(a);
                    File.WriteAllLines(communicatorPath, a);
                }
                else if (s.Contains("LRot"))
                {
                    var a = BoxSwimming.SwimLeft(s.Last());
                    history.AddRange(a);
                    File.WriteAllLines(communicatorPath, a);
                }
                else if (s.Contains("RRot"))
                {
                    var a = BoxSwimming.SwimRight(s.Last());
                    history.AddRange(a);
                    File.WriteAllLines(communicatorPath, a);
                }
                else
                {
                    history.Add(s);
                    File.WriteAllText(communicatorPath, s);
                }
            }
        }

        private static void ServerReceiveInteractiveConsole(ServerCommunicator serverCom)
        {
            //serverCom.SendCommands(new string[4] { "Pull(E,N)", "Push(N,W)", "Pull(S,E)", "Push(E,N)" });
            serverCom.SendCommands(new string[1] { "NoOp" });
            string[] s;
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                s = File.ReadAllLines(communicatorPath);
                if (s.Length == 0) continue;
                if (s[0] == "end") break;

                serverCom.SendCommands(s);
                File.WriteAllText(communicatorPath, string.Empty);
            }
        }

        // Set to suitable folders before enabling Interactive Console.
        const string communicatorPath = @"C:\Meine Items\Coding Ambitions\8. Semester\02285 Box Problems\Box Problem Solver\Communicator.txt";
        const string savePath = @"C:\Meine Items\Coding Ambitions\8. Semester\02285 Box Problems\Box Problem Solver\saved.txt";

        static void Main(string[] args)
        {
            ServerCommunicator.SkipConsoleRead = false;
            bool InteractiveConsoleEnable = false; // WARNING: Set const folder paths above before enabling!
            bool Parallelize = ! ServerCommunicator.SkipConsoleRead;

            //string levelPath = "MARipOffNew.lvl";
            //string levelPath = "MAInterestingManeuver.lvl";
            //string levelPath = "SAVisualKei.lvl";
            string levelPath = "MAVisualKei.lvl";
            //string levelPath = "SAKarlMarx.lvl";


            // string levelPath = "MAAlphaOne.lvl";

            #region Optimize
            //string levelPath = "SAanagram.lvl";
            //string levelPath = "SAtesuto.lvl";
            #endregion

            #region Bugfix
            //string levelPath = "SAsimple2.lvl";
            //string levelPath = "SADangerbot.lvl"; 
            #endregion

            #region Heuristics fail
            //string levelPath = "SAOptimal.lvl";
            //string levelPath = "SAOmnics.lvl";
            //string levelPath = "SASolo.lvl";
            //string levelPath = "MADAT.lvl";
            //string levelPath = "MAbongu.lvl";
            //string levelPath = "MAJMAI.lvl";
            //string levelPath = "MACybot.lvl";
            //string levelPath = "MABeTrayEd.lvl";

            #endregion

            // Heuristic Win?!?
            //string levelPath = "SAdashen.lvl";



            #region Old shite
            // string levelPath = "SAtowersOfSaigon03.lvl";
            //string levelPath = "SAchoice3.lvl";
            //string levelPath = "MAExample.lvl";
            //string levelPath = "SAKarlMarx.lvl";
            //string levelPath = "SAAiMasTers.lvl";
            //string levelPath = "SAsoko3_32.lvl";
            //string levelPath = "MACorridor.lvl";
            //string levelPath = "MAKarlMarx.lvl";
            //string levelPath = "SAVisualKei.lvl";
            //string levelPath = "SALeo.lvl";
            //string levelPath = "MAInterestingManeuver.lvl";
            //string levelPath = "SAGeneralAI.lvl";
            //Not enough free space
            //string levelPath = "SAGroupOne.lvl";
            #endregion

            string convertedLevelPath = "temp.lvl";

            ServerCommunicator serverCom = new ServerCommunicator();
            if (args.Length == 0 && !ServerCommunicator.SkipConsoleRead)
            {
                levelPath = GetLevelPath(levelPath);
                ConvertFilesToCorrectFormat(levelPath, convertedLevelPath);

                serverCom.StartServer(convertedLevelPath);

                if (InteractiveConsoleEnable)
                    InteractiveConsole();
            }
            else
            {
                ServerCommunicator.GiveGroupNameToServer();

                Level level;
                if (ServerCommunicator.SkipConsoleRead)
                {
                    levelPath = GetLevelPath(levelPath);
                    ConvertFilesToCorrectFormat(levelPath, convertedLevelPath);
                    level = Level.ReadLevel(File.ReadAllLines(convertedLevelPath));
                }
                else
                {
                    level = ServerCommunicator.GetLevelFromServer();
                }

                if (InteractiveConsoleEnable)
                {
                    ServerReceiveInteractiveConsole(serverCom);
                    return;
                }

                var highLevelCommands = ProblemSolver.SolveLevel(level, TimeSpan.FromHours(1), false);
                var lowLevelCommands = serverCom.NonAsyncSolve(level, highLevelCommands);

                if (!Parallelize)
                    serverCom.SendCommandsSequentially(lowLevelCommands, level);
                else
                {
                    var finalCommands = CommandParallelizer.Parallelize(lowLevelCommands, level);
                    serverCom.SendCommands(finalCommands);
                }
                Console.Read();
                return;
            }
        }
    }
}
