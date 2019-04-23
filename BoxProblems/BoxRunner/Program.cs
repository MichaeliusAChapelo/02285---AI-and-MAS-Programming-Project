using BoxProblems;
using BoxProblems.Solver;
using System;

namespace BoxRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            var solution = ProblemSolver.SolveLevel("Levels/Old_Format/initial_levels/SAtowersOfSaigon04.lvl", TimeSpan.FromHours(1), false);
            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
