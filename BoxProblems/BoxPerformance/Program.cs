using BoxProblems;
using BoxProblems.Solver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
            List<string> filePaths = GetFilePathsFromFolderRecursively("Levels\\New_Format\\comp_levels");
            ConcurrentBag<SolveStatistic> statisticsBag = new ConcurrentBag<SolveStatistic>();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(filePaths, x =>
            {
                var statistic = ProblemSolver.GetSolveStatistics(x, TimeSpan.FromSeconds(10), false);

                if (statistic.Status == SolverStatus.SUCCESS)
                {
                    try
                    {
                        var sc = new ServerCommunicator();
                        var commands = sc.NonAsyncSolve(statistic.Level, statistic.Solution);
                        CommandParallelizer.Parallelize(commands, statistic.Level);
                    }
                    catch (Exception e)
                    {
                        statistic.Status = SolverStatus.ERROR;
                        statistic.ErrorThrown = e;
                    }
                }

                Console.WriteLine($"{statistic.Status.ToString()} {Path.GetFileName(x)} Time: {statistic.RunTimeInMiliseconds}");
                statisticsBag.Add(statistic);
            });
            watch.Stop();
            List<SolveStatistic> statistics = statisticsBag.ToList();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            var errorGroups = statistics.Where(x => x.Status == SolverStatus.ERROR)
                                        .GroupBy(x => string.Join(Environment.NewLine, x.ErrorThrown.StackTrace.Split(Environment.NewLine).Take(2)))
                                        .OrderByDescending(x => x.Count())
                                        .ToList();

            foreach (var errorGroup in errorGroups)
            {
                var orderedErrors = errorGroup.OrderBy(x => x.ErrorThrown.StackTrace.Split(Environment.NewLine).Length);
                Console.WriteLine("Levels with this error:");
                Console.WriteLine(string.Join(Environment.NewLine, orderedErrors.Select(x => x.LevelName)));
                Console.WriteLine();
                Console.WriteLine("Error: ");
                Console.WriteLine(orderedErrors.First().ErrorThrown.Message + Environment.NewLine + orderedErrors.First().ErrorThrown.StackTrace);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Timeout:");
            Console.WriteLine(string.Join(Environment.NewLine, statistics.Where(x => x.Status == SolverStatus.TIMEOUT).Select(x => x.LevelName)));

            Console.WriteLine();
            Console.WriteLine($"Total time: {watch.ElapsedMilliseconds}");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"Success: {statistics.Sum(x => x.Status == SolverStatus.SUCCESS ? 1 : 0)}");
            Console.WriteLine($"Timeout: {statistics.Sum(x => x.Status == SolverStatus.TIMEOUT ? 1 : 0)}");
            Console.WriteLine($"Error  : {statistics.Sum(x => x.Status == SolverStatus.ERROR ? 1 : 0)}");


            Console.Read();
        }
    }
}
