using BoxProblems.Graphing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxProblems
{
    internal class HighlevelMove
    {
        public State CurrentState;
        public Entity MoveThis;
        public Point ToHere;
        public Entity? UsingThisAgent;

        public HighlevelMove(State state, Entity moveThis, Point toHere, Entity? usingThisAgent)
        {
            this.CurrentState = state;
            this.MoveThis = moveThis;
            this.ToHere = toHere;
            this.UsingThisAgent = usingThisAgent;
        }
    }

    internal static class ProblemSolver
    {
        public static List<(List<HighlevelMove> solutionMovesParts, List<BoxConflictGraph> solutionGraphs)> SolveLevel(Level level)
        {
            var solutionPieces = new ConcurrentBag<(List<HighlevelMove>, List<BoxConflictGraph>)>();
            foreach (var x in LevelSplitter.SplitLevel(level))
            {
                solutionPieces.Add(SolvePartialLevel(x));
            }
            //Parallel.ForEach(LevelSplitter.SplitLevel(level), x =>
            //{
            //    solutionPieces.Add(SolvePartialLevel(x));
            //    });

            return solutionPieces.ToList();
        }

        private static (List<HighlevelMove> solutionMoves, List<BoxConflictGraph> solutionGraphs) SolvePartialLevel(Level level)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            List<BoxConflictGraph> solutionGraphs = new List<BoxConflictGraph>();

            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph);
            HashSet<Entity> removedEntities = new HashSet<Entity>();
            BoxConflictGraph currentConflicts = new BoxConflictGraph(level.InitialState, level, null, removedEntities);
            State currentState = level.InitialState;
            HashSet<Point> freePath = new HashSet<Point>();
            HashSet<Entity> solvedGoals = new HashSet<Entity>();
            foreach (var goalPriorityLayer in priority.PriorityLayers)
            {
                for (int i = 0; i < goalPriorityLayer.Length; i++)
                {
                    currentConflicts = new BoxConflictGraph(currentState, level, null, removedEntities);
                    Entity goalToSolve = GetGoalToSolve(goalPriorityLayer, goalGraph, currentConflicts, solvedGoals);
                    Entity box = GetBoxToSolveProblem(currentConflicts, goalToSolve);
                    if (currentConflicts.PositionHasNode(goalToSolve.Pos))
                    {
                        INode nodeongoal = currentConflicts.GetNodeFromPosition(goalToSolve.Pos);
                        if (nodeongoal is BoxConflictNode boxongoal)
                        {
                            Point freespace = GetFreeSpaceToMoveConflictTo(goalToSolve, currentConflicts, freePath);
                            List<HighlevelMove> boxongoalSolution;
                            if (!TrySolveSubProblem(box, goalToSolve, boxongoal.Value.Ent, freespace, boxongoal.Value.EntType == EntityType.AGENT, level, solutionGraphs, ref currentConflicts, ref currentState, out boxongoalSolution, freePath, removedEntities))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solution.AddRange(boxongoalSolution);
                        }


                    }
                    currentConflicts = new BoxConflictGraph(currentState, level, goalToSolve, removedEntities);
                    currentConflicts.AddFreeNodes(level, box.Pos, goalToSolve.Pos);
                    solutionGraphs.Add(currentConflicts);
                    //GraphShower.ShowSimplifiedGraph(currentConflicts);
                    //Console.WriteLine(level.StateToString(currentConflicts.CreatedFromThisState));

                    var storeConflicts = currentConflicts;
                    var storeState = currentState;
                    List<HighlevelMove> solutionMoves;
                    if (!TrySolveSubProblem(box, goalToSolve, box, goalToSolve.Pos, false, level, solutionGraphs, ref currentConflicts, ref currentState, out solutionMoves, freePath, removedEntities))
                    {
                        currentConflicts = storeConflicts;
                        currentState = storeState;

                        throw new Exception("Can't handle that there is no high level solution yet.");
                    }

                    solution.AddRange(solutionMoves);
                    solvedGoals.Add(goalToSolve);

                    level.AddPermanentWalll(goalToSolve.Pos);
                    removedEntities.Add(new Entity(solutionMoves.Last().ToHere,box.Color,box.Type));
                }
            }

            return (solution, solutionGraphs);
        }

        private static bool TrySolveSubProblem(Entity topLevelToMove, Entity topLevelGoal, Entity toMove, Point goal, bool toMoveIsAgent, Level level, List<BoxConflictGraph> solutionGraphs, ref BoxConflictGraph currentConflicts, ref State currentState, out List<HighlevelMove> solutionToSubProblem, HashSet<Point> freePath, HashSet<Entity> removedEntities)
        {
            solutionToSubProblem = new List<HighlevelMove>();
            Entity? agentToUse = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(currentConflicts, toMove);
            }
            List<HighlevelMove> solveConflictMoves;
            if (!TrySolveConflicts(topLevelToMove, topLevelGoal, toMove, goal, level, solutionGraphs, ref currentConflicts, ref currentState, out solveConflictMoves, freePath, agentToUse, removedEntities))
            {
                return false;
            }
            if (solveConflictMoves != null)
            {
                solutionToSubProblem.AddRange(solveConflictMoves);
            }


            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(currentConflicts, toMove);

                List<HighlevelMove> solveAgentConflictMoves;
                if (!TrySolveConflicts(topLevelToMove, topLevelGoal, agentToUse.Value, toMove.Pos, level, solutionGraphs, ref currentConflicts, ref currentState, out solveAgentConflictMoves, freePath, agentToUse, removedEntities))
                {
                    return false;
                }
                if (solveAgentConflictMoves != null)
                {
                    solutionToSubProblem.AddRange(solveAgentConflictMoves);
                }
            }

            currentState = currentState.GetCopy();
            for (int i = 0; i < currentState.Entities.Length; i++)
            {
                if (currentState.Entities[i] == toMove)
                {
                    currentState.Entities[i] = new Entity(goal, toMove.Color, toMove.Type);
                    break;
                }
            }

            if (topLevelToMove != toMove || topLevelGoal.Pos != goal)
            {
                currentConflicts = new BoxConflictGraph(currentState, level, topLevelGoal, removedEntities);
                currentConflicts.AddFreeNodes(level, topLevelToMove.Pos, topLevelGoal.Pos);
                solutionGraphs.Add(currentConflicts);
                //Console.WriteLine(level.StateToString(currentConflicts.CreatedFromThisState));
               // GraphShower.ShowSimplifiedGraph(currentConflicts);
            }

            solutionToSubProblem.Add(new HighlevelMove(currentState, toMove, goal, agentToUse));
            return true;
        }

        private static bool TrySolveConflicts(Entity topLevelToMove, Entity topLevelGoal, Entity toMove, Point goal, Level level, List<BoxConflictGraph> solutionGraphs, ref BoxConflictGraph currentConflicts, ref State currentState, out List<HighlevelMove> solutionToSubProblem, HashSet<Point> freePath, Entity? agentNotConflict, HashSet<Entity> removedEntities)
        {
            solutionToSubProblem = null;
            List<BoxConflictNode> conflicts = GetConflicts(toMove, goal, currentConflicts);
            if (conflicts != null)
            {
                solutionToSubProblem = new List<HighlevelMove>();
                Point[] path = Precomputer.GetPath(level, toMove.Pos, goal, false);
                freePath.UnionWith(path);
                do
                {
                    BoxConflictNode conflict = conflicts.First();
                    if (agentNotConflict.HasValue && conflict.Value.Ent == agentNotConflict.Value)
                    {
                        conflicts.Remove(conflict);
                        if (conflicts.Count == 0)
                        {
                            break;
                        }
                        continue;
                    }
                    Point freeSpace = GetFreeSpaceToMoveConflictTo(conflict.Value.Ent, currentConflicts, freePath);
                    if (TrySolveSubProblem(topLevelToMove, topLevelGoal, conflict.Value.Ent, freeSpace, conflict.Value.EntType == EntityType.AGENT, level, solutionGraphs, ref currentConflicts, ref currentState, out List<HighlevelMove> solutionMoves, freePath, removedEntities))
                    {
                        solutionToSubProblem.AddRange(solutionMoves);
                    }
                    else
                    {
                        //Do astar graph searching thing
                        throw new Exception("Can't handle that there is no high level solution yet.");
                    }
                    conflicts = GetConflicts(toMove, goal, currentConflicts);
                } while (conflicts != null && conflicts.Count > 0);         
                freePath.ExceptWith(path);
            }

            return true;
        }

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

        private static List<BoxConflictNode> GetConflicts(Entity toMove, Point goal, BoxConflictGraph currentConflicts)
        {
            INode startNode = currentConflicts.GetNodeFromPosition(toMove.Pos);
            foreach (var edgeNode in startNode.GetNodeEnds())
            {
                if (edgeNode is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == goal)
                {
                    return null;
                }
                else if (edgeNode is FreeSpaceNode freeNode && freeNode.Value.FreeSpaces.Contains(goal))
                {
                    return null;
                }
            }

            List<BoxConflictNode> conflicts = new List<BoxConflictNode>();
            HashSet<INode> exploredSet = new HashSet<INode>();
            Queue<INode> frontier = new Queue<INode>();
            Dictionary<INode, INode> childToParent = new Dictionary<INode, INode>();
            frontier.Enqueue(startNode);

            while (frontier.Count > 0)
            {
                INode leaf = frontier.Dequeue();

                bool isGoal = false;
                if (leaf is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == goal)
                {
                    isGoal = true;
                }
                else if (leaf is FreeSpaceNode freeNode && freeNode.Value.FreeSpaces.Contains(goal))
                {
                    isGoal = true;
                }

                if (isGoal)
                {
                    INode current = leaf;
                    while (childToParent.ContainsKey(current))
                    {
                        current = childToParent[current];
                        if (current is BoxConflictNode boxconNode)
                        {
                            conflicts.Add(boxconNode);
                        }
                        
                    }
                    //The last conflict is toMove itself which isn't a conflict
                    conflicts.RemoveAt(conflicts.Count - 1);

                    return conflicts;
                }

                foreach (var child in leaf.GetNodeEnds())
                {
                    if (!childToParent.ContainsKey(child) && !exploredSet.Contains(child))
                    {
                        frontier.Enqueue(child);
                        childToParent.Add(child, leaf);
                    }
                }
                exploredSet.Add(leaf);
            }

            throw new Exception("Found no path from  entity to goal.");
        }

        private static Point GetFreeSpaceToMoveConflictTo(Entity conflict, BoxConflictGraph currentConflicts, HashSet<Point> freePath)
        {
            foreach (var iNode in currentConflicts.Nodes)
            {
                if (iNode is FreeSpaceNode freeSpaceNode)
                {
                    var sdf = freeSpaceNode.Value.FreeSpaces.Except(freePath);
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
