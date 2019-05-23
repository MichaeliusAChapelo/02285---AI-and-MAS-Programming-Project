using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoxProblems.Solver
{
    internal static class HighLevelOptimizer
    {
        public static HighlevelLevelSolution Optimize(HighlevelLevelSolution solution)
        {
            solution = RemoveMovingSameThingTwiceInARow(solution);

            return solution;
        }

        private static HighlevelLevelSolution RemoveMovingSameThingTwiceInARow(HighlevelLevelSolution solution)
        {
            List<HighlevelMove> solutionMoves = solution.SolutionMovesParts;
            var optimizedSolution = new List<HighlevelMove>();
            int counter = 0;
            bool finalMoveWasEdited = false;
            for (int i = 0; i < solutionMoves.Count - 1; i++)
            {
                //check that the same thing is moved twice in a row
                if (solutionMoves[i].ToHere == solutionMoves[i + 1].MoveThis.Pos && solutionMoves[i].UsingThisAgent.HasValue == solutionMoves[i + 1].UsingThisAgent.HasValue)
                {
                    //if there is an agent to move then it should be the same agent moving the box in both moves.
                    //Otherwise the agent could be moved to an incorrect position as agents now don't go back to their original position.
                    if (!solutionMoves[i].UsingThisAgent.HasValue || (solutionMoves[i].UsingThisAgent.HasValue && solutionMoves[i].UsingThisAgent.Value.Type == solutionMoves[i + 1].UsingThisAgent.Value.Type))
                    {
                        optimizedSolution.Add(new HighlevelMove(solutionMoves[i + 1].CurrentState, solutionMoves[i].MoveThis, solutionMoves[i + 1].ToHere, solutionMoves[i].UsingThisAgent, solutionMoves[i + 1].AgentFinalPos));
                        i++;
                        if (i == solutionMoves.Count - 1)
                            finalMoveWasEdited = true;
                    }
                }
                else
                {
                    optimizedSolution.Add(solutionMoves[i]);
                }
                counter++;
            }
            if (solutionMoves.Count > 0 && !finalMoveWasEdited)
            {
                optimizedSolution.Add(solutionMoves.Last());
            }
            return new HighlevelLevelSolution(optimizedSolution, solution.SolutionGraphs, solution.Level);
        }

        //public static void WriteToFile(HighlevelLevelSolution solution)
        //{
        //    string fileName = @"C:\Users\theis\Desktop\MultiAgent\Project\HighlevelMoves.txt";
        //    try
        //    {
        //        // Check if file already exists. If yes, delete it.     
        //        if (File.Exists(fileName))
        //        {
        //            File.Delete(fileName);
        //        }

        //        // Create a new file     
        //        using (StreamWriter sw = File.CreateText(fileName))
        //        {
        //            foreach (HighlevelMove move in solution.SolutionMovesParts)
        //            {
        //                sw.WriteLine(move);
        //            }
        //        }
        //    }
        //    catch (Exception Ex)
        //    {
        //        Console.WriteLine(Ex);
        //    }
        //}
    }
}
