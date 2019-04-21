using System.Collections.Generic;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private readonly struct LevelGroupsInfo
        {
            public readonly List<LevelGroup> Groups;

            public LevelGroupsInfo(bool _)
            {
                this.Groups = new List<LevelGroup>();
            }

            public void AddGroup(LevelGroup group)
            {
                Groups.Add(group);
            }
        }
    }
}
