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
            ServerCommunicator.SkipServerLaunch = false;
            bool Parallelize = true;

            // Failed to find any possible distant turning point, possibly blocked by other agents.
            // string levelPath = "MANOAsArk.lvl";

            // Failed to find the agent.
            // string levelPath = "MAOneOneTwo.lvl";

            // Index was outside the bounds of the array.
            // string levelPath = "SAGroupName.lvl";

            // sub problem depth limit reached.
            // string levelPath = "MAGroupName.lvl";

            // reee
            // string levelPath = "SARegExAZ.lvl";
            // string levelPath = "MABob.lvl";

            // Pulling to faraway turn-point not yet implemented.
            //string levelPath = "MASoulman.lvl";
            //string levelPath = "SAOneOneTwo.lvl";

            // Found no path from  entity to goal.
            // string levelPath = "SAAIMAS.lvl";
            // string levelPath = "SAPOPstars.lvl";

            // Can't move into agent
            // string levelPath = "MAgroup.lvl";
            // string levelPath = "MANulPoint.lvl";

            //Not enough free space is available
            string levelPath = "MABob.lvl";
            //string levelPath = "SAWallZ.lvl";
            //string levelPath = "SAgTHIRTEEN.lvl";
            //string levelPath = "SANulPoint.lvl";
            //string levelPath = "SATheBTeam.lvl";
            //string levelPath = "MAgTHIRTEEN.lvl";
            //string levelPath = "MAMKM.lvl";
            //string levelPath = "MASubpoena.lvl";
            //string levelPath = "MAMASAI.lvl";




            #region Mein Levels
            //string levelPath = "SAVisualKei.lvl";
            //string levelPath = "MAVisualKei.lvl";
            #endregion

            #region Optimize these
            //string levelPath = "SAanagram.lvl";
            //string levelPath = "SAtesuto.lvl";
            #endregion

            #region Bugfix
            //string levelPath = "MACorridor.lvl";
            //string levelPath = "SAsimple2.lvl";
            //string levelPath = "SADangerbot.lvl"; 
            #endregion

            //Not enough free space
            //string levelPath = "SAGroupOne.lvl";

            string convertedLevelPath = "temp.lvl";

            ServerCommunicator serverCom = new ServerCommunicator();
            if (args.Length == 0 && !ServerCommunicator.SkipServerLaunch)
            {
                levelPath = GetLevelPath(levelPath);
                ConvertFilesToCorrectFormat(levelPath, convertedLevelPath);

                serverCom.StartServer(convertedLevelPath);
            }
            else
            {
                ServerCommunicator.GiveGroupNameToServer();

                Level level;
                if (ServerCommunicator.SkipServerLaunch)
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
