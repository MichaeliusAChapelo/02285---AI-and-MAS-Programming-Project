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
            ServerCommunicator.SkipConsoleRead = true;

            //string levelPath = "SABahaMAS.lvl";
            //string levelPath = "MAExample.lvl";
            //string levelPath = "SAExample.lvl";
            string levelPath = "SACrunch.lvl";
            //string levelPath = "SAAiMasTers.lvl";
            //string levelPath = "SAExample2.lvl";
            //string levelPath = "MAPullPush.lvl";
            //string levelPath = "MAFiveWalls.lvl";
            //string levelPath = "MAPullPush2.lvl";
            //string levelPath = "SABahaMAS.lvl";
            //string levelPath = "MACorridor.lvl";
            //string levelPath = "SAlabyrinthOfStBertin.lvl";
            //string levelPath = "MAKarlMarx.lvl";'


            levelPath = GetLevelPath(levelPath);
            string convertedLevelPath = "temp.lvl";

            ServerCommunicator serverCom = new ServerCommunicator();
            if (args.Length == 0 && !ServerCommunicator.SkipConsoleRead)
            {
                ConvertFilesToCorrectFormat(levelPath, convertedLevelPath);

                serverCom.StartServer(convertedLevelPath);
            }
            else
            {
                ServerCommunicator.PrintMap(); // Michaelius: With the new solver, everything messes up if I don't print this. DON'T ASK, I DON'T KNOW WHY

                var result = ProblemSolver.SolveLevel(convertedLevelPath, TimeSpan.FromHours(1), false);

                //new ServerCommunicator(superList).NonAsyncSolve(); // Solve locally for debugging purposes
                serverCom.NonAsyncSolve(result);//.Run(args); // Uses heuristics to solve in server client.

                return;
                // Michaelius ENDO
            }
        }
    }
}
