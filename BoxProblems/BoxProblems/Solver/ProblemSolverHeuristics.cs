using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private static Entity GetGoalToSolve(GoalNode[] goals, GoalGraph goalGraph, BoxConflictGraph currentConflicts, HashSet<Entity> solvedGoals)
        {
            return goals.Where(x => !solvedGoals.Contains(x.Value.Ent)).First().Value.Ent;
        }

        private static Entity GetBoxToSolveProblem(BoxConflictGraph currentConflicts, Entity goal)
        {
            foreach (var iNode in currentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.BOX && boxNode.Value.Ent.Type == goal.Type)
                {
                    return boxNode.Value.Ent;
                }
            }

            throw new Exception("No box exist that can solve the goal.");
        }

        private static Entity GetAgentToSolveProblem(BoxConflictGraph currentConflicts, Entity toMove)
        {
            foreach (var iNode in currentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode agentNode && agentNode.Value.EntType == EntityType.AGENT && agentNode.Value.Ent.Color == toMove.Color)
                {
                    return agentNode.Value.Ent;
                }
            }

            throw new Exception("No agent exists that can solve this problem.");
        }

        private static Point GetFreeSpaceToMoveConflictTo(Entity conflict, BoxConflictGraph currentConflicts, Dictionary<Point, int> freePath)
        {
            foreach (var iNode in currentConflicts.Nodes)
            {
                if (iNode is FreeSpaceNode freeSpaceNode)
                {
                    var sdf = freeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x));
                    if (sdf.Count() > 0)
                    {
                        return sdf.First();
                    }
                }
            }

            throw new Exception("No free space is available");
        }
    }
}
