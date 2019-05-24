using BoxProblems.Graphing;
using PriorityQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private static Goal GetGoalToSolve(HashSet<Goal> goals, GoalGraph goalGraph, SolverData sData)
        {
            if (goals.Count == 1)
            {
                return goals.First();
            }
            //LevelVisualizer.PrintLatestStateDiff(sData.Level, sData.SolutionGraphs); 
            Entity ent = GetEntityToSolveProblem(sData, EntityType.BOX, null, goals);
            if (ent ==new Entity())
            {
                throw new Exception("Could not find path from any goal in the layer to a box that can solve that goal");
            }
            return sData.Level.Goals.Single(x => x.Ent == ent);
        }

        private static Entity GetBoxToSolveProblem(SolverData sData, Goal goal)
        {
            Entity returnEntity = new Entity();
            int numBoxes = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.BOX && boxNode.Value.Ent.Type == goal.Ent.Type)
                {
                    numBoxes += 1;
                    returnEntity = boxNode.Value.Ent;
                }
            }
            if (numBoxes == 0)
            {
                throw new Exception("No box exist that can solve this problem.");
            }
            if (numBoxes == 1)
            {
                return returnEntity;
            }
            returnEntity = GetEntityToSolveProblem(sData, EntityType.BOX, goal.Ent);
            if (returnEntity == new Entity())
            {
                throw new Exception("Could not find any path from the goal to a box of the correct type");
            }
            return returnEntity;
        }

        private static Entity GetAgentToSolveProblem(SolverData sData, Entity toMove)
        {
            Entity returnEntity = new Entity();
            int NumAgents = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.AGENT && boxNode.Value.Ent.Color == toMove.Color)
                {
                    NumAgents += 1;
                    returnEntity = boxNode.Value.Ent;
                }
            }
            if (NumAgents == 0)
            {
                throw new Exception("No agent exists that can solve this problem.");
            }
            if (NumAgents == 1)
            {
                return returnEntity;
            }
            return GetEntityToSolveProblem(sData, EntityType.AGENT, toMove);
        }

        private static Entity GetEntityToSolveProblem(SolverData sData, EntityType entitytype, Entity? entity = null, HashSet<Goal> goals = null)
        {
            int minimumConflict = int.MaxValue;
            Entity minimumConflictEntity = new Entity();
            if (entity == null)
            {
                foreach (var goal in goals)
                {
                    INode startnode = sData.CurrentConflicts.GetNodeFromPosition(goal.Ent.Pos);
                    (int numConflicts, Entity goalEntity) = CalculateMinimumConflict(sData, startnode, goal.Ent, goal.EntType == EntityType.AGENT_GOAL ? EntityType.AGENT : EntityType.BOX);
                    if (minimumConflict > numConflicts)
                    {
                        minimumConflict = numConflicts;
                        minimumConflictEntity = goal.Ent;
                        if (minimumConflict == 0)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                INode startnode = sData.CurrentConflicts.GetNodeFromPosition(entity.Value.Pos);
                if (startnode is FreeSpaceNode)
                {
                    //Dictionary<Point, INode> positionToNode = sData.CurrentConflicts.getPositionToNode();
                    //Console.WriteLine("\n\n\n Entity: " + entity.ToString());
                    //foreach (KeyValuePair<Point, INode> node in positionToNode)
                    //{
                    //    Console.WriteLine("Point = {0}, Node = {1}", node.Key, node.Value.ToString());
                    //}
                    //if (entity.Value.Pos == new Point(13, 6))
                    //{
                    //
                    //}
                } 
                (minimumConflict, minimumConflictEntity) = CalculateMinimumConflict(sData, startnode, entity.Value, entitytype);
            }
            return minimumConflictEntity;

        }

        private static (int, Entity) CalculateMinimumConflict(SolverData sData, INode startNode, Entity entity, EntityType bfsGoalEntType)
        {
            var visitedNodes = new HashSet<INode>();

            var priorityQueue = new PriorityQueue<(INode node, int numConflicts), int>();

            int minimumConflict = int.MaxValue;
            Entity minimumConflictEntity = new Entity();

            if (startNode is FreeSpaceNode)
            {
                var distanceMap = Precomputer.GetDistanceMap(sData.Level.Walls, entity.Pos, false);

                foreach (BoxConflictNode edge in startNode.GetNodeEnds()) // Add edge as BoxConflict 
                {
                    int currentNumConflicts = 0;
                    if (edge.Value.EntType == EntityType.BOX)
                    {
                        currentNumConflicts++;
                    }
                    int distance = distanceMap[edge.Value.Ent.Pos.X, edge.Value.Ent.Pos.Y];
                    priorityQueue.Enqueue((edge, currentNumConflicts), distance);
                    visitedNodes.Add(edge);
                }
            }
            else
            {
                // startNode is not FreeSpaceNode
                (INode node, int numConflicts) starttuple = (startNode, 0);
                priorityQueue.Enqueue(starttuple, int.MaxValue);
                visitedNodes.Add(startNode);
            }

            // Run greedy search on BoxConflictGraph
            while (priorityQueue.Count > 0)
            {
                var priResult = priorityQueue.DequeueWithPriority();
                var tuple = priResult.Value;
                var currentNode = tuple.node;
                var currentNumConflicts = tuple.numConflicts;

                if (currentNode is BoxConflictNode currentBoxNode)
                {
                    if (currentBoxNode.Value.EntType == EntityType.BOX)
                    {
                        currentNumConflicts += 1;
                    }
                    if (currentBoxNode.Value.EntType.EntityEquals(bfsGoalEntType))
                    {
                        if (startNode is BoxConflictNode startBoxNode)
                        {
                            if ((bfsGoalEntType == EntityType.AGENT && currentBoxNode.Value.Ent.Color == startBoxNode.Value.Ent.Color) || (bfsGoalEntType == EntityType.BOX && entity.Type == currentBoxNode.Value.Ent.Type))
                            {
                                if (currentNumConflicts < minimumConflict)
                                {
                                    minimumConflict = currentNumConflicts;
                                    minimumConflictEntity = currentBoxNode.Value.Ent;
                                }
                            }
                        }
                        if (startNode is FreeSpaceNode startFreeSpaceNode && entity.Type == currentBoxNode.Value.Ent.Type && currentNumConflicts < minimumConflict)
                        {
                            minimumConflict = currentNumConflicts;
                            minimumConflictEntity = currentBoxNode.Value.Ent;
                        }
                        if (minimumConflict == 0)
                        {
                            break;
                        }
                    }
                }

                var boxNode = (BoxConflictNode)currentNode;
                foreach (var edge in boxNode.Edges)
                {
                    if (edge.End is BoxConflictNode boxConflictNode)
                    {
                        if (!visitedNodes.Contains(boxConflictNode))
                        {
                            visitedNodes.Add(boxConflictNode);
                            priorityQueue.Enqueue((boxConflictNode, currentNumConflicts), priResult.Priority + edge.Value.Distance);
                        }
                    }
                }

            }
            return (minimumConflict, minimumConflictEntity);
        }

        private static Point GetFreeSpaceToMoveConflictTo(Entity conflict, SolverData sData, int additonalIntoFreeSpace = 0)
        {
            //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.RoutesUsed);

            HashSet<Point> agentPositions = new HashSet<Point>();
            if (((BoxConflictNode)sData.CurrentConflicts.GetNodeFromPosition(conflict.Pos)).Value.EntType == EntityType.AGENT)
            {
                foreach (var node in sData.CurrentConflicts.Nodes)
                {
                    if (node is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.AGENT)
                    {
                        agentPositions.Add(boxNode.Value.Ent.Pos);
                    }
                }
            }
            Func<Point, bool> isFreeSpaceAvailable = new Func<Point, bool>(freeSpace => !sData.FreePath.ContainsKey(freeSpace) && !sData.RoutesUsed.ContainsKey(freeSpace) && !agentPositions.Contains(freeSpace));

            //See if there is even 1 free space.
            int avaiableFreeSpacesCount = GetAvailableSpaces(sData, isFreeSpaceAvailable);
            if (avaiableFreeSpacesCount < 1)
            {
                isFreeSpaceAvailable = new Func<Point, bool>(freeSpace => !sData.FreePath.ContainsKey(freeSpace) && !agentPositions.Contains(freeSpace));
                avaiableFreeSpacesCount = GetAvailableSpaces(sData, isFreeSpaceAvailable);
                if (avaiableFreeSpacesCount < 1)
                {
                    throw new Exception("No free space is available");
                }

            }
            //Get the node to start the BFS in the conflict graph, probably an easier way to do this, but not sure how this works
            INode startnode = sData.CurrentConflicts.GetNodeFromPosition(conflict.Pos); //Had to initalize it to something
            int howFarIntoFreeSpace = -1;// -1 for the box that should be moved into the goal as it is always on the path;
            var boxes = sData.Level.GetBoxes();
            foreach (var p in sData.RoutesUsed)
            {
                if (sData.CurrentConflicts.PositionHasNode(p.Key))
                {
                    if (sData.CurrentConflicts.GetNodeFromPosition(p.Key) is BoxConflictNode boxnode)
                    {
                        howFarIntoFreeSpace += 1;
                    }
                }
            }
            howFarIntoFreeSpace += additonalIntoFreeSpace + 1;

            int[,] spacesDistance = new int[sData.Level.Width, sData.Level.Height];
            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    spacesDistance[x, y] = int.MaxValue;
                }
            }
            foreach (var routePos in sData.RoutesUsed)
            {
                MergePosIntoDistanceGrid(sData, spacesDistance, routePos.Key, isFreeSpaceAvailable);
            }
            MergePosIntoDistanceGrid(sData, spacesDistance, conflict.Pos, isFreeSpaceAvailable);

            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    spacesDistance[x, y] = isFreeSpaceAvailable(new Point(x, y)) ? spacesDistance[x, y] : 0;
                }
            }

            foreach (var entity in sData.CurrentState.GetBoxes(sData.Level))
            {
                spacesDistance[entity.Pos.X, entity.Pos.Y] = 0;
            }

            int maxDistance = int.MinValue;
            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    if (spacesDistance[x, y] != int.MaxValue)
                    {
                        maxDistance = Math.Max(maxDistance, spacesDistance[x, y]);
                    }
                }
            }

            if (maxDistance == 0)
            {
                //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.RoutesUsed);
                //LevelVisualizer.PrintFreeSpace(sData.Level, sData.CurrentState, sData.FreePath);
            }

            int[] freeSpacesCounbtByDistance = new int[maxDistance];
            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    int distance = spacesDistance[x, y];
                    if (distance != int.MaxValue && distance != 0)
                    {
                        int index = distance - 1;
                        freeSpacesCounbtByDistance[index]++;
                    }
                }
            }

            Func<Point, int> heuristic = new Func<Point, int>(x =>
            {
                int h = 0;
                INode node = sData.CurrentConflicts.GetNodeFromPosition(x);
                if (node is BoxConflictNode)
                {
                    h += 20;
                }
                if (IsCorridor(sData.Level, x))
                {
                    h += 10;
                }
                return h;
            });
            short[,] spacesPriorityMap = Precomputer.GetDistanceHeuristicsMap(sData.Level.Walls, conflict.Pos, heuristic, false);

            //LevelVisualizer.PrintSpaceDistances(sData.Level, sData.CurrentState, spacesDistance);
            //LevelVisualizer.PrintSpaceDistances(sData.Level, sData.CurrentState, spacesPriorityMap);

            for (int i = howFarIntoFreeSpace - 1; i < freeSpacesCounbtByDistance.Length; i++)
            {
                if (freeSpacesCounbtByDistance[i] != 0)
                {
                    return GetBestPriorityFreeSpace(i + 1, spacesDistance, spacesPriorityMap, sData.Level);
                }
            }

            for (int i = freeSpacesCounbtByDistance.Length - 1; i >= 0; i--)
            {
                if (freeSpacesCounbtByDistance[i] != 0)
                {
                    return GetBestPriorityFreeSpace(i + 1, spacesDistance, spacesPriorityMap, sData.Level);
                }
            }

            throw new Exception("Failed to find a freespace at least one was expected to exist.");
        }

        private static Point GetBestPriorityFreeSpace(int bestDistance, int[,] spacesDistance, short[,] spacesPriorityMap, Level level)
        {
            int bestPriority = int.MaxValue;
            Point bestPosition = new Point();
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    int distance = spacesDistance[x, y];
                    if (distance >= bestDistance && distance != int.MaxValue)
                    {
                        int priority = spacesPriorityMap[x, y];
                        if (priority < bestPriority)
                        {
                            bestPriority = priority;
                            bestPosition = new Point(x, y);
                        }
                    }
                }
            }

            if (bestPriority == int.MaxValue)
            {
                throw new Exception($"Expected to find at least one free space with a distance of {bestDistance} but found none.");
            }

            return bestPosition;
        }

        private static void MergePosIntoDistanceGrid(SolverData sData, int[,] spacesDistance, Point start, Func<Point, bool> isSpaceFree)
        {
            short[,] distanceMap = Precomputer.GetDistanceMap(sData.Level.Walls, start, false);
            for (int y = 0; y < sData.Level.Height; y++)
            {
                for (int x = 0; x < sData.Level.Width; x++)
                {
                    if (distanceMap[x, y] != 0)
                    {
                        spacesDistance[x, y] = Math.Min(spacesDistance[x, y], distanceMap[x, y]);
                    }
                }
            }
        }

        private static int GetAvailableSpaces(SolverData sData, Func<Point, bool> isFreeSpaceAvailable)
        {
            int avaiableFreeSpacesCount = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is FreeSpaceNode freeSpaceNode)
                {
                    avaiableFreeSpacesCount += freeSpaceNode.Value.FreeSpaces.Sum(x => isFreeSpaceAvailable(x) ? 1 : 0);
                }
            }

            return avaiableFreeSpacesCount;
        }

        private static bool IsNextToTurningPoint(Level level, Point FSP)
        {
            foreach (Point n in neighbours)
            {
                Point p = FSP + n;
                if (!level.Walls[p.X, p.Y] && !IsCorridor(level, p))
                    return true;
            }
            return false;
        }

        private static readonly Point[] neighbours = new Point[4] { new Point(0, 1), new Point(0, -1), new Point(-1, 0), new Point(1, 0) };

        private static bool IsCorridor(Level level, Point p)
        { // This works for corridor corners too.
            int walls = 0;
            if (level.Walls[p.X + 1, p.Y]) walls++;
            if (level.Walls[p.X - 1, p.Y]) walls++;
            if (level.Walls[p.X, p.Y + 1]) walls++;
            if (level.Walls[p.X, p.Y - 1]) walls++;
            return (2 <= walls);
        }
    }
}
