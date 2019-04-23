using System.Collections.Generic;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private readonly struct LevelGroup
        {
            public readonly List<Point> FreeSpaces;
            public readonly List<Entity> Entities;

            public LevelGroup(List<Point> freeSpaces, List<Entity> entities)
            {
                this.FreeSpaces = freeSpaces;
                this.Entities = entities;
            }
        }
    }
}
