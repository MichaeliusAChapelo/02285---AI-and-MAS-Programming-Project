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
    internal class GroupInformation
    {
        public readonly Dictionary<int, int> agentColors;
        public readonly Dictionary<char, int> boxeTypes;
        public readonly Dictionary<(int color, char type), int> goalTypeAndColor;
        internal GroupInformation(List<INode> group)
        {
            this.agentColors = new Dictionary<int, int>();
            this.boxeTypes = new Dictionary<char, int>();
            this.goalTypeAndColor = new Dictionary<(int, char), int>();
            foreach (var iNode in group)
            {
                if (iNode is FreeSpaceNode)
                {
                    continue;
                }
                BoxConflictNode boxNode = (BoxConflictNode)iNode;
                char entityType = boxNode.Value.Ent.Type;
                int entityColor = boxNode.Value.Ent.Color;
                switch (boxNode.Value.EntType)
                {
                    case EntityType.AGENT:
                        if (!agentColors.ContainsKey(entityColor))
                        {
                            agentColors.Add(entityColor, 0);
                        }
                        agentColors[entityColor] += 1;
                        break;
                    case EntityType.BOX:
                        if (!boxeTypes.ContainsKey(entityType))
                        {
                            boxeTypes.Add(entityType, 0);
                        }
                        boxeTypes[entityType] += 1;
                        break;
                    case EntityType.GOAL:
                        var goalKey = (entityColor, entityType);
                        if (!goalTypeAndColor.ContainsKey(goalKey))
                        {
                            goalTypeAndColor.Add(goalKey, 0);
                        }
                        goalTypeAndColor[goalKey] += 1;
                        break;
                    default:
                        throw new Exception("Unknown entity type.");
                }
            }
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

            List<HighlevelLevelSolution> solution = null;
            try
            {
                solution = SolveLevel(level, timeoutTime, parallelize);
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

            return new SolveStatistic(timer.ElapsedMilliseconds, error, status, Path.GetFileNameWithoutExtension(levelPath), solution, level);
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
                        var solution = SolvePartialLevel(x, cancelSource.Token);
                        solution = HighLevelOptimizer.Optimize(solution);
                        //WriteToFile(optimizedSolution);
                        solutionPieces.Add(solution);
                    });
                }
                else
                {
                    foreach (var x in levels)
                    {
                        var solution = SolvePartialLevel(x, cancelSource.Token);
                        solution = HighLevelOptimizer.Optimize(solution);
                        //WriteToFile(optimizedSolution);
                        solutionPieces.Add(solution);
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
            if (bestGoalCount == 0)
            {
                int bestFreeSpaceCount = int.MinValue;
                foreach (var group in graphGroups)
                {
                    int freeSpaceCount = 0;
                    foreach (var node in group)
                    {
                        if (node is FreeSpaceNode freeSpaceNode)
                        {
                            freeSpaceCount += freeSpaceNode.Value.FreeSpaces.Count;
                        }
                    }
                    if (freeSpaceCount > bestFreeSpaceCount)
                    {
                        bestGroup = group;
                        bestFreeSpaceCount = freeSpaceCount;
                    }
                }

            }

            return bestGroup;
        }



        private static bool EveryGroupHasEverythingNeeded(List<List<INode>> graphGroups, List<INode> mainGroup, Entity goalEntity)
        {
            foreach (var group in graphGroups)
            {
                HashSet<int> agentColors = new HashSet<int>();
                Dictionary<char, int> boxeTypes = new Dictionary<char, int>();
                Dictionary<(int color, char type), int> goalTypeAndColor = new Dictionary<(int, char), int>();
                foreach (var iNode in group)
                {
                    if (iNode is FreeSpaceNode)
                    {
                        continue;
                    }
                    BoxConflictNode boxNode = (BoxConflictNode)iNode;
                    char entityType = boxNode.Value.Ent.Type;
                    int entityColor = boxNode.Value.Ent.Color;
                    switch (boxNode.Value.EntType)
                    {
                        case EntityType.AGENT:
                            agentColors.Add(entityColor);
                            break;
                        case EntityType.BOX:
                            if (!boxeTypes.ContainsKey(entityType))
                            {
                                boxeTypes.Add(entityType, 0);
                            }
                            boxeTypes[entityType] += 1;
                            break;
                        case EntityType.GOAL:
                            var goalKey = (entityColor, entityType);
                            if (!goalTypeAndColor.ContainsKey(goalKey))
                            {
                                goalTypeAndColor.Add(goalKey, 0);
                            }
                            goalTypeAndColor[goalKey] += 1;
                            break;
                        default:
                            throw new Exception("Unknown entity type.");
                    }
                }

                if (group == mainGroup)
                {
                    if (boxeTypes.ContainsKey(goalEntity.Type))
                    {
                        boxeTypes[goalEntity.Type] += 1;
                    }
                    else
                    {
                        return false;
                    }
                }
                var goalTuple = (goalEntity.Color, goalEntity.Type);
                goalTypeAndColor.TryAdd(goalTuple, 0);
                goalTypeAndColor[goalTuple] += 1;
                foreach (var goalInfo in goalTypeAndColor)
                {
                    if (!boxeTypes.TryGetValue(goalInfo.Key.type, out int boxCount) || boxCount < goalInfo.Value)
                    {
                        return false;
                    }
                    if (!agentColors.TryGetValue(goalInfo.Key.color, out int agentCount))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<Point> GetSpacesInGraphGroup(GraphSearchData gsData, List<INode> graphGroup, Level level)
        {
            var foundFreeSpace = new Func<(Point pos, int distance), GraphSearcher.GoalFound<Point>>(x =>
            {
                return new GraphSearcher.GoalFound<Point>(x.pos, !level.IsWall(x.pos));
            });
            Point start;
            INode firstNode = graphGroup.First();
            if (firstNode is BoxConflictNode boxNode)
            {
                start = boxNode.Value.Ent.Pos;
            }
            else
            {
                start = ((FreeSpaceNode)firstNode).Value.FreeSpaces.First();
            }
            return GraphSearcher.GetReachedGoalsBFS(gsData, level, start, foundFreeSpace);
        }

        private static HighlevelLevelSolution SolvePartialLevel(Level level, CancellationToken cancelToken)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            SolverData sData = new SolverData(level, cancelToken);
            GoalGraph goalGraph = new GoalGraph(sData.gsData, level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph);
            //Console.WriteLine(priority);
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

                    sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, level);
                    Entity goalToSolve = GetGoalToSolve(currentLayer.Goals, goalGraph, sData);


                    sData.Level.AddPermanentWalll(goalToSolve.Pos);
                    sData.Level.AddWall(goalToSolve.Pos);
                    sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddGoalNodes(sData.gsData, sData.Level, goalToSolve);
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
                    sData.Level.RemovePermanentWall(goalToSolve.Pos);
                    sData.Level.RemoveWall(goalToSolve.Pos);

                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);
                    //LevelVisualizer.PrintLatestStateDiff(sData.Level, sData.SolutionGraphs);
                    var graphGroups = GetGraphGroups(sData.CurrentConflicts, goalToSolve.Pos);
                    var mainGroup = GetMainGraphGroup(graphGroups);
                    if (graphGroups.Where(x => x.Any(y => y is BoxConflictNode)).Count() > 1 && 
                        !EveryGroupHasEverythingNeeded(graphGroups, mainGroup, goalToSolve) &&
                        mainGroup.Any(x => x is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.GOAL))
                    {
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

                                sData.Level.AddWall(goalToSolve.Pos);
                                foreach (var group in graphGroups)
                                {
                                    if (group != mainGroup)
                                    {
                                        List<Point> freeSpacesInGroup = GetSpacesInGraphGroup(sData.gsData, group, sData.Level);
                                        foreach (var space in freeSpacesInGroup)
                                        {
                                            sData.FreePath.TryAdd(space, 0);
                                            sData.FreePath[space] += 1;
                                        }
                                    }
                                }
                                sData.FreePath.Add(goalToSolve.Pos, 1);
                                sData.Level.RemoveWall(goalToSolve.Pos);
                                sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                                sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
                                var newGraphGroups = GetGraphGroups(sData.CurrentConflicts, new Point(-1, -1));
                                var newMainGroup = GetMainGraphGroup(newGraphGroups);
                                bool ignoreGroup = true;
                                foreach (var group in graphGroups)
                                {
                                    if (group != mainGroup)
                                    {
                                        ignoreGroup = true;
                                        if (group.First() is BoxConflictNode boxnode)
                                        {
                                            foreach (var newNode in newMainGroup)
                                            {
                                                if (newNode is BoxConflictNode newBoxNode && newBoxNode.Value.Ent == boxnode.Value.Ent)
                                                {
                                                    ignoreGroup = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if (group.First() is FreeSpaceNode freeNode)
                                        {
                                            Point freeSpacePoint = freeNode.Value.FreeSpaces.First();
                                            int freeSpaceCount = 0;
                                            foreach (var point in freeNode.Value.FreeSpaces)
                                            {

                                                foreach (var newNode in newMainGroup)
                                                {
                                                    if (newNode is FreeSpaceNode newFreeNode && !newFreeNode.Value.FreeSpaces.Contains(freeSpacePoint))
                                                    {
                                                        freeSpaceCount += 1;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (freeSpaceCount == freeNode.Value.FreeSpaces.Count)
                                            {
                                                ignoreGroup = false;
                                            }
                                        }
                                        if (ignoreGroup)
                                        {
                                            continue;
                                        }
                                        foreach (var iNode in group)
                                        {
                                            if (iNode is FreeSpaceNode)
                                            {
                                                continue;
                                            }

                                            BoxConflictNode boxNode = (BoxConflictNode)iNode;
                                            int boxOnGoalIndex = sData.GetEntityIndex(boxNode.Value.Ent);
                                            if (boxOnGoalIndex == -1)
                                            {
                                                continue;
                                            }
                                            Point freeSpace = GetFreeSpaceToMoveConflictTo(boxNode.Value.Ent, sData);
                                            sData.AddToFreePath(freeSpace);
                                            List<HighlevelMove> boxOnGoalSolution;
                                            if (!TrySolveSubProblem(boxOnGoalIndex, freeSpace, boxNode.Value.EntType == EntityType.AGENT, out boxOnGoalSolution, sData, 0))
                                            {
                                                throw new Exception("Could not move wrong box from goal.");
                                            }
                                            solution.AddRange(boxOnGoalSolution);
                                        }
                                    }
                                }

                                sData.FreePath.Clear();

                                //throw new Exception("level will be split by this action.");
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

                    sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                    //sData.CurrentConflicts.AddGoalNodes(sData.Level, goalToSolve);
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
                    //sData.CurrentConflicts.RemoveGoalNodes();
                    //sData.CurrentConflicts.AddFreeSpaceNodes(level);


                    Dictionary<Point, int> freeSpaceInSplitGroups = new Dictionary<Point, int>();
                    if (mainGroup.Any(x => x is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.GOAL))
                    {
                        sData.Level.AddWall(goalToSolve.Pos);
                        foreach (var group in graphGroups)
                        {
                            if (group != mainGroup)
                            {
                                List<Point> freeSpacesInGroup = GetSpacesInGraphGroup(sData.gsData, group, sData.Level).Distinct().ToList();
                                foreach (var space in freeSpacesInGroup)
                                {
                                    freeSpaceInSplitGroups.TryAdd(space, 0);
                                    freeSpaceInSplitGroups[space] += 1;
                                }
                            }
                        }

                        sData.Level.RemoveWall(goalToSolve.Pos);
                        foreach (var freespace in freeSpaceInSplitGroups)
                        {
                            sData.FreePath.TryAdd(freespace.Key, freespace.Value);
                        }
                    }


                    
                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);

                    Entity box = GetBoxToSolveProblem(sData, goalToSolve);
                    int boxIndex = sData.GetEntityIndex(box);

                    if (sData.CurrentConflicts.PositionHasNode(goalToSolve.Pos))
                    {
                        INode nodeOnGoal = sData.CurrentConflicts.GetNodeFromPosition(goalToSolve.Pos);
                        if (nodeOnGoal is BoxConflictNode boxOnGoal && boxOnGoal.Value.EntType != EntityType.GOAL)
                        {
                            int boxOnGoalIndex = sData.GetEntityIndex(boxOnGoal.Value.Ent);
                            Point freeSpace = GetFreeSpaceToMoveConflictTo(boxOnGoal.Value.Ent, sData);
                            sData.AddToFreePath(freeSpace);
                            List<HighlevelMove> boxOnGoalSolution;
                            if (!TrySolveSubProblem(boxOnGoalIndex, freeSpace, boxOnGoal.Value.EntType == EntityType.AGENT, out boxOnGoalSolution, sData, 0))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solution.AddRange(boxOnGoalSolution);
                            sData.FreePath.Clear();
                            foreach (var freespace in freeSpaceInSplitGroups)
                            {
                                sData.FreePath.Add(freespace.Key, freespace.Value);
                            }
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
                    //PrintLatestStateDiff(sData.Level, sData.SolutionGraphs);
                    Debug.Assert(sData.FreePath.Count == freeSpaceInSplitGroups.Count, "Expecting FreePath to be empty after each problem has been solved.");
                    Debug.Assert(sData.SolutionGraphs.Count == solution.Count, "asda");
                    sData.FreePath.Clear();
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

            Entity toMove = sData.GetEntity(toMoveIndex);
            solutionToSubProblem = new List<HighlevelMove>();
            Entity? agentToUse = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(sData, toMove);
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
            Point[] pathToBox = null;
            if (!toMoveIsAgent)
            {
                agentToUse = GetAgentToSolveProblem(sData, toMove);
                agentIndex = sData.GetEntityIndex(agentToUse.Value);

                sData.AddToRoutesUsed(toMovePath);
                List<HighlevelMove> solveAgentConflictMoves;
                if (!TrySolveConflicts(agentIndex.Value, toMove.Pos, out solveAgentConflictMoves, out pathToBox, sData, agentToUse, depth))
                {
                    return false;
                }
                toMove = sData.GetEntity(toMoveIndex);
                if (solveAgentConflictMoves != null)
                {
                    //solutionToSubProblem.InsertRange(0, solveAgentConflictMoves);
                    solutionToSubProblem.AddRange(solveAgentConflictMoves);
                }
                sData.RemoveFromRoutesUsed(toMovePath);
            }

            sData.CurrentState = sData.CurrentState.GetCopy();
            sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);

            Point? newAgentPos = null;
            if (agentIndex.HasValue) // then set agent pos
            {
                agentToUse = sData.CurrentState.Entities[agentIndex.Value];

                List<Point> possibleAgentPositions = new List<Point>();
                foreach (var dirDelta in Direction.NONE.DirectionDeltas())
                {
                    Point possibleAgentPos = goal + dirDelta;

                    //Can't place the agent inside a wall
                    if (sData.Level.IsWall(possibleAgentPos))
                    {
                        continue;
                    }

                    //Don't place the agent at an illegal position
                    if (sData.FreePath.ContainsKey(possibleAgentPos))
                    {
                        continue;
                    }

                    //Can't place the box on another agent or box
                    INode nodeAtPos = sData.CurrentConflicts.GetNodeFromPosition(possibleAgentPos);
                    if (nodeAtPos is BoxConflictNode boxNode && 
                        (boxNode.Value.EntType == EntityType.AGENT || 
                         boxNode.Value.EntType == EntityType.BOX) && 
                        boxNode.Value.Ent != toMove && 
                        boxNode.Value.Ent != agentToUse.Value)
                    {
                        continue;
                    }

                    possibleAgentPositions.Add(possibleAgentPos);
                }

                if (possibleAgentPositions.Count == 0)
                {
                    throw new Exception("No agent position found.");
                }

                //If the agent isn't in the box path to the goal then the agent will presumably start by pusing the box
                bool startPush = !toMovePath.Contains(agentToUse.Value.Pos);

                //If the agent starts by pushing then it should also try to end by pushing
                //as turning around to pull is more moves
                bool isPushPossible = false;
                if (startPush)
                {
                    foreach (var endAgentPos in possibleAgentPositions)
                    {
                        //If the box path contains the agents end position then the agent must've pushed the box
                        if (toMovePath.Contains(endAgentPos))
                        {
                            newAgentPos = endAgentPos;
                            isPushPossible = true;
                            break;
                        }
                    }
                }

                //If push wasn't possible then chose one of the possible pull positions
                if (!isPushPossible)
                {
                    newAgentPos = possibleAgentPositions.First();
                }

                sData.CurrentState.Entities[agentIndex.Value] = sData.CurrentState.Entities[agentIndex.Value].Move(newAgentPos.Value);
            }
            //Console.WriteLine(sData.Level.WorldToString(sData.Level.GetWallsAsWorld()));


            sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, sData.Level, sData.RemovedEntities);
            sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
            sData.SolutionGraphs.Add(sData.CurrentConflicts);
            solutionToSubProblem.Add(new HighlevelMove(sData.CurrentState, toMove, goal, agentToUse, newAgentPos));
            //PrintLatestStateDiff(sData.Level, sData.SolutionGraphs);
            //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);
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

                sData.AddToRoutesUsed(toMovePath);

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

                    Point freeSpace = GetFreeSpaceToMoveConflictTo(conflict.Value.Ent, sData);
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

                sData.RemoveFromRoutesUsed(toMovePath);
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
    }
}