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

        public SolveStatistic(long runTimeInMiliseconds, Exception error, SolverStatus status, string levelName)
        {
            this.RunTimeInMiliseconds = runTimeInMiliseconds;
            this.ErrorThrown = error;
            this.Status = status;
            this.LevelName = levelName;
        }
    }
}
