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

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            var errorGroups = statistics.Where(x => x.Status == SolverStatus.ERROR)
                                        .GroupBy(x => string.Join(Environment.NewLine, x.ErrorThrown.StackTrace.Split(Environment.NewLine).Take(2))).OrderByDescending(x => x.Count()).ToList();

            foreach (var errorGroup in errorGroups.Take(Math.Min(3, errorGroups.Count)))
            {
                Console.WriteLine("Levels with this error:");
                Console.WriteLine(string.Join(Environment.NewLine, errorGroup.Select(x => x.LevelName)));
                Console.WriteLine();
                Console.WriteLine("Error: ");
                Console.WriteLine(errorGroup.First().ErrorThrown.Message + Environment.NewLine + errorGroup.First().ErrorThrown.StackTrace);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }


            Console.Read();
        }
    }
}
