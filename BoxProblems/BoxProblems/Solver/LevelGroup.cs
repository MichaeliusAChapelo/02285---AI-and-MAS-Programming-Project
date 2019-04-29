using System.Collections.Generic;
using System.Linq;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private class LevelGroup
        {
            public readonly List<Point> FreeSpaces;
            public readonly List<Entity> Boxes;
            public readonly List<Entity> Agents;
            public readonly List<Entity> Goals;

            public LevelGroup(List<Point> freeSpaces, List<Entity> boxes, List<Entity> agents, List<Entity> goals)
            {
                this.FreeSpaces = freeSpaces;
                this.Boxes = boxes;
                this.Agents = agents;
                this.Goals = goals;
            }

            public bool HasEverythingItNeeds()
            {
                foreach (var goalGroup in Goals.GroupBy(x => x.Type))
                {
                    if (!(Boxes.Count(x => x.Type == goalGroup.Key) >= goalGroup.Count()))
                    {
                        return false;
                    }
                    if (!Agents.Any(x => x.Type == goalGroup.Key))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
