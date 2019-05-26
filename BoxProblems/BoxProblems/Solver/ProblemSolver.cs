using BoxProblems.Graphing;
using PriorityQueue;
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
        public readonly Dictionary<char, int> agentTypes;
        public readonly Dictionary<char, int> boxeTypes;
        public readonly Dictionary<(int color, char type), int> boxGoalTypeAndColor;
        public readonly Dictionary<(int color, char type), int> agentGoalTypeAndColor;
        internal GroupInformation(List<INode> group)
        {
            this.agentColors = new Dictionary<int, int>();
            this.agentTypes = new Dictionary<char, int>();
            this.boxeTypes = new Dictionary<char, int>();
            this.boxGoalTypeAndColor = new Dictionary<(int, char), int>();
            this.agentGoalTypeAndColor = new Dictionary<(int color, char type), int>();
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
                        agentColors.TryAdd(entityColor, 0);
                        agentColors[entityColor] += 1;

                        agentTypes.TryAdd(entityType, 0);
                        agentTypes[entityType] += 1;
                        break;
                    case EntityType.BOX:
                        if (!boxeTypes.ContainsKey(entityType))
                        {
                            boxeTypes.Add(entityType, 0);
                        }
                        boxeTypes[entityType] += 1;
                        break;
                    case EntityType.BOX_GOAL:
                        var boxGoalKey = (entityColor, entityType);
                        if (!boxGoalTypeAndColor.ContainsKey(boxGoalKey))
                        {
                            boxGoalTypeAndColor.Add(boxGoalKey, 0);
                        }
                        boxGoalTypeAndColor[boxGoalKey] += 1;
                        break;
                    case EntityType.AGENT_GOAL:
                        var agentGoalKey = (entityColor, entityType);
                        if (!agentGoalTypeAndColor.ContainsKey(agentGoalKey))
                        {
                            agentGoalTypeAndColor.Add(agentGoalKey, 0);
                        }
                        agentGoalTypeAndColor[agentGoalKey] += 1;
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
                        if (BoxSwimming.MeasureBoxDensity(x) > 0.99)
                        {
                            Point p = new Point();
                            foreach (Goal g in x.Goals)
                                if (g.EntType == EntityType.AGENT_GOAL)
                                {
                                    p = g.Ent.Pos;
                                    break;
                                }
                            (new BoxSwimmingSolver(x, p)).Solve(); // Set point to agent pos goal.
                            solutionPieces.Add(new HighlevelLevelSolution(null, null, x));
                        }
                        else
                        {
                            var solution = SolvePartialLevel(x, cancelSource.Token);
                            solution = HighLevelOptimizer.Optimize(solution);
                            //WriteToFile(optimizedSolution);
                            solutionPieces.Add(solution);
                        }
                    });
                }
                else
                {
                    foreach (var x in levels)
                    {
                        if (BoxSwimming.MeasureBoxDensity(x) > 0.99)
                        {
                            Point p = new Point();
                            foreach (Goal g in x.Goals)
                                if (g.EntType == EntityType.AGENT_GOAL)
                                {
                                    p = g.Ent.Pos;
                                    break;
                                }
                            (new BoxSwimmingSolver(x, p)).Solve(); // Set point to agent pos goal.
                            solutionPieces.Add(new HighlevelLevelSolution(null, null, x));
                        }
                        else
                        {
                            var solution = SolvePartialLevel(x, cancelSource.Token);
                            solution = HighLevelOptimizer.Optimize(solution);
                            //WriteToFile(optimizedSolution);
                            solutionPieces.Add(solution);
                        }
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
                    if (node is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal())
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



        private static bool EveryGroupHasEverythingNeeded(List<List<INode>> graphGroups, List<INode> mainGroup, Goal goal)
        {
            foreach (var group in graphGroups)
            {
                GroupInformation groupInfo = new GroupInformation(group);

                if (group == mainGroup)
                {
                    if (goal.EntType == EntityType.AGENT_GOAL)
                    {
                        if (groupInfo.agentTypes.ContainsKey(goal.Ent.Type))
                        {
                            groupInfo.agentTypes[goal.Ent.Type] += 1;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (goal.EntType == EntityType.BOX_GOAL)
                    {
                        if (groupInfo.boxeTypes.ContainsKey(goal.Ent.Type))
                        {
                            groupInfo.boxeTypes[goal.Ent.Type] += 1;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        throw new Exception($"Unknown entity type: {goal.EntType}");
                    }
                }
                var goalTuple = (goal.Ent.Color, goal.Ent.Type);
                if (goal.EntType == EntityType.AGENT_GOAL)
                {
                    groupInfo.agentGoalTypeAndColor.TryAdd(goalTuple, 0);
                    groupInfo.agentGoalTypeAndColor[goalTuple] += 1;
                }
                else if (goal.EntType == EntityType.BOX_GOAL)
                {
                    groupInfo.boxGoalTypeAndColor.TryAdd(goalTuple, 0);
                    groupInfo.boxGoalTypeAndColor[goalTuple] += 1;
                }
                else
                {
                    throw new Exception($"Unknown entity type: {goal.EntType}");
                }

                foreach (var goalInfo in groupInfo.boxGoalTypeAndColor)
                {
                    if (!groupInfo.boxeTypes.TryGetValue(goalInfo.Key.type, out int boxCount) || boxCount < goalInfo.Value)
                    {
                        return false;
                    }
                    if (!groupInfo.agentColors.TryGetValue(goalInfo.Key.color, out int agentCount))
                    {
                        return false;
                    }
                }

                foreach (var goalInfo in groupInfo.agentGoalTypeAndColor)
                {
                    if (!groupInfo.agentTypes.TryGetValue(goalInfo.Key.type, out int agentCount) || agentCount < goalInfo.Value)
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

        private static bool DoesMainGroupNeedTheEntity(BoxConflictNode boxNode, Dictionary<(int color, char type), int> goalsNeeded, Dictionary<char, int> entities)
        {
            int entitiesNeeded = 0;
            if (boxNode.Value.EntType==EntityType.BOX)
            {
                if (!goalsNeeded.TryGetValue((0, boxNode.Value.Ent.Type), out entitiesNeeded))
                {
                    return false;
                }
            }
            else
            {
                if (!goalsNeeded.TryGetValue((boxNode.Value.Ent.Color, boxNode.Value.Ent.Type), out entitiesNeeded))
                {
                    return false;
                }
            }

            int boxesInMain = 0;
            if (entities.TryGetValue(boxNode.Value.Ent.Type, out boxesInMain))
            {
                if (boxesInMain >= entitiesNeeded)
                {
                    goalsNeeded.Remove((boxNode.Value.Ent.Color, boxNode.Value.Ent.Type));
                    return false;
                }
                else
                {
                    goalsNeeded[(boxNode.Value.Ent.Color, boxNode.Value.Ent.Type)] -= 1;
                }
            }

            return true;
        }

        private static HighlevelLevelSolution SolvePartialLevel(Level level, CancellationToken cancelToken)
        {
            List<HighlevelMove> solution = new List<HighlevelMove>();
            SolverData sData = new SolverData(level, cancelToken);
            GoalGraph goalGraph = new GoalGraph(sData.gsData, level.InitialState, level);
            GoalPriority priority = new GoalPriority(level, goalGraph, cancelToken);
            //Console.WriteLine(priority.ToLevelString(sData.Level));
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
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, level);//Something is wrong in here in SAGroupName missing edges from freespace to X
                    Goal goalToSolve = GetGoalToSolve(currentLayer.Goals, goalGraph, sData);

                    sData.Level.AddPermanentWalll(goalToSolve.Ent.Pos);
                    sData.Level.AddWall(goalToSolve.Ent.Pos);
                    bool[] boxesOfAgentColor = null;
                    if (goalToSolve.EntType==EntityType.AGENT_GOAL)
                    {
                        var agentWithGoalType = sData.CurrentConflicts.Nodes.Where(x => x is BoxConflictNode boxNode &&
                                                                                       boxNode.Value.EntType == EntityType.AGENT &&
                                                                                       boxNode.Value.Ent.Type == goalToSolve.Ent.Type).Cast<BoxConflictNode>().ToList();

                        // should only do this if this is the only agent of that color
                        if (agentWithGoalType.Count==1)
                        {
                            var boxes = sData.CurrentState.GetBoxes(sData.Level);
                            boxesOfAgentColor = new bool[sData.Level.BoxCount];
                            for (int i = 0; i < boxes.Length; i++)
                            {
                                if (boxes[i].Color == agentWithGoalType.First().Value.Ent.Color && !sData.RemovedEntities.Contains(boxes[i]))
                                {
                                    sData.Level.AddPermanentWalll(boxes[i].Pos);
                                    sData.RemovedEntities.Add(boxes[i]);
                                    boxesOfAgentColor[i] = true;
                                }
                            }
                        }

                    }

                    sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                    sData.CurrentConflicts.AddGoalNodes(sData.gsData, sData.Level, goalToSolve.Ent);
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
                    sData.Level.RemovePermanentWall(goalToSolve.Ent.Pos);
                    sData.Level.RemoveWall(goalToSolve.Ent.Pos);
                    if (boxesOfAgentColor!=null)
                    {
                        var boxes = sData.CurrentState.GetBoxes(sData.Level);
                        for (int i = 0; i < boxesOfAgentColor.Length; i++)
                        {
                            if (boxesOfAgentColor[i])
                            {
                                sData.Level.RemovePermanentWall(boxes[i].Pos);
                                sData.RemovedEntities.Remove(boxes[i]);
                            }
                        }

                    }
                    //GraphShower.ShowGraph(sData.CurrentConflicts);
                    var graphGroups = GetGraphGroups(sData.CurrentConflicts, goalToSolve.Ent.Pos);
                    var mainGroup = GetMainGraphGroup(graphGroups);
                    if (goalToSolve.EntType == EntityType.AGENT_GOAL)
                    {
                        var agentWithGoalType = sData.CurrentConflicts.Nodes.Where(x => x is BoxConflictNode boxNode && 
                                                                                        boxNode.Value.EntType == EntityType.AGENT && 
                                                                                        boxNode.Value.Ent.Type == goalToSolve.Ent.Type).Cast<BoxConflictNode>().ToList();
                        var boxesWithGoalType = level.InitialState.GetBoxes(level).ToArray().Where(x => x.Color == agentWithGoalType.First().Value.Ent.Color).ToList();
                        var unsolvedGoalsWithAgentColor = goalPriorityLinkedLayers.SelectMany(x => x.Goals.Where(y => boxesWithGoalType.Any(z => z.Type == y.Ent.Type) && y.Ent != goalToSolve.Ent)).ToList();

                        if ((agentWithGoalType.Count == 1 && unsolvedGoalsWithAgentColor.Count > 0)||graphGroups.Count(x=>x.Any(y => y is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal()))>1)
                        {
                            currentLayer.Goals.Remove(goalToSolve);
                            if (currentLayerNode.Next != null)
                            {
                                currentLayerNode.Next.Value.Goals.Add(goalToSolve);
                            }
                            else
                            {
                                var goalsInLayer = new HashSet<Goal>() { goalToSolve };
                                goalPriorityLinkedLayers.AddAfter(currentLayerNode, new GoalPriorityLayer(goalsInLayer));

                            }
                            continue;
                        }
                    }




                    if (graphGroups.Where(x => x.Any(y => y is BoxConflictNode)).Count() > 1 &&
                        !EveryGroupHasEverythingNeeded(graphGroups, mainGroup, goalToSolve) &&
                        mainGroup.Any(x => x is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal()))
                    {
                        var mainGroupInformation = new GroupInformation(mainGroup);
                        bool isGroupOtherThenMainWithGoal = false;
                        foreach (var group in graphGroups)
                        {
                            if (group != mainGroup)
                            {
                                foreach (var node in group)
                                {
                                    if (node is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal())
                                    {
                                        isGroupOtherThenMainWithGoal = true;
                                        goto groupDone;
                                    }
                                }
                            }
                        }
                        groupDone:

                        if (!isGroupOtherThenMainWithGoal)
                        {
                            if (currentLayer.Goals.Count == 1)
                            {

                                sData.Level.AddWall(goalToSolve.Ent.Pos);
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
                                sData.FreePath.Add(goalToSolve.Ent.Pos, 1);
                                sData.Level.RemoveWall(goalToSolve.Ent.Pos);
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

                                        var groupinfo = new GroupInformation(group);
                                        int intoFreeSpace = group.Where(x => x is BoxConflictNode boxNode && boxNode.Value.EntType.IsMoveable()).Count();
                                        foreach (var iNode in group)
                                        {
                                            if (iNode is FreeSpaceNode)
                                            {
                                                continue;
                                            }

                                            BoxConflictNode boxNode = (BoxConflictNode)iNode;

                                            if (goalToSolve.EntType == EntityType.AGENT_GOAL)
                                            {
                                                if (boxNode.Value.EntType == EntityType.AGENT)
                                                {
                                                    if (!DoesMainGroupNeedTheEntity(boxNode, mainGroupInformation.agentGoalTypeAndColor, mainGroupInformation.agentTypes))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else if (goalToSolve.EntType == EntityType.BOX_GOAL)
                                            {
                                                if (boxNode.Value.EntType == EntityType.BOX)
                                                {
                                                    if (!DoesMainGroupNeedTheEntity(boxNode, mainGroupInformation.boxGoalTypeAndColor, mainGroupInformation.boxeTypes))
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                throw new Exception($"Unknown entity type: {goalToSolve.EntType}");
                                            }

                                            int boxOnGoalIndex = sData.GetEntityIndex(boxNode.Value.Ent);
                                            if (boxOnGoalIndex == -1)
                                            {
                                                continue;
                                            }
                                            //LevelVisualizer.PrintLatestStateDiff(sData.Level,sData.SolutionGraphs);
                                           //LevelVisualizer.PrintFreeSpace(sData.Level,sData.CurrentState,sData.FreePath);
                                            Point freeSpace = GetFreeSpaceToMoveConflictTo(boxNode.Value.Ent, sData, intoFreeSpace);
                                            intoFreeSpace--;
                                            sData.AddToFreePath(freeSpace);
                                            List<HighlevelMove> boxOnGoalSolution;
                                            if (!TrySolveSubProblem(boxOnGoalIndex, freeSpace, boxNode.Value.EntType == EntityType.AGENT, out boxOnGoalSolution, sData, 0, false))
                                            {
                                                throw new Exception("Could not move wrong box from goal.");
                                            }
                                            sData.RemoveFromFreePath(freeSpace);
                                            solution.AddRange(boxOnGoalSolution);
                                        }
                                    }
                                }

                                sData.FreePath.Clear();

                                //throw new Exception("level will be split by this action.");
                            }
                        }

                        if (currentLayer.Goals.Count(x => x.EntType == EntityType.BOX_GOAL) > 1 || isGroupOtherThenMainWithGoal)
                        {
                            currentLayer.Goals.Remove(goalToSolve);
                            if (currentLayerNode.Next != null)
                            {
                                currentLayerNode.Next.Value.Goals.Add(goalToSolve);
                            }
                            else
                            {
                                var goalsInLayer = new HashSet<Goal>() { goalToSolve };
                                goalPriorityLinkedLayers.AddAfter(currentLayerNode, new GoalPriorityLayer(goalsInLayer));

                            }

                            //foreach (var layer in goalPriorityLinkedLayers)
                            //{
                            //    Console.WriteLine(string.Join(" ", layer.Goals.Select(x => char.ToLower(x.Ent.Type).ToString() + " " + x.Ent.Pos)));
                            //}
                            continue;
                        }

                    }

                    //GraphShower.ShowSimplifiedGraph<DistanceEdgeInfo>(sData.CurrentConflicts);

                    sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, level, sData.RemovedEntities);
                    //sData.CurrentConflicts.AddGoalNodes(sData.Level, goalToSolve);
                    sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
                    //sData.CurrentConflicts.RemoveGoalNodes();
                    //sData.CurrentConflicts.AddFreeSpaceNodes(level);


                    Dictionary<Point, int> freeSpaceInSplitGroups = new Dictionary<Point, int>();
                    if (mainGroup.Any(x => x is BoxConflictNode boxNode && boxNode.Value.EntType.IsGoal()))
                    {
                        sData.Level.AddWall(goalToSolve.Ent.Pos);
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

                        sData.Level.RemoveWall(goalToSolve.Ent.Pos);
                        foreach (var freespace in freeSpaceInSplitGroups)
                        {
                            sData.FreePath.TryAdd(freespace.Key, freespace.Value);
                        }
                    }
                    //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);

                    List<HighlevelMove> solutionMoves;
                    if (goalToSolve.EntType == EntityType.AGENT_GOAL)
                    {
                        Entity agent = sData.CurrentState.Entities.Single(x => x.Type == goalToSolve.Ent.Type);
                        int agentIndex = sData.GetEntityIndex(agent);
                        sData.AddToFreePath(goalToSolve.Ent.Pos);
                        if (!TrySolveSubProblem(agentIndex, goalToSolve.Ent.Pos, true, out solutionMoves, sData, 0, true))
                        {
                            throw new Exception("Can't handle that there is no high level solution yet.");
                        }
                        sData.RemoveFromFreePath(goalToSolve.Ent.Pos);
                        sData.RemovedEntities.Add(new Entity(solutionMoves.Last().ToHere, agent.Color, agent.Type));
                    }
                    else if (goalToSolve.EntType == EntityType.BOX_GOAL)
                    {
                        Entity box = GetBoxToSolveProblem(sData, goalToSolve);
                        int boxIndex = sData.GetEntityIndex(box);
                        sData.AddToFreePath(goalToSolve.Ent.Pos);
                        if (!TrySolveSubProblem(boxIndex, goalToSolve.Ent.Pos, false, out solutionMoves, sData, 0, true))
                        {
                            throw new Exception("Can't handle that there is no high level solution yet.");
                        }
                        sData.RemoveFromFreePath(goalToSolve.Ent.Pos);
                        sData.RemovedEntities.Add(new Entity(solutionMoves.Last().ToHere, box.Color, box.Type));
                    }
                    else
                    {
                        throw new Exception($"Unknown entity type: {goalToSolve.EntType}");
                    }

                    solution.AddRange(solutionMoves);

                    level.AddPermanentWalll(goalToSolve.Ent.Pos);

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
                level.RemovePermanentWall(goal.Ent.Pos);
                level.RemoveWall(goal.Ent.Pos);
            }

#if DEBUG
            foreach (var goal in level.Goals)
            {
                if (goal.EntType == EntityType.AGENT_GOAL)
                {
                    if (!sData.CurrentState.GetAgents(sData.Level).ToArray().Any(x => x.Pos == goal.Ent.Pos && x.Type == goal.Ent.Type))
                    {
                        throw new Exception("Didn't fix all agent goals");
                    }
                }
                else
                {
                    if (!sData.CurrentState.GetBoxes(sData.Level).ToArray().Any(x => x.Pos == goal.Ent.Pos && x.Type == goal.Ent.Type))
                    {
                        throw new Exception("Didn't fix all goals");
                    }
                }
            }
#endif

            return new HighlevelLevelSolution(solution, sData.SolutionGraphs, level);
        }

        private static bool TrySolveSubProblem(int toMoveIndex, Point goal, bool toMoveIsAgent, out List<HighlevelMove> solutionToSubProblem, SolverData sData, int depth, bool isGoalAnObstable)
        {
            if (depth == 200)
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
            if (!TrySolveConflicts(toMoveIndex, goal, out solveConflictMoves, out toMovePath, sData, agentToUse, depth, isGoalAnObstable))
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
                if (!TrySolveConflicts(agentIndex.Value, toMove.Pos, out solveAgentConflictMoves, out pathToBox, sData, agentToUse, depth, false))
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



            Point? newAgentPos = null;
            Point? uTurnPos = null;
            if (agentIndex.HasValue) // then set agent pos
            {
                agentToUse = sData.CurrentState.Entities[agentIndex.Value];
                List<(Point, bool)> possibleAgentPositions = new List<(Point, bool)>();
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

                    //Can may place the box on another agent or box
                    INode nodeAtPos = sData.CurrentConflicts.GetNodeFromPosition(possibleAgentPos);
                    if (nodeAtPos is BoxConflictNode boxNode && boxNode.Value.EntType.IsMoveable())
                    {
                        if (boxNode.Value.Ent == toMove || boxNode.Value.Ent == agentToUse.Value)
                        {
                            possibleAgentPositions.Add((possibleAgentPos, false));

                        }
                        else
                        {
                            possibleAgentPositions.Add((possibleAgentPos, true));
                        }
                    }
                    else
                    {
                        possibleAgentPositions.Add((possibleAgentPos, false));
                    }


                }

                if (possibleAgentPositions.Count == 0)
                {
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
                            newAgentPos = possibleAgentPos;
                            break;
                        }
                    }
                    return true;
                }
                //If the agent isn't in the box path to the goal then the agent will presumably start by pusing the box
                bool startPush = !toMovePath.Contains(agentToUse.Value.Pos);
                uTurnPos = GetUTurnPos(sData, agentToUse.Value.Pos);
                if (!uTurnPos.HasValue)
                {
                    startPush = startPush && !pathToBox.Contains(goal);
                }

                //If the agent starts by pushing then it should also try to end by pushing
                //as turning around to pull is more moves
                bool positionFound = false;
                bool occupiedPositionFound = false;
                if (startPush)
                {
                    foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                    {
                        // In clustered situations (friendOfDFS), it makes little sense to put the agent's end at the box's start position.
                        if (endAgentPos == toMove.Pos)
                            continue;

                        //If the box path contains the agents end position then the agent must've pushed the box
                        if (toMovePath.Contains(endAgentPos))
                        {
                            if (positionOccupied)
                            {
                                occupiedPositionFound = true;
                                continue;
                            }
                            newAgentPos = endAgentPos;
                            positionFound = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                    {
                        if (!toMovePath.Contains(endAgentPos))
                        {
                            if (positionOccupied)
                            {
                                occupiedPositionFound = true;
                                continue;
                            }
                            newAgentPos = endAgentPos;
                            positionFound = true;
                            break;
                        }
                    }
                }

                bool needUTurn = true;
                if (startPush)
                {
                    foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                    {
                        // In clustered situations (friendOfDFS), it makes little sense to put the agent's end at the box's start position.
                        if (endAgentPos == toMove.Pos)
                            continue;

                        //If the box path contains the agents end position then the agent must've pushed the box
                        if (toMovePath.Contains(endAgentPos))
                        {
                            needUTurn = false;
                            break;
                        }
                    }
                }
                else
                {
                    foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                    {

                        if (!toMovePath.Contains(endAgentPos))
                        {
                            needUTurn = false;
                            break;
                        }
                    }
                }

                if (needUTurn)
                {
                    bool hasTurnOnPath = false;
                    for (int i = 1; i < toMovePath.Length - 1; i++)
                    {
                        if (!IsCorridor(sData.Level, toMovePath[i]))
                        {
                            hasTurnOnPath = true;
                            break;
                        }
                    }

                    if (!hasTurnOnPath)
                    {
                        if (uTurnPos.HasValue)
                        {
                            sData.AddToRoutesUsed(toMovePath);
                            sData.AddToRoutesUsed(pathToBox);
                            sData.AddToFreePath(goal);
                            sData.AddToFreePath(uTurnPos.Value);
                            sData.AddToFreePath(uTurnPos.Value + Direction.E.DirectionDelta());
                            sData.AddToFreePath(uTurnPos.Value + Direction.N.DirectionDelta());
                            sData.AddToFreePath(uTurnPos.Value + Direction.W.DirectionDelta());
                            sData.AddToFreePath(uTurnPos.Value + Direction.S.DirectionDelta());
                            List<HighlevelMove> entityOnAgentEndPositionSolution;
                            if (!TrySolveSubProblem(toMoveIndex, uTurnPos.Value, false, out entityOnAgentEndPositionSolution, sData, depth + 1, true))
                            {
                                throw new Exception("Could not move wrong box from goal.");
                            }
                            solutionToSubProblem.AddRange(entityOnAgentEndPositionSolution);
                            sData.RemoveFromRoutesUsed(toMovePath);
                            sData.RemoveFromRoutesUsed(pathToBox);
                            sData.RemoveFromFreePath(uTurnPos.Value);
                            sData.RemoveFromFreePath(uTurnPos.Value + Direction.E.DirectionDelta());
                            sData.RemoveFromFreePath(uTurnPos.Value + Direction.N.DirectionDelta());
                            sData.RemoveFromFreePath(uTurnPos.Value + Direction.W.DirectionDelta());
                            sData.RemoveFromFreePath(uTurnPos.Value + Direction.S.DirectionDelta());
                            sData.RemoveFromFreePath(goal);
                            agentToUse = sData.GetEntity(agentIndex.Value);
                            toMove = sData.GetEntity(toMoveIndex);
                        }
                    }
                }

                //If push wasn't possible then chose one of the possible pull positions
                if (!positionFound && possibleAgentPositions.Count(x => !x.Item2) > 0)
                {
                    if (uTurnPos.HasValue)
                    {
                        newAgentPos = possibleAgentPositions.First(x => !x.Item2).Item1;
                        positionFound = true;
                    }

                }
                if (!positionFound && possibleAgentPositions.Count(x => x.Item2) == 0)
                {
                    newAgentPos = possibleAgentPositions.First().Item1;
                }
                if (!positionFound)
                {
                    if (occupiedPositionFound)
                    {
                        if (startPush)
                        {
                            foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                            {
                                // In clustered situations (friendOfDFS), it makes little sense to put the agent's end at the box's start position.
                                if (endAgentPos == toMove.Pos)
                                    continue;

                                //If the box path contains the agents end position then the agent must've pushed the box
                                if (toMovePath.Contains(endAgentPos))
                                {
                                    newAgentPos = endAgentPos;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach ((var endAgentPos, var positionOccupied) in possibleAgentPositions)
                            {
                                if (!toMovePath.Contains(endAgentPos))
                                {
                                    newAgentPos = endAgentPos;

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        newAgentPos= possibleAgentPositions.First().Item1;
                    }
                    
                    
                    var entityOnAgentEndPosition = ((BoxConflictNode)sData.CurrentConflicts.GetNodeFromPosition(newAgentPos.Value)).Value.Ent;
                    var entityOnAgentEndPositionType = ((BoxConflictNode)sData.CurrentConflicts.GetNodeFromPosition(newAgentPos.Value)).Value.EntType;
                    int entityOnAgentEndPositionIndex = sData.GetEntityIndex(entityOnAgentEndPosition);
                    sData.AddToRoutesUsed(toMovePath);
                    sData.AddToRoutesUsed(pathToBox);
                    sData.AddToFreePath(goal);
                    Point freeSpace = GetFreeSpaceToMoveConflictTo(entityOnAgentEndPosition, sData);
                    sData.AddToFreePath(freeSpace);
                    List<HighlevelMove> entityOnAgentEndPositionSolution;
                    if (!TrySolveSubProblem(entityOnAgentEndPositionIndex, freeSpace, entityOnAgentEndPositionType == EntityType.AGENT, out entityOnAgentEndPositionSolution, sData, depth + 1, false))
                    {
                        throw new Exception("Could not move wrong box from goal.");
                    }
                    solutionToSubProblem.AddRange(entityOnAgentEndPositionSolution);
                    sData.RemoveFromRoutesUsed(toMovePath);
                    sData.RemoveFromRoutesUsed(pathToBox);
                    sData.RemoveFromFreePath(freeSpace);
                    sData.RemoveFromFreePath(goal);
                    agentToUse = sData.GetEntity(agentIndex.Value); 
                }

                toMove = sData.GetEntity(toMoveIndex);

            }
            sData.CurrentState = sData.CurrentState.GetCopy();
            sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);
            if (agentIndex.HasValue)
            {
                sData.CurrentState.Entities[agentIndex.Value] = sData.CurrentState.Entities[agentIndex.Value].Move(newAgentPos.Value);
            }

            //Console.WriteLine(sData.Level.WorldToString(sData.Level.GetWallsAsWorld()));


            sData.CurrentConflicts = new BoxConflictGraph(sData.gsData, sData.CurrentState, sData.Level, sData.RemovedEntities);
            sData.CurrentConflicts.AddFreeSpaceNodes(sData.gsData, sData.Level);
            sData.SolutionGraphs.Add(sData.CurrentConflicts);
            solutionToSubProblem.Add(new HighlevelMove(sData.CurrentState, toMove, goal, agentToUse, newAgentPos, uTurnPos));
            //LevelVisualizer.PrintLatestStateDiff(sData.Level, sData.SolutionGraphs);
            //GraphShower.ShowSimplifiedGraph<EmptyEdgeInfo>(sData.CurrentConflicts);
            return true;
        }

        private static bool TrySolveConflicts(int toMoveIndex, Point goal, out List<HighlevelMove> solutionToSubProblem, out Point[] toMovePath, SolverData sData, Entity? agentNotConflict, int depth, bool isGoalAnObstable)
        {
#if DEBUG
            Dictionary<Point, int> freePathCopy = new Dictionary<Point, int>(sData.FreePath);
#endif

            solutionToSubProblem = new List<HighlevelMove>();
            int counter = 0;
            while (true)
            {
                Entity toMove = sData.GetEntity(toMoveIndex);
                List<BoxConflictNode> conflicts = GetConflicts(toMove, goal, sData.CurrentConflicts, isGoalAnObstable, sData);

                //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.FreePath);
                //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.RoutesUsed);

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
                //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.RoutesUsed);

                bool toMoveMoved = false;
                do
                {
                    if (counter == 50)
                    {
                        throw new Exception($"Failed to solve conflicts in {counter} tries.");
                    }
                    counter++;
                    sData.CancelToken.ThrowIfCancellationRequested();

                    //LevelVisualizer.PrintPath(sData.Level, sData.CurrentState, toMovePath.ToList());

                    if (goal == new Point(25, 33))
                    {

                    }

                    BoxConflictNode conflict = conflicts.First();
                    if (agentNotConflict.HasValue && conflict.Value.Ent == agentNotConflict.Value)
                    {
                        conflicts.Remove(conflict);
                        continue;
                    }

                    Point freeSpace = GetFreeSpaceToMoveConflictTo(conflict.Value.Ent, sData);
                    sData.AddToFreePath(freeSpace);

                    //Console.WriteLine($"Conflict: {conflict.ToString()} -> {freeSpace}");
                    if (TrySolveSubProblem(sData.GetEntityIndex(conflict.Value.Ent), freeSpace, conflict.Value.EntType == EntityType.AGENT, out List<HighlevelMove> solutionMoves, sData, depth + 1, true))
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

                    conflicts = GetConflicts(toMove, goal, sData.CurrentConflicts, isGoalAnObstable, sData);
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

        private static List<BoxConflictNode> GetConflicts(Entity toMove, Point goal, BoxConflictGraph currentConflicts, bool isGoalAnObstable, SolverData sData)
        {
            INode startNode = currentConflicts.GetNodeFromPosition(toMove.Pos);
            foreach (var edgeNode in startNode.GetNodeEnds())
            {
                if (edgeNode is BoxConflictNode boxNode && boxNode.Value.Ent.Pos == goal)
                {
                    //if there is a box or agent on the end path and it's not supposed to be there then return that as a conflict
                    if ((boxNode.Value.EntType.IsMoveable()) && !(!isGoalAnObstable && boxNode.Value.Ent.Pos == goal))
                    {
                        return new List<BoxConflictNode>() { boxNode };
                    }
                    else
                    {
                        return null;
                    }
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
                    if (current is BoxConflictNode asdasd)
                    {
                        conflicts.Add(asdasd);
                    }
                    while (childToParent.ContainsKey(current))
                    {
                        current = childToParent[current];
                        if (current is BoxConflictNode boxconNode)
                        {
                            conflicts.Add(boxconNode);
                        }

                    }

                    //toMove itself can't be a conflict to itself
                    conflicts.RemoveAll(x => x.Value.Ent == toMove || (!isGoalAnObstable && x.Value.Ent.Pos == goal));

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

            //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.RoutesUsed);
            //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.FreePath);

            throw new Exception("Found no path from  entity to goal.");
        }
        private static Point? GetUTurnPos(SolverData sData, Point agentpos)
        {
            if (!IsCorridor(sData.Level, agentpos))
            {
                return agentpos;
            }
            short[,] distanceMap = Precomputer.GetDistanceMap(sData.Level.Walls, agentpos, false);
            Point? bestTurningPoint = null;
            int bestScore = int.MaxValue;
            for (int x = 1; x < sData.Level.Width - 1; x++)
            {
                for (int y = 1; y < sData.Level.Height - 1; y++)
                {
                    Point p = new Point(x, y);
                    if (!sData.Level.Walls[x, y] && !IsCorridor(sData.Level, p))
                    {
                        if (distanceMap[x, y] != 0)
                        {
                            int score = distanceMap[x, y] + ((sData.CurrentConflicts.PositionHasNode(p) && sData.CurrentConflicts.GetNodeFromPosition(p) is BoxConflictNode) ? 5 : 0);
                            if (score < bestScore)
                            {
                                bestTurningPoint = p;
                                bestScore = score;
                            }
                        }
                    }
                }
            }

            return bestTurningPoint;
        }
    }
}