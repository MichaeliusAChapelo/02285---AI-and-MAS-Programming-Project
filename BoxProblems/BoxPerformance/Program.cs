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
                return (float)(Math.Log(Math.Min(time, (float)SATimeScore)) / Math.Log(time));
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
                return (float)(Math.Log(Math.Min(time, (float)MATimeScore)) / Math.Log(time));
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
        private readonly string CompetitionFolder;
        public readonly string CompetitionName;

        public CompetitionScores(string subFolder, string competitionName)
        {
            this.CompetitionFolder = subFolder;
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
                //if (!Directory.EnumerateDirectories(statistic.LevelPath).Any(x => x == CompetitionFolder))
                //{
                //    continue;
                //}

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
            new CompetitionScores("comp_levels_2017", "Year 2017")
            {
                { "AIoliMAsh"   ,  1930, 1416,   80,  281 },
                { "Beliebers"   ,   316,  252,   99,  385 },
                { "Blinky"      ,   124,   17,   52,   46 },
                { "BoxBunny"    ,  3213, 1054,  842,  902 },
                { "Bronies"     , 12662, 1830,  214,  530 },
                { "DAT"         ,   953,  484,  231,  216 },
                { "DoDo"        ,   228,  122,    4,   15 },
                { "EvilCorp"    ,  3262, 5970,  192,  253 },
                { "FooBar"      ,   193, 3341,   12,  106 },
                { "GeneralAI"   ,   521,  287,  128,  265 },
                { "groupname"   ,  3687,  433,   32,  104 },
                { "HiveMind"    ,   234,  269,  107,  264 },
                { "IamGreedy"   ,   927,  608,  188, 1168 },
                { "Jomarki"     ,    60,   15,   41,   71 },
                { "Kalle"       ,   202,  152,   31,  128 },
                { "Lemmings"    ,   114,  118,   53,   71 },
                { "Liquorice"   ,    95,   80,   69,  151 },
                { "MasAiArne"   ,   604,  203, 1563, 2073 },
                { "MASters"     ,   212,  268,  214,  526 },
                { "NeverMind"   ,   238,  182,   27,  114 },
                { "Omnics"      ,  3592, 2307,  579,  658 },
                { "tnrbts"      ,   262,  205, 1108, 1714 },
            },
            new CompetitionScores("real_levels", "Year 2018")
            {
                { "AiAiCap"     ,    70,  260,  114,     89 },
                { "AIFather"    ,    90,  255,   50,    137 },
                { "AiMasTers"   , 15702, 2249,    9,    169 },
                { "AlphaOne"    ,    89,  260,  467,    658 },
                { "AntsStar"    ,   238,  224,   88,    319 },
                { "BahaMAS"     ,    90,  105,   64,    312 },
                { "bAnAnA"      ,   409,  678,   77,    153 },
                { "BeTrayEd"    ,   234,  257,  188,    405 },
                { "bongu"       ,    80,   73,   63,    224 },
                { "ByteMe"      ,   970,  403,  131,    334 },
                { "Cybot"       ,   140,  305,   68,    260 },
                { "dashen"      ,  3857,  859,  467,    337 },
                { "DaVinci"     ,  4102,  840,  102,    283 },
                { "EasyPeasy"   ,   604,  408,   44,    215 },
                { "GreenDots"   ,   187,  266,  113,    298 },
                { "Kaldi"       ,  2809, 1381,  133,    377 },
                { "KarlMarx"    , 13792, 1011, 1683, 167943 },
                { "KJFWAOL"     ,    43,   41,   15,     67 },
                { "Lobot"       ,    75,  227,  131,    329 },
                { "Magicians"   ,   397,  220,   61,    116 },
                { "Navy"        ,  4579, 1483,  299,    689 },
                { "Nikrima"     ,   222,   44,   30,     99 },
                { "NotHard"     ,   150,   37,   58,    370 },
                { "ora"         ,   199,   42,   82,    314 },
                { "PushPush"    ,   770,  561,  147,    466 },
                { "ZEROagent"   ,   480,   98,  295,    534 },
            },
            new CompetitionScores("comp_levels", "Year 2019")
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
            public string LevelPath;
            public int Moves;
            public long Time;

            public LevelStatistic(string path, string name, int moves, long time)
            {
                this.LevelPath = path;
                this.LevelName = name;
                this.Moves = moves;
                this.Time = time;
            }
        }

        static void Main(string[] args)
        {
            //List<string> filePaths = GetFilePathsFromFolderRecursively("Levels\\Old_Format\\comp_levels_2017"); // 68.37
            //List<string> filePaths = GetFilePathsFromFolderRecursively("Levels\\Old_Format\\real_levels"); // 81.41
            List<string> filePaths = GetFilePathsFromFolderRecursively("Levels\\New_Format\\comp_levels"); // 66.44
            ConcurrentBag<SolveStatistic> statisticsBag = new ConcurrentBag<SolveStatistic>();
            ConcurrentBag<LevelStatistic> levelStatisticsBag = new ConcurrentBag<LevelStatistic>();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(filePaths, x =>
            {
                var statistic = ProblemSolver.GetSolveStatistics(x, TimeSpan.FromSeconds(70), false);
                int? movesCount = null;

                if (statistic.Status == SolverStatus.SUCCESS)
                {
                    try
                    {
                        long startTime = watch.ElapsedMilliseconds;
                        var sc = new ServerCommunicator();
                        var commands = sc.NonAsyncSolve(statistic.Level, statistic.Solution);
                        movesCount = CommandParallelizer.Parallelize(commands, statistic.Level).Length;
                        long endTime = watch.ElapsedMilliseconds;

                        levelStatisticsBag.Add(new LevelStatistic(x, statistic.LevelName, movesCount.Value, endTime - startTime));
                    }
                    catch (Exception e)
                    {
                        statistic.Status = SolverStatus.ERROR;
                        statistic.ErrorThrown = e;
                    }
                }

                Console.WriteLine($"{statistic.Status.ToString()} {Path.GetFileName(x)} Time: {statistic.RunTimeInMiliseconds} Moves: {(movesCount ?? -1)}");
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
