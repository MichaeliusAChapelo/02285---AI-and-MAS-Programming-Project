using BoxProblems;
using BoxProblems.Graphing;
using BoxProblems.Solver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                new ServerCommunicator().Run(args);
            }
            else
            {
                ServerCommunicator.PrintMap(); // Michaelius: With the new solver, everything messes up if I don't print this. DON'T ASK, I DON'T KNOW WHY


                string levelPath = ServerCommunicator.levelPath; // Go to ServerCommunicator to pick a level.

                var result = ProblemSolver.SolveLevel(levelPath, TimeSpan.FromHours(1), false);

                var superList = result.Select(x => x.solutionMovesParts).ToList();

                //new ServerCommunicator(superList).NonAsyncSolve(); // Solve locally for debugging purposes
                var fisk = new ServerCommunicator(superList);//.Run(args); // Uses heuristics to solve in server client.
                fisk.NonAsyncSolve();

                return;
                // Michaelius ENDO
            }
        }
    }
}
