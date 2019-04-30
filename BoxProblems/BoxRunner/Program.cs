using BoxProblems;
using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            string levelPath = ServerCommunicator.levelPath; // Go to ServerCommunicator to pick a level.

            var result = ProblemSolver.SolveLevel(levelPath, TimeSpan.FromHours(1), false);

            var superList = result.Select(x => x.solutionMovesParts).ToList();

            new ServerCommunicator(superList).NonAsyncSolve(); // Solve locally for debugging purposes
            //new ServerCommunicator(superList).Run(args); // Uses heuristics to solve in server client.
            return;
            // Michaelius ENDO
        }
    }
}
