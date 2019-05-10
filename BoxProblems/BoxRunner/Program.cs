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

        static void Main(string[] args)
        {
            ServerCommunicator.SkipConsoleRead = false;

            //string levelPath = "MABahaMAS.lvl";
            //string levelPath = "MAExample.lvl";
            //string levelPath = "friendofDFS.lvl";
            //string levelPath = "SAKarlMarx.lvl";
            //string levelPath = "SAExample.lvl";
            //string levelPath = "SACrunch.lvl";
            //string levelPath = "SAAiMasTers.lvl";
            //string levelPath = "SAExample2.lvl";
            //string levelPath = "MAPullPush.lvl";
            //string levelPath = "MAFiveWalls.lvl";
            //string levelPath = "MAPullPush2.lvl";
            //string levelPath = "SABahaMAS.lvl";
            //string levelPath = "MACorridor.lvl";
            //string levelPath = "SAlabyrinthOfStBertin.lvl";
            //string levelPath = "MAKarlMarx.lvl";
            string levelPath = "SAVisualKei.lvl";

            string convertedLevelPath = "temp.lvl";

            ServerCommunicator serverCom = new ServerCommunicator();
            if (args.Length == 0 && !ServerCommunicator.SkipConsoleRead)
            {
                levelPath = GetLevelPath(levelPath);
                ConvertFilesToCorrectFormat(levelPath, convertedLevelPath);

                serverCom.StartServer(convertedLevelPath);
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

                var highLevelCommands = ProblemSolver.SolveLevel(level, TimeSpan.FromHours(1), false);
                var lowLevelCommands = serverCom.NonAsyncSolve(level, highLevelCommands);
                //serverCom.SendCommandsSequentially(lowLevelCommands, level);

                var finalCommands = CommandParallelizer.Parallelize(lowLevelCommands, level);
                serverCom.SendCommands(finalCommands);


                return;
                // Michaelius ENDO
            }
        }
    }
}
