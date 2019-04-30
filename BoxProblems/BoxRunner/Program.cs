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
            var solution = ProblemSolver.SolveLevel("Levels/Old_Format/real_levels/SAEasyPeasy.lvl", TimeSpan.FromHours(1), false);
            //var solution = ProblemSolver.SolveLevel("Levels/New_Format/MACorridor.lvl", TimeSpan.FromHours(1), false);
            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
