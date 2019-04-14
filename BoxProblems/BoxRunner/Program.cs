using BoxProblems;
using System;

namespace BoxRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("asdasda");
            ProblemSolver.GetSolveStatistics("Levels/Old_Format/real_levels/SACybot.lvl", TimeSpan.MaxValue, false);
            Console.WriteLine("Hello World!");
            Console.Read();
        }
    }
}
