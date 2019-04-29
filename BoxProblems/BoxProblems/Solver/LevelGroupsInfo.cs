using System.Collections.Generic;
using System.Linq;

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

            public bool IsSplit()
            {
                if (Groups.Count == 1)
                {
                    return false;
                }

                foreach (var group in Groups)
                {
                    if (!group.HasEverythingItNeeds())
                    {
                        return true;
                    }
                }

                return false;
            }

            public LevelGroup GetMainGroup()
            {
                LevelGroup bestGroup = Groups.First();
                foreach (var group in Groups)
                {
                    if (group.Goals.Count > bestGroup.Goals.Count)
                    {
                        bestGroup = group;
                    }
                }

                return bestGroup;
            }
        }
    }
}
