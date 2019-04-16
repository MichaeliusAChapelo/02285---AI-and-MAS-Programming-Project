using BoxProblems;
using System;

namespace BoxRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("asdasda");
            ProblemSolver.SolveLevel("Levels/Old_Format/initial_levels/SAtowersOfSaigon04.lvl", TimeSpan.FromHours(1), false);
            Console.Read();
        }
    }
}
