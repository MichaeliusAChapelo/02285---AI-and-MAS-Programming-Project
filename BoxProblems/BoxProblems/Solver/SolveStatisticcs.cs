using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Solver
{
    public class SolveStatistic
    {
        public readonly long RunTimeInMiliseconds;
        public readonly Exception ErrorThrown;
        public SolverStatus Status;
        public string LevelName;
        public List<HighlevelLevelSolution> Solution;

        public SolveStatistic(long runTimeInMiliseconds, Exception error, SolverStatus status, string levelName, List<HighlevelLevelSolution> solution)
        {
            this.RunTimeInMiliseconds = runTimeInMiliseconds;
            this.ErrorThrown = error;
            this.Status = status;
            this.LevelName = levelName;
            this.Solution = solution;
        }
    }
}
