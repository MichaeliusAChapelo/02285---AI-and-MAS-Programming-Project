using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems.Solver
{
    public class SolveStatistic
    {
        public readonly long RunTimeInMiliseconds;
        public Exception ErrorThrown;
        public SolverStatus Status;
        public string LevelName;
        public List<HighlevelLevelSolution> Solution;
        public Level Level;

        public SolveStatistic(long runTimeInMiliseconds, Exception error, SolverStatus status, string levelName, List<HighlevelLevelSolution> solution, Level level)
        {
            this.RunTimeInMiliseconds = runTimeInMiliseconds;
            this.ErrorThrown = error;
            this.Status = status;
            this.LevelName = levelName;
            this.Solution = solution;
            this.Level = level;
        }
    }
}
