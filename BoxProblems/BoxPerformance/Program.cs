using BoxProblems;
using BoxProblems.Solver;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BoxPerformance
{
    internal class CompetitionScores : IEnumerable<string>
    {
        private class LevelScore
        {
            public string Name;
            public int SAMoveScore;
            public int SATimeScore;
            public int MAMoveScore;
            public int MATimeScore;

            public LevelScore(string name, int saMoves, int saTime, int maMoves, int maTimes)
            {
                this.Name = name;
                this.SAMoveScore = saMoves;
                this.SATimeScore = saTime;
                this.MAMoveScore = maMoves;
                this.MATimeScore = maTimes;
            }

            public float CalculateSAMoveScore(int moves)
            {
                return Math.Min(moves, (float)SAMoveScore) / moves;
            }

            public float CalculateSATimeScore(long time)
            {
                if (time <= 1)
                {
                    return 1;
                }
                if (SATimeScore == 0)
                {
                    return 0;
                }
                float ass = (float)(Math.Log(Math.Min(time, (float)SATimeScore)) / Math.Log(time));
                if (float.IsNaN(ass))
                {

                }
                return ass;
            }

            public float CalculateMAMoveScore(int moves)
            {
                return Math.Min(moves, (float)MAMoveScore) / moves;
            }

            public float CalculateMATimeScore(long time)
            {
                if (time <= 1)
                {
                    return 1;
                }
                if (MATimeScore == 0)
                {
                    return 0;
                }
                float ass = (float)(Math.Log(Math.Min(time, (float)MATimeScore)) / Math.Log(time));
                if (float.IsNaN(ass))
                {

                }
                return ass;
            }
        }

        internal class CompetitionScore
        {
            public float SAMoveScore;
            public float SATimeScore;
            public float MAMoveScore;
            public float MATimeScore;
            
            public CompetitionScore(float saMoves, float saTime, float maMoves, float maTimes)
            {
                this.SAMoveScore = saMoves;
                this.SATimeScore = saTime;
                this.MAMoveScore = maMoves;
                this.MATimeScore = maTimes;
            }
        }

        Dictionary<string, LevelScore> Scores = new Dictionary<string, LevelScore>();
        public readonly string CompetitionName;

        public CompetitionScores(string competitionName)
        {
            this.CompetitionName = competitionName;
        }

        public void Add(string name, int saMoves, int saTime, int maMoves, int maTimes)
        {
            Scores.Add(name, new LevelScore(name, saMoves, saTime, maMoves, maTimes));
        }

        public CompetitionScore GetScore(List<Program.LevelStatistic> solutions)
        {
            float saMovesScore = 0;
            float saTimeScore = 0;
            float maMovesScore = 0;
            float maTimeScore = 0;

            foreach (var statistic in solutions)
            {
                ReadOnlySpan<char> nameWithType = statistic.LevelName;
                ReadOnlySpan<char> name = nameWithType.Slice(2);
                if (Scores.ContainsKey(name.ToString()))
                {
                    LevelScore score = Scores[name.ToString()];

                    ReadOnlySpan<char> levelType = nameWithType.Slice(0, 2);
                    bool isSingleAgent = levelType.ToString() == "SA";
                    if (isSingleAgent)
                    {
                        saMovesScore += score.CalculateSAMoveScore(statistic.Moves);
                        saTimeScore += score.CalculateSATimeScore(statistic.Time);
                    }
                    else
                    {
                        maMovesScore += score.CalculateMAMoveScore(statistic.Moves);
                        maTimeScore += score.CalculateMATimeScore(statistic.Time);
                    }
                }
            }

            return new CompetitionScore(saMovesScore, saTimeScore, maMovesScore, maTimeScore);
        }





        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    internal class Program
    {
        private static CompetitionScores[] ScoresData = new CompetitionScores[]
        {
            new CompetitionScores("Year 2019")
            {
                { "AIMAS"       ,   492,  158,  115,    11 },
                { "Avicii"      ,   432,  271,    5,     0 },
                { "Bob"         ,  7423,  385,   78,   240 },
                { "deepurple"   ,    70,   40,  126,    83 },
                { "ForThePie"   ,   509,  376,  338,    90 },
                { "Gronhoff"    ,     4,    0,    7,     2 },
                { "group"       ,  1272,   16,  307,   420 },
                { "GroupName"   ,  8261, 6155, 1826, 16378 },
                { "GruppeTo"    ,   183,    9,   83,   174 },
                { "gTHIRTEEN"   , 15118, 2353,  171, 10682 },
                { "HelloWorl"   , 18996, 2980, 1435,   582 },
                { "MASA"        ,    24,    0,   15,     1 },
                { "MASAI"       ,  1857, 8636,   44,    36 },
                { "MKM"         ,    63,    6,   21,     1 },
                { "Nameless"    ,   699,  122,   62,     4 },
                { "NOAsArk"     ,    42,    1,   18,     1 },
                { "NulPoint"    ,   313,  255,  254,   623 },
                { "OneOneTwo"   ,   291,   59,  510,   396 },
                { "POPstars"    ,   954,  385,  186,   305 },
                { "RegExAZ"     ,   127,   24,   87,    47 },
                { "Soulman"     ,   597,  242,  180,   618 },
                { "Subpoena"    ,  8666, 4194, 1053,   241 },
                { "TheBTeam"    ,  4420, 5228, 1018,    62 },
                { "VisualKei"   , 14325, 2148, 1825,  2671 },
                { "WallZ"       ,    92,   18,   14,     4 },
            }
        };

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

        internal class LevelStatistic
        {
            public string LevelName;
            public int Moves;
            public long Time;

            public LevelStatistic(string name, int moves, long time)
            {
                this.LevelName = name;
                this.Moves = moves;
                this.Time = time;
            }
        }

        static void Main(string[] args)
        {
            float asd = (float)(Math.Log(1) / Math.Log(1));

            List<string> filePaths = GetFilePathsFromFolderRecursively("Levels\\New_Format\\comp_levels");
            ConcurrentBag<SolveStatistic> statisticsBag = new ConcurrentBag<SolveStatistic>();
            ConcurrentBag<LevelStatistic> levelStatisticsBag = new ConcurrentBag<LevelStatistic>();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(filePaths, x =>
            {
                var statistic = ProblemSolver.GetSolveStatistics(x, TimeSpan.FromSeconds(70), false);

                if (statistic.Status == SolverStatus.SUCCESS)
                {
                    try
                    {
                        long startTime = watch.ElapsedMilliseconds;
                        var sc = new ServerCommunicator();
                        var commands = sc.NonAsyncSolve(statistic.Level, statistic.Solution);
                        int moves = CommandParallelizer.Parallelize(commands, statistic.Level).Length;
                        long endTime = watch.ElapsedMilliseconds;

                        levelStatisticsBag.Add(new LevelStatistic(statistic.LevelName, moves, endTime - startTime));
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
                var splittedError = orderedErrors.First().ErrorThrown.StackTrace.Split(Environment.NewLine);
                Console.WriteLine(orderedErrors.First().ErrorThrown.Message + Environment.NewLine + string.Join(Environment.NewLine, splittedError.Take(Math.Min(15, splittedError.Length))));
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

            foreach (var scoreData in ScoresData)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                var score = scoreData.GetScore(levelStatisticsBag.ToList());
                Console.WriteLine($"Competition: {scoreData.CompetitionName}");
                Console.WriteLine($"SA move score: {score.SAMoveScore.ToString("N2")}");
                Console.WriteLine($"SA time score: {score.SATimeScore.ToString("N2")}");
                Console.WriteLine($"MA move score: {score.MAMoveScore.ToString("N2")}");
                Console.WriteLine($"MA time score: {score.MATimeScore.ToString("N2")}");
                Console.WriteLine();
                Console.WriteLine($"Moves score:   {(score.SAMoveScore + score.MAMoveScore).ToString("N2")}");
                Console.WriteLine($"Time score:    {(score.SATimeScore + score.MATimeScore).ToString("N2")}");
                Console.WriteLine($"Total score:   {(score.SAMoveScore + score.MAMoveScore + score.SATimeScore + score.MATimeScore).ToString("N2")}");
            }


            Console.Read();
        }
    }
}
