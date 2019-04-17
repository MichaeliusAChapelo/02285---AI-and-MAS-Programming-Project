using BoxProblems.Graphing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxProblems
{
    public class HighlevelMove
    {
        internal State CurrentState;
        internal Entity MoveThis;
        internal Point ToHere;
        internal Entity? UsingThisAgent;

        internal HighlevelMove(State state, Entity moveThis, Point toHere, Entity? usingThisAgent)
        {
            this.CurrentState = state;
            this.MoveThis = moveThis;
            this.ToHere = toHere;
            this.UsingThisAgent = usingThisAgent;
        }
    }

    public enum SolverStatus
    {
        ERROR,
        TIMEOUT,
        SUCCESS
    }

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

    public static class ProblemSolver
    {
        public static SolveStatistic GetSolveStatistics(string levelPath, TimeSpan timeoutTime, bool parallelize = false)
        {
            if (!File.Exists(levelPath))
            {
                throw new Exception($"No level exists with the path: {levelPath}");
            }

            Level level = Level.ReadLevel(File.ReadAllLines(levelPath));

            Exception error = null;
            SolverStatus status = SolverStatus.ERROR;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            try
            {
                SolveLevel(level, timeoutTime, parallelize);
                status = SolverStatus.SUCCESS;
            }
            catch (Exception e)
            {
                error = e;
                if (e is OperationCanceledException)
                {
                    status = SolverStatus.TIMEOUT;
                }
            }

            timer.Stop();

            return new SolveStatistic(timer.ElapsedMilliseconds, error, status, Path.GetFileNameWithoutExtension(levelPath));
        }

        public static List<(List<HighlevelMove> solutionMovesParts, List<BoxConflictGraph> solutionGraphs)> SolveLevel(string levelPath, TimeSpan timeoutTime, bool parallelize)
        {
            if (!File.Exists(levelPath))
            {
                throw new Exception($"No level exists with the path: {levelPath}");
            }

            Level level = Level.ReadLevel(File.ReadAllLines(levelPath));

            return SolveLevel(level, timeoutTime, parallelize);
        }

        internal static List<(List<HighlevelMove> solutionMovesParts, List<BoxConflictGraph> solutionGraphs)> SolveLevel(Level level, TimeSpan timeoutTime, bool parallelize)
        {
            var solutionPieces = new ConcurrentBag<(List<HighlevelMove>, List<BoxConflictGraph>)>();
            List<Level> levels = LevelSplitter.SplitLevel(level);

            using (CancellationTokenSource cancelSource = new CancellationTokenSource(timeoutTime))
            {
                if (parallelize)
                {
                    Parallel.ForEach(levels, x =>
                    {
                        solutionPieces.Add(SolvePartialLevel(x, cancelSource.Token));
                    });
                }
                else
                {
                    foreach (var x in levels)
                    {
                        solutionPieces.Add(SolvePartialLevel(x, cancelSource.Token));
                    }
                }
            }


            return solutionPieces.ToList();
        }

        private static (List<HighlevelMove> solutionMoves, List<BoxConflictGraph> solutionGraphs) SolvePartialLevel(Level level, CancellationToken cancelToken)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            List<BoxConflictGraph> solutionGraphs = new List<BoxConflictGraph>();

            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph);
            HashSet<Entity> removedEntities = new HashSet<Entity>();
            BoxConflictGraph currentConflicts;
            State currentState = level.InitialState;
            HashSet<Point> freePath = new HashSet<Point>();
            HashSet<Entity> solvedGoals = new HashSet<Entity>();
            foreach (var goalPriorityLayer in priority.PriorityLayers)
            {
                for (int i = 0; i < goalPriorityLayer.Length; i++)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    currentConflicts = new BoxConflictGraph(currentState, level, removedEntities);
                    currentConflicts.AddFreeNodes(level);
                    solutionGraphs.Add(currentConflicts);
                    PrintLatestStateDiff(level, solutionGraphs);
                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(currentConflicts);

                    Entity goalToSolve = GetGoalToSolve(goalPriorityLayer, goalGraph, currentConflicts, solvedGoals);
                    Entity box = GetBoxToSolveProblem(currentConflicts, goalToSolve);

                    if (currentConflicts.PositionHasNode(goalToSolve.Pos))
                    {
                        INode nodeongoal = currentConflicts.GetNodeFromPosition(goalToSolve.Pos);
                        if (nodeongoal is BoxConflictNode boxongoal && boxongoal.Value.EntType != EntityType.GOAL)
                        {
                            Point freespace = GetFreeSpaceToMoveConflictTo(goalToSolve, currentConflicts, freePath);
                            freePath.Add(freespace);
                            List<HighlevelMove> boxongoalSolution;
                            if (!TrySolveSubProblem(boxongoal.Value.Ent, freespace, boxongoal.Value.EntType == EntityType.AGENT, level, solutionGraphs, ref currentConflicts, ref currentState, out boxongoalSolution, freePath, removedEntities, 0, cancelToken))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solution.AddRange(boxongoalSolution);
                            freePath.Clear();
                        }
                    }

                    var storeConflicts = currentConflicts;
                    var storeState = currentState;
                    List<HighlevelMove> solutionMoves;
                    if (!TrySolveSubProblem(box, goalToSolve.Pos, false, level, solutionGraphs, ref currentConflicts, ref currentState, out solutionMoves, freePath, removedEntities, 0, cancelToken))
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

        private static bool TrySolveSubProblem(Entity toMove, Point goal, bool toMoveIsAgent, Level level, List<BoxConflictGraph> solutionGraphs, ref BoxConflictGraph currentConflicts, ref State currentState, out List<HighlevelMove> solutionToSubProblem, HashSet<Point> freePath, HashSet<Entity> removedEntities, int depth, CancellationToken cancelToken)
        {
            if (depth == 20)
            {
                throw new Exception("sub problem depth limit reached.");
            }

            solutionToSubProblem = new List<HighlevelMove>();
            Entity? agentToUse = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(currentConflicts, toMove);
            }
            List<HighlevelMove> solveConflictMoves;
            if (!TrySolveConflicts(toMove, goal, level, solutionGraphs, ref currentConflicts, ref currentState, out solveConflictMoves, freePath, agentToUse, removedEntities, depth, cancelToken))
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
                if (!TrySolveConflicts(agentToUse.Value, toMove.Pos, level, solutionGraphs, ref currentConflicts, ref currentState, out solveAgentConflictMoves, freePath, agentToUse, removedEntities, depth, cancelToken))
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
                    currentState.Entities[i] = currentState.Entities[i].Move(goal);
                    break;
                }
            }

            currentConflicts = new BoxConflictGraph(currentState, level, removedEntities);
            currentConflicts.AddFreeNodes(level);
            solutionGraphs.Add(currentConflicts);
            //PrintLatestStateDiff(level, solutionGraphs);
            //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(currentConflicts);

            solutionToSubProblem.Add(new HighlevelMove(currentState, toMove, goal, agentToUse));
            return true;
        }

        private static bool TrySolveConflicts(Entity toMove, Point goal, Level level, List<BoxConflictGraph> solutionGraphs, ref BoxConflictGraph currentConflicts, ref State currentState, out List<HighlevelMove> solutionToSubProblem, HashSet<Point> freePath, Entity? agentNotConflict, HashSet<Entity> removedEntities, int depth, CancellationToken cancelToken)
        {
            solutionToSubProblem = null;
            List<BoxConflictNode> conflicts = GetConflicts(toMove, goal, currentConflicts);
            if (conflicts != null)
            {
                solutionToSubProblem = new List<HighlevelMove>();
                HashSet<Point> pathAlsoFree = new HashSet<Point>(freePath);

                //The path needs to go through the same entitites as the conflicts
                //list says it does but the precosnputer may not return the same
                //path as it doesn't care if it goes through more entitites
                //to get to the goal. So the solution is to mark all entities,
                //except the conflicting ones, as walls so the precomputer
                //can't find an alternative path through other entitites.

                for (int i = 0; i < currentState.Entities.Length; i++)
                {
                    level.AddWall(currentState.Entities[i].Pos);
                }

                level.RemoveWall(toMove.Pos);
                level.RemoveWall(goal);
                for (int i = 0; i < conflicts.Count; i++)
                {
                    level.RemoveWall(conflicts[i].Value.Ent.Pos);
                }

                Point[] path = Precomputer.GetPath(level, toMove.Pos, goal, false);
                pathAlsoFree.UnionWith(path);

                level.ResetWalls();
                do
                {
                    cancelToken.ThrowIfCancellationRequested();

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
                    Point freeSpace = GetFreeSpaceToMoveConflictTo(conflict.Value.Ent, currentConflicts, pathAlsoFree);
                    pathAlsoFree.Add(freeSpace);
                    if (TrySolveSubProblem(conflict.Value.Ent, freeSpace, conflict.Value.EntType == EntityType.AGENT, level, solutionGraphs, ref currentConflicts, ref currentState, out List<HighlevelMove> solutionMoves, pathAlsoFree, removedEntities, depth + 1, cancelToken))
                    {
                        solutionToSubProblem.AddRange(solutionMoves);
                    }
                    else
                    {
                        //Do astar graph searching thing
                        throw new Exception("Can't handle that there is no high level solution yet.");
                    }
                    if (!currentState.Entities.Contains(toMove))
                    {
                        throw new Exception("toMove moved.");
                    }

                    pathAlsoFree.Remove(freeSpace);
                    conflicts = GetConflicts(toMove, goal, currentConflicts);
                } while (conflicts != null && conflicts.Count > 0);
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
                    //conflicts.RemoveAt(conflicts.Count - 1);
                    conflicts.RemoveAll(x => x.Value.Ent == toMove);

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

        private static void PrintLatestStateDiff(Level level, List<BoxConflictGraph> graphs)
        {
            if (graphs.Count == 1)
            {
                Console.WriteLine(level.StateToString(graphs.First().CreatedFromThisState));
            }
            else
            {
                State last = graphs.Last().CreatedFromThisState;
                State sLast = graphs[graphs.Count - 2].CreatedFromThisState;

                string[] lastStateStrings = level.StateToString(last).Split(Environment.NewLine);
                string[] sLastStateStrings = level.StateToString(sLast).Split(Environment.NewLine);

                for (int y = 0; y < lastStateStrings.Length; y++)
                {
                    for (int x = 0; x < lastStateStrings[y].Length; x++)
                    {
                        if (lastStateStrings[y][x] != sLastStateStrings[y][x])
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                        }
                        else
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                        }
                        Console.Write(lastStateStrings[y][x]);
                    }
                    Console.WriteLine();
                }
            }

            Console.ReadLine();
        }
    }
}
