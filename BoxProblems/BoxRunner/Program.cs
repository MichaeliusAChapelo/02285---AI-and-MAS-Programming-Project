using BoxProblems;
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

        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            var solution = ProblemSolver.SolveLevel(GetLevelPath("MATALK.lvl"), TimeSpan.FromHours(1), false);
            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
