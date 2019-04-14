using BoxProblems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BoxPerformance
{
    internal class Program
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

        static void Main(string[] args)
        {
            List<string> filePaths = GetFilePathsFromFolderRecursively("Levels");
            ConcurrentBag<SolveStatistic> statisticsBag = new ConcurrentBag<SolveStatistic>();

            //foreach (var filepath in filePaths)
            //{
            //    Console.WriteLine($"Running {Path.GetFileName(filepath)}");
            //    statistics.Add(ProblemSolver.GetSolveStatistics(filepath, TimeSpan.FromSeconds(5), false));
            //}

            Parallel.ForEach(filePaths, x =>
            {
                Console.WriteLine($"Running {Path.GetFileName(x)}");
                statisticsBag.Add(ProblemSolver.GetSolveStatistics(x, TimeSpan.FromSeconds(5), false));
            });

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            List<SolveStatistic> statistics = statisticsBag.ToList();
            Console.WriteLine($"Success: {statistics.Sum(x => x.Status == SolverStatus.SUCCESS ? 1 : 0)}");
            Console.WriteLine($"Timeout: {statistics.Sum(x => x.Status == SolverStatus.TIMEOUT ? 1 : 0)}");
            Console.WriteLine($"Error  : {statistics.Sum(x => x.Status == SolverStatus.ERROR ? 1 : 0)}");

            Console.Read();
        }
    }
}
