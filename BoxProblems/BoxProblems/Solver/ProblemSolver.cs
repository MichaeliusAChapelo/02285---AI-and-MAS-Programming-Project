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
    public class HighlevelLevelSolution
    {
        public readonly List<HighlevelMove> SolutionMovesParts;
        public readonly List<BoxConflictGraph> SolutionGraphs;
        public readonly Level Level;

        public HighlevelLevelSolution(List<HighlevelMove> moves, List<BoxConflictGraph> graphs, Level level)
        {
            this.SolutionMovesParts = moves;
            this.SolutionGraphs = graphs;
            this.Level = level;
        }
    }

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

        public static List<HighlevelLevelSolution> SolveLevel(string levelPath, TimeSpan timeoutTime, bool parallelize)
        {
            if (!File.Exists(levelPath))
            {
                throw new Exception($"No level exists with the path: {levelPath}");
            }

            Level level = Level.ReadLevel(File.ReadAllLines(levelPath));

            return SolveLevel(level, timeoutTime, parallelize);
        }

        public static List<HighlevelLevelSolution> SolveLevel(Level level, TimeSpan timeoutTime, bool parallelize)
        {
            var solutionPieces = new ConcurrentBag<HighlevelLevelSolution>();
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

        private static List<List<INode>> GetGraphGroups(BoxConflictGraph graph, Point posToMakeWall)
        {
            HashSet<INode> exploredSet = new HashSet<INode>();
            Queue<INode> frontier = new Queue<INode>();

            int seenNodesCount = 0;

            int nodesToSee = graph.Nodes.Count;
            if (graph.Nodes.Any(x => x is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == posToMakeWall))
            {
                nodesToSee--;
            }

            List<List<INode>> graphGroups = new List<List<INode>>();
            while (seenNodesCount < nodesToSee)
            {
                List<INode> seenNodes = new List<INode>();

                INode startNode = graph.Nodes.First(x => !exploredSet.Contains(x) && (x is FreeSpaceNode || ((BoxConflictNode)x).Value.Ent.Pos != posToMakeWall));
                frontier.Enqueue(startNode);
                exploredSet.Add(startNode);

                while (frontier.Count > 0)
                {
                    INode leaf = frontier.Dequeue();

                    if (leaf is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == posToMakeWall)
                    {
                        continue;
                    }

                    seenNodes.Add(leaf);
                    seenNodesCount++;

                    foreach (var edgeEnd in leaf.GetNodeEnds())
                    {
                        if (!exploredSet.Contains(edgeEnd) && !(edgeEnd is BoxConflictNode boxNodeee && boxNodeee.Value.Ent.Pos == posToMakeWall))
                        {
                            frontier.Enqueue(edgeEnd);
                            exploredSet.Add(edgeEnd);
                        }
                    }
                }

                graphGroups.Add(seenNodes);
            }

            return graphGroups;
        }

        private static List<INode> GetMainGraphGroup(List<List<INode>> graphGroups)
        {
            var bestGroup = graphGroups.First();
            int bestGoalCount = int.MinValue;
            foreach (var group in graphGroups)
            {
                int goalsCount = 0;
                foreach (var node in group)
                {
                    if (node is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.GOAL)
                    {
                        goalsCount++;
                    }
                }
                if (goalsCount > bestGoalCount)
                {
                    bestGroup = group;
                    bestGoalCount = goalsCount;
                }
            }

            return bestGroup;
        }

        private static bool EveryGroupHasEverythingNeeded(List<List<INode>> graphGroups)
        {
            foreach (var group in graphGroups)
            {
                HashSet<char> agents = new HashSet<char>();
                Dictionary<char, int> boxes = new Dictionary<char, int>();
                Dictionary<char, int> goals = new Dictionary<char, int>();
                foreach (var iNode in group)
                {
                    BoxConflictNode boxNode = (BoxConflictNode)iNode;
                    char entityType = boxNode.Value.Ent.Type;
                    switch (boxNode.Value.EntType)
                    {
                        case EntityType.AGENT:
                            agents.Add(entityType);
                            break;
                        case EntityType.BOX:
                            if (!boxes.ContainsKey(entityType))
                            {
                                boxes.Add(entityType, 0);
                            }
                            boxes[entityType] += 1;
                            break;
                        case EntityType.GOAL:
                            if (!goals.ContainsKey(entityType))
                            {
                                goals.Add(entityType, 0);
                            }
                            goals[entityType] += 1;
                            break;
                        default:
                            throw new Exception("Unknown entity type.");
                    }
                }

                foreach (var goalInfo in goals)
                {
                    if (!boxes.TryGetValue(goalInfo.Key, out int boxCount) || boxCount < goalInfo.Value)
                    {
                        return false;
                    }
                    if (!agents.Contains(goalInfo.Key))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static HighlevelLevelSolution SolvePartialLevel(Level level, CancellationToken cancelToken)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph);
            //Console.WriteLine(priority);
            SolverData sData = new SolverData(level, cancelToken);
            HashSet<Entity> solvedGoals = new HashSet<Entity>();

            var goalPriorityLinkedLayers = priority.GetAsLinkedLayers();
            var currentLayerNode = goalPriorityLinkedLayers.First;
            while (currentLayerNode != null)
            {
                var currentLayer = currentLayerNode.Value;
                bool goToNextLayer = true;

                while (currentLayer.Goals.Count > 0)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    sData.CurrentConflicts = new BoxConflictGraph(sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddFreeSpaceNodes(level);
                    Entity goalToSolve = GetGoalToSolve(currentLayer.Goals, goalGraph, sData.CurrentConflicts);


                    sData.Level.AddPermanentWalll(goalToSolve.Pos);
                    sData.CurrentConflicts = new BoxConflictGraph(sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddGoalNodes(sData.Level);
                    sData.Level.RemovePermanentWall(goalToSolve.Pos);

                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);
                    var graphGroups = GetGraphGroups(sData.CurrentConflicts, goalToSolve.Pos);
                    if (graphGroups.Where(x => x.Any(y => y is BoxConflictNode)).Count() > 1 && !EveryGroupHasEverythingNeeded(graphGroups))
                    {
                        var mainGroup = GetMainGraphGroup(graphGroups);
                        List<Entity> goalsWithHigherPriority = new List<Entity>();
                        foreach (var group in graphGroups)
                        {
                            if (group != mainGroup)
                            {
                                foreach (var node in group)
                                {
                                    if (node is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.GOAL)
                                    {
                                        goalsWithHigherPriority.Add(boxNode.Value.Ent);
                                    }
                                }
                            }
                        }

                        if (goalsWithHigherPriority.Count == 0)
                        {
                            goalsWithHigherPriority.AddRange(currentLayer.Goals);
                            goalsWithHigherPriority.Remove(goalToSolve);
                            foreach (var higherGoal in goalsWithHigherPriority)
                            {
                                currentLayer.Goals.Remove(higherGoal);
                            }

                            if (goalsWithHigherPriority.Count == 0)
                            {
                                throw new Exception("level will be split by this action.");
                            }
                        }
                        else
                        {
                            foreach (var layer in goalPriorityLinkedLayers)
                            {
                                foreach (var goal in goalsWithHigherPriority)
                                {
                                    layer.Goals.Remove(goal);
                                }

                            }
                        }

                        goalPriorityLinkedLayers.AddBefore(currentLayerNode, new GoalPriorityLayer(goalsWithHigherPriority.ToHashSet()));
                        currentLayerNode = currentLayerNode.Previous;
                        goToNextLayer = false;
                        break;
                    }

                    sData.CurrentConflicts.RemoveGoalNodes();
                    sData.CurrentConflicts.AddFreeSpaceNodes(level);
                    //PrintLatestStateDiff(level, sData.SolutionGraphs);
                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);

                    Entity box = GetBoxToSolveProblem(sData.CurrentConflicts, goalToSolve);
                    int boxIndex = sData.GetEntityIndex(box);

                    List<HighlevelMove> boxOnGoalSolution = null;
                    if (sData.CurrentConflicts.PositionHasNode(goalToSolve.Pos))
                    {
                        INode nodeOnGoal = sData.CurrentConflicts.GetNodeFromPosition(goalToSolve.Pos);
                        if (nodeOnGoal is BoxConflictNode boxOnGoal && boxOnGoal.Value.EntType != EntityType.GOAL)
                        {
                            int boxOnGoalIndex = sData.GetEntityIndex(boxOnGoal.Value.Ent);
                            Point freeSpace = GetFreeSpaceToMoveConflictTo(goalToSolve, sData, sData.FreePath);
                            sData.AddToFreePath(freeSpace);
                            if (!TrySolveSubProblem(boxOnGoalIndex, freeSpace, boxOnGoal.Value.EntType == EntityType.AGENT, out boxOnGoalSolution, sData, 0))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solution.AddRange(boxOnGoalSolution);
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
                    currentLayer.Goals.Remove(goalToSolve);

                    Debug.Assert(sData.FreePath.Count == 0, "Expecting FreePath to be empty after each problem has been solved.");
                    Debug.Assert(sData.SolutionGraphs.Count == solution.Count, "asda");
                }

                if (goToNextLayer)
                {
                    currentLayerNode = currentLayerNode.Next;
                }
            }

            //var sortedSolution = solution.Zip(sData.SolutionGraphs, (move, graph) => (move, graph)).OrderBy(x => x.move.MoveNumber);
            //solution = sortedSolution.Select(x => x.move).ToList();
            //sData.SolutionGraphs = sortedSolution.Select(x => x.graph).ToList();

            //for (int z = 0; z < sData.SolutionGraphs.Count; z++)
            //{
            //    PrintLatestStateDiff(level, sData.SolutionGraphs, z);
            //}

            foreach (var goal in level.Goals)
            {
                level.RemovePermanentWall(goal.Pos);
                level.RemoveWall(goal.Pos);
            }

            return new HighlevelLevelSolution(solution, sData.SolutionGraphs, level);
        }

        private static bool TrySolveSubProblem(int toMoveIndex, Point goal, bool toMoveIsAgent, out List<HighlevelMove> solutionToSubProblem, SolverData sData, int depth)
        {
            if (depth == 100)
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
                //solutionToSubProblem.InsertRange(0, solveConflictMoves);
                solutionToSubProblem.AddRange(solveConflictMoves);
            }

            int? agentIndex = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(sData.CurrentConflicts, toMove);
                agentIndex = sData.GetEntityIndex(agentToUse.Value);

                sData.AddToFreePath(toMovePath);
                List<HighlevelMove> solveAgentConflictMoves;
                if (!TrySolveConflicts(agentIndex.Value, toMove.Pos, out solveAgentConflictMoves, out _, sData, agentToUse, depth))
                {
                    return false;
                }
                toMove = sData.GetEntity(toMoveIndex);
                if (solveAgentConflictMoves != null)
                {
                    //solutionToSubProblem.InsertRange(0, solveAgentConflictMoves);
                    solutionToSubProblem.AddRange(solveAgentConflictMoves);
                }
                sData.RemoveFromFreePath(toMovePath);
            }

            sData.CurrentState = sData.CurrentState.GetCopy();
            sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);

            sData.CurrentConflicts = new BoxConflictGraph(sData.CurrentState, sData.Level, sData.RemovedEntities);
            sData.CurrentConflicts.AddFreeSpaceNodes(sData.Level);
            sData.SolutionGraphs.Add(sData.CurrentConflicts);
            solutionToSubProblem.Add(new HighlevelMove(sData.CurrentState, toMoveIndex, goal, agentIndex, counter));
            //Console.WriteLine(solutionToSubProblem.Last());
            return true;
        }

        private static bool TrySolveConflicts(int toMoveIndex, Point goal, out List<HighlevelMove> solutionToSubProblem, out Point[] toMovePath, SolverData sData, Entity? agentNotConflict, int depth)
        {
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
                        //solutionToSubProblem.InsertRange(0, solutionMoves);
                        solutionToSubProblem.AddRange(solutionMoves);
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
            if (index == -1)
            {
                Console.WriteLine(level.ToString());
            }
            else if (index == 0)
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