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

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
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

        private static LevelGroupsInfo GetLevelGroups(SolverData sData, Entity goalToMakeWall)
        {
            sData.Level.AddWall(goalToMakeWall.Pos);

            HashSet<Point> entityPositions = new HashSet<Point>();
            foreach (Entity entity in sData.CurrentState.Entities)
            {
                entityPositions.Add(entity.Pos);
            }

            HashSet<Point> goalPositions = new HashSet<Point>();
            foreach (Entity goal in sData.Level.Goals)
            {
                goalPositions.Add(goal.Pos);
            }

            var goalCondition = new Func<Point, GraphSearcher.GoalFound<Point>>(x =>
            {
                return new GraphSearcher.GoalFound<Point>(x, !sData.Level.Walls[x.X, x.Y]);
            });

            LevelGroupsInfo groupsInfo = new LevelGroupsInfo(false);
            HashSet<Point> alreadySeen = new HashSet<Point>();
            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    if (!sData.Level.Walls[x, y] && !alreadySeen.Contains(new Point(x, y)))
                    {
                        List<Point> foundSpaces = GraphSearcher.GetReachedGoalsBFS(sData.Level, new Point(x, y), goalCondition);
                        alreadySeen.UnionWith(foundSpaces);

                        List<Entity> foundEntities = new List<Entity>();
                        foreach (var foundSpace in foundSpaces)
                        {
                            if (entityPositions.Contains(foundSpace))
                            {
                                Entity entity = sData.GetEntityAtPos(foundSpace).Value;
                                if (!sData.RemovedEntities.Contains(entity))
                                {
                                    foundEntities.Add(entity);
                                }
                            }
                            else if (goalPositions.Contains(foundSpace))
                            {
                                Entity entity = sData.GetGoalEntityAtPos(foundSpace).Value;
                                foundEntities.Add(entity);
                            }
                        }

                        if (foundEntities.Count > 0)
                        {
                            groupsInfo.AddGroup(new LevelGroup(foundSpaces, foundEntities));
                        }
                    }
                }
            }

            return groupsInfo;
        }

        private static (List<HighlevelMove> solutionMoves, List<BoxConflictGraph> solutionGraphs) SolvePartialLevel(Level level, CancellationToken cancelToken)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph);
            //Console.WriteLine(priority);
            SolverData sData = new SolverData(level, cancelToken);
            HashSet<Entity> solvedGoals = new HashSet<Entity>();
            foreach (var goalPriorityLayer in priority.PriorityLayers)
            {
                int goalsFinished = 0;
                while (goalsFinished < goalPriorityLayer.Length)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    sData.CurrentConflicts = new BoxConflictGraph(sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddFreeSpaceNodes(level);
                    //sData.SolutionGraphs.Add(sData.CurrentConflicts);
                    //PrintLatestStateDiff(level, sData.SolutionGraphs);
                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(currentConflicts);

                    Entity goalToSolve = GetGoalToSolve(goalPriorityLayer, goalGraph, sData.CurrentConflicts, solvedGoals);
                    LevelGroupsInfo groups = GetLevelGroups(sData, goalToSolve);
                    //if (groups.Count > 1)
                    //{
                    //    //Console.WriteLine(priority.ToLevelString(sData.Level));
                    //    throw new Exception("level will be split by this action.");
                    //}


                    Entity box = GetBoxToSolveProblem(sData.CurrentConflicts, goalToSolve);
                    int boxIndex = sData.GetEntityIndex(box);

                    if (sData.CurrentConflicts.PositionHasNode(goalToSolve.Pos))
                    {
                        INode nodeOnGoal = sData.CurrentConflicts.GetNodeFromPosition(goalToSolve.Pos);
                        if (nodeOnGoal is BoxConflictNode boxOnGoal && boxOnGoal.Value.EntType != EntityType.GOAL)
                        {
                            int boxOnGoalIndex = sData.GetEntityIndex(boxOnGoal.Value.Ent);
                            Point freeSpace = GetFreeSpaceToMoveConflictTo(goalToSolve, sData, sData.FreePath);
                            sData.AddToFreePath(freeSpace);
                            List<HighlevelMove> boxongoalSolution;
                            if (!TrySolveSubProblem(boxOnGoalIndex, freeSpace, boxOnGoal.Value.EntType == EntityType.AGENT, out boxongoalSolution, sData, 0))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solution.AddRange(boxongoalSolution);
                            sData.FreePath.Clear();
                        }
                    }

                    var storeConflicts = sData.CurrentConflicts;
                    var storeState = sData.CurrentState;
                    List<HighlevelMove> solutionMoves;
                    if (!TrySolveSubProblem(boxIndex, goalToSolve.Pos, false, out solutionMoves, sData, 0))
                    {
                        sData.CurrentConflicts = storeConflicts;
                        sData.CurrentState = storeState;

                        throw new Exception("Can't handle that there is no high level solution yet.");
                    }

                    solution.AddRange(solutionMoves);
                    solvedGoals.Add(goalToSolve);

                    level.AddPermanentWalll(goalToSolve.Pos);
                    sData.RemovedEntities.Add(new Entity(solutionMoves.Last().ToHere, box.Color, box.Type));

                    Debug.Assert(sData.FreePath.Count == 0, "Expecting FreePath to be empty after each problem has been solved.");
                    Debug.Assert(sData.SolutionGraphs.Count == solution.Count, "asda");

                    goalsFinished++;
                }
            }

            //var sortedSolution = solution.Zip(sData.SolutionGraphs, (move, graph) => (move, graph)).OrderBy(x => x.move.MoveNumber);
            //solution = sortedSolution.Select(x => x.move).ToList();
            //sData.SolutionGraphs = sortedSolution.Select(x => x.graph).ToList();
            for (int z = 0; z < sData.SolutionGraphs.Count; z++)
            {
                PrintLatestStateDiff(level, sData.SolutionGraphs, z);
            }

            return (solution, sData.SolutionGraphs);
        }

        private static bool TrySolveSubProblem(int toMoveIndex, Point goal, bool toMoveIsAgent, out List<HighlevelMove> solutionToSubProblem, SolverData sData, int depth)
        {
            if (depth == 30)
            {
                throw new Exception("sub problem depth limit reached.");
            }
            int counter = sData.Counter++;

            Entity toMove = sData.GetEntity(toMoveIndex);
            solutionToSubProblem = new List<HighlevelMove>();
            Entity? agentToUse = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(sData.CurrentConflicts, toMove);
            }
            List<HighlevelMove> solveConflictMoves;
            Point[] toMovePath;
            if (!TrySolveConflicts(toMoveIndex, goal, out solveConflictMoves, out toMovePath, sData, agentToUse, depth))
            {
                return false;
            }
            toMove = sData.GetEntity(toMoveIndex);
            if (solveConflictMoves != null)
            {
                solutionToSubProblem.AddRange(solveConflictMoves);
            }

            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(sData.CurrentConflicts, toMove);
                int agentIndex = sData.GetEntityIndex(agentToUse.Value);

                sData.AddToFreePath(toMovePath);
                List<HighlevelMove> solveAgentConflictMoves;
                if (!TrySolveConflicts(agentIndex, toMove.Pos, out solveAgentConflictMoves, out _, sData, agentToUse, depth))
                {
                    return false;
                }
                toMove = sData.GetEntity(toMoveIndex);
                if (solveAgentConflictMoves != null)
                {
                    solutionToSubProblem.AddRange(solveAgentConflictMoves);
                }
                sData.RemoveFromFreePath(toMovePath);
            }

            sData.CurrentState = sData.CurrentState.GetCopy();
            sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);

            sData.CurrentConflicts = new BoxConflictGraph(sData.CurrentState, sData.Level, sData.RemovedEntities);
            sData.CurrentConflicts.AddFreeSpaceNodes(sData.Level);
            sData.SolutionGraphs.Add(sData.CurrentConflicts);
            //PrintLatestStateDiff(sData.Level, sData.SolutionGraphs);
            //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(currentConflicts);

            solutionToSubProblem.Add(new HighlevelMove(sData.CurrentState, toMove, goal, agentToUse, counter));
            return true;
        }

        private static bool TrySolveConflicts(int toMoveIndex, Point goal, out List<HighlevelMove> solutionToSubProblem, out Point[] toMovePath, SolverData sData, Entity? agentNotConflict, int depth)
        {
            solutionToSubProblem = null;

#if DEBUG
            Dictionary<Point, int> freePathCopy = new Dictionary<Point, int>(sData.FreePath);
#endif

            solutionToSubProblem = new List<HighlevelMove>();
            while (true)
            {
                Entity toMove = sData.GetEntity(toMoveIndex);
                List<BoxConflictNode> conflicts = GetConflicts(toMove, goal, sData.CurrentConflicts);

                //The path needs to go through the same entitites as the conflicts
                //list says it does but the precosnputer may not return the same
                //path as it doesn't care if it goes through more entitites
                //to get to the goal. So the solution is to mark all entities,
                //except the conflicting ones, as walls so the precomputer
                //can't find an alternative path through other entitites.

                toMovePath = GetPathThroughConflicts(goal, sData, toMove, conflicts);

                if (conflicts == null)
                {
                    break;
                }

                sData.AddToFreePath(toMovePath);

                bool toMoveMoved = false;
                do
                {
                    sData.CancelToken.ThrowIfCancellationRequested();

                    BoxConflictNode conflict = conflicts.First();
                    if (agentNotConflict.HasValue && conflict.Value.Ent == agentNotConflict.Value)
                    {
                        conflicts.Remove(conflict);
                        continue;
                    }

                    Point freeSpace = GetFreeSpaceToMoveConflictTo(conflict.Value.Ent, sData, sData.FreePath);
                    sData.AddToFreePath(freeSpace);

                    //Console.WriteLine($"Conflict: {conflict.ToString()} -> {freeSpace}");
                    if (TrySolveSubProblem(sData.GetEntityIndex(conflict.Value.Ent), freeSpace, conflict.Value.EntType == EntityType.AGENT, out List<HighlevelMove> solutionMoves, sData, depth + 1))
                    {
                        solutionToSubProblem.InsertRange(0, solutionMoves);
                        //solutionToSubProblem.AddRange(solutionMoves);
                    }
                    else
                    {
                        //Do astar graph searching thing
                        throw new Exception("Can't handle that there is no high level solution yet.");
                    }

                    sData.RemoveFromFreePath(freeSpace);
                    if (sData.GetEntity(toMoveIndex) != toMove)
                    {
                        toMoveMoved = true;
                        break;
                    }

                    conflicts = GetConflicts(toMove, goal, sData.CurrentConflicts);
                } while (conflicts != null && conflicts.Count > 0);

                sData.RemoveFromFreePath(toMovePath);
                if (!toMoveMoved)
                {
                    break;
                }
            }
            //if (solutionToSubProblem.Count > 0)
            //{

            //}
#if DEBUG
            Debug.Assert(sData.FreePath.Except(freePathCopy).Count() == 0, "Expected the end result to be the same as when this method started");
#endif

            return true;
        }

        private static Point[] GetPathThroughConflicts(Point goal, SolverData sData, Entity toMove, List<BoxConflictNode> conflicts)
        {
            Point[] toMovePath;
            for (int i = 0; i < sData.CurrentState.Entities.Length; i++)
            {
                sData.Level.AddWall(sData.CurrentState.Entities[i].Pos);
            }

            sData.Level.RemoveWall(toMove.Pos);
            sData.Level.RemoveWall(goal);
            if (conflicts != null)
            {
                for (int i = 0; i < conflicts.Count; i++)
                {
                    sData.Level.RemoveWall(conflicts[i].Value.Ent.Pos);
                }
            }

            toMovePath = Precomputer.GetPath(sData.Level, toMove.Pos, goal, false);
            sData.Level.ResetWalls();
            return toMovePath;
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

                    //toMove itself can't be a conflict to itself
                    conflicts.RemoveAll(x => x.Value.Ent == toMove);

                    return conflicts.Count == 0 ? null : conflicts;
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

        private static void PrintLatestStateDiff(Level level, List<BoxConflictGraph> graphs)
        {
            PrintLatestStateDiff(level, graphs, graphs.Count - 1);
        }

        private static void PrintLatestStateDiff(Level level, List<BoxConflictGraph> graphs, int index)
        {
            if (index == 0)
            {
                Console.WriteLine(level.StateToString(graphs[index].CreatedFromThisState));
            }
            else
            {
                State last = graphs[index].CreatedFromThisState;
                State sLast = graphs[index - 1].CreatedFromThisState;

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