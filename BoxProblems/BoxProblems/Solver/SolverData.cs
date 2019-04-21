using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private class SolverData
        {
            public readonly Dictionary<Point, int> FreePath = new Dictionary<Point, int>();
            public readonly List<BoxConflictGraph> SolutionGraphs = new List<BoxConflictGraph>();
            public readonly HashSet<Entity> RemovedEntities = new HashSet<Entity>();
            public readonly Level Level;
            public readonly CancellationToken CancelToken;
            public BoxConflictGraph CurrentConflicts;
            public State CurrentState;

            public SolverData(Level level, CancellationToken cancelToken)
            {
                this.Level = level;
                this.CancelToken = cancelToken;
                this.CurrentState = level.InitialState;
            }

            public void AddToFreePath(Point[] path)
            {
                foreach (var pos in path)
                {
                    AddToFreePath(pos);
                }
            }

            public void AddToFreePath(Point pos)
            {
                if (FreePath.TryGetValue(pos, out int value))
                {
                    FreePath[pos] = value + 1;
                }
                else
                {
                    FreePath.Add(pos, 1);
                }
            }

            public void RemoveFromFreePath(Point[] path)
            {
                foreach (var pos in path)
                {
                    RemoveFromFreePath(pos);
                }
            }

            public void RemoveFromFreePath(Point pos)
            {
                int value = FreePath[pos];
                if (value == 1)
                {
                    FreePath.Remove(pos);
                }
                else
                {
                    FreePath[pos] = value - 1;
                }
            }

            public Entity GetEntity(int index)
            {
                return CurrentState.Entities[index];
            }

            public int GetEntityIndex(Entity entity)
            {
                return Array.IndexOf(CurrentState.Entities, entity);
            }

            public Entity? GetEntityAtPos(Point pos)
            {
                foreach (var entity in CurrentState.Entities)
                {
                    if (entity.Pos == pos)
                    {
                        return entity;
                    }
                }

                return null;
            }

            public Entity? GetGoalEntityAtPos(Point pos)
            {
                foreach (var goal in Level.Goals)
                {
                    if (goal.Pos == pos)
                    {
                        return goal;
                    }
                }

                return null;
            }
        }
    }
}
