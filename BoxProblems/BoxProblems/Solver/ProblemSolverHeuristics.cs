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
        private static Entity GetGoalToSolve(HashSet<Entity> goals, GoalGraph goalGraph, SolverData sData)
        {
            if (goals.Count == 1)
            {
                return goals.First();
            }
            return GetEntityToSolveProblem(sData, EntityType.BOX, null, goals);
        }

        private static Entity GetBoxToSolveProblem(SolverData sData, Entity goal)
        {
            Entity returnEntity = new Entity();
            int numBoxes = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.BOX && boxNode.Value.Ent.Type == goal.Type)
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
            return GetEntityToSolveProblem(sData, EntityType.BOX, goal);
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

        private static Entity GetEntityToSolveProblem(SolverData sData, EntityType entitytype, Entity? entity = null, HashSet<Entity> goals = null)
        {
            int minimumConflict = int.MaxValue;
            Entity minimumConflictEntity = new Entity();
            if (entity == null)
            {
                foreach (var goal in goals)
                {
                    INode startnode = sData.CurrentConflicts.GetNodeFromPosition(goal.Pos);
                    (int numConflicts, Entity goalEntity) = CalculateMinimumConflict(sData, startnode, goal, entitytype);
                    if (minimumConflict > numConflicts)
                    {
                        minimumConflict = numConflicts;
                        minimumConflictEntity = goal;
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
                    if (currentBoxNode.Value.EntType == bfsGoalEntType)
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

        private static Point GetFreeSpaceToMoveConflictTo(Entity conflict, SolverData sData)
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
            int avaiableFreeSpacesCount = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is FreeSpaceNode freeSpaceNode)
                {
                    avaiableFreeSpacesCount += freeSpaceNode.Value.FreeSpaces.Sum(x => isFreeSpaceAvailable(x) ? 1 : 0);
                }
            }
            if (avaiableFreeSpacesCount < 1)
            {
                throw new Exception("No free space is available");

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
            FreeSpaceNode freeSpaceNodeToUse = null;
            var visitedNodes = new HashSet<INode>();
            (INode node, int extraBoxes, int numFreeSpaces, int repeatBoxes) starttuple = (startnode, 0, 0, 0);
            var bfsQueue = new Queue<(INode node, int extraBoxes, int numFreeSpaces, int repeatBoxes)>();
            int minExtraBoxes = int.MaxValue;
            int totalExtraBoxes = 0;
            bfsQueue.Enqueue(starttuple);
            while (bfsQueue.Count > 0)
            {
                var tuple = bfsQueue.Dequeue();
                var currentNode = tuple.node;
                var currentExtraBoxes = tuple.extraBoxes;
                var currentNumFreeSpaces = tuple.numFreeSpaces;
                var currentRepeatBoxes = tuple.repeatBoxes;
                if (visitedNodes.Contains(currentNode))
                {
                    continue;
                }
                visitedNodes.Add(currentNode);
                if (currentNode is BoxConflictNode currentBoxNode)
                {
                    if (currentBoxNode.Value.EntType == EntityType.BOX)
                    {
                        if (!sData.RoutesUsed.ContainsKey(currentBoxNode.Value.Ent.Pos))
                        {
                            currentExtraBoxes += 1;
                        }
                        else
                        {
                            currentRepeatBoxes += 1;
                        }

                    }
                }
                if (currentNode is FreeSpaceNode currentFreeSpaceNode)
                {
                    var newFreeSpacesCount = currentFreeSpaceNode.Value.FreeSpaces.Where(isFreeSpaceAvailable).Count();
                    if(newFreeSpacesCount>0)
                    {
                        currentNumFreeSpaces += newFreeSpacesCount;
                        if (currentNumFreeSpaces >= howFarIntoFreeSpace + currentExtraBoxes)
                        {
                            if (currentExtraBoxes + currentRepeatBoxes < minExtraBoxes)
                            {
                                freeSpaceNodeToUse = currentFreeSpaceNode;
                                minExtraBoxes = currentExtraBoxes + currentRepeatBoxes;
                                totalExtraBoxes = currentExtraBoxes;
                            }
                            if (currentExtraBoxes == 0 && currentRepeatBoxes == 0)
                            {
                                break;
                            }

                        }

                    }

                }
                foreach (var edge in currentNode.GetNodeEnds())
                {
                    var newNode = edge;
                    (INode node, int extraBoxes, int numFreeSpaces, int repeatBoxes) newTuple = (newNode, currentExtraBoxes, currentNumFreeSpaces, currentRepeatBoxes);
                    bfsQueue.Enqueue(newTuple);
                }
                if (bfsQueue.Count == 0)
                {
                    howFarIntoFreeSpace += totalExtraBoxes;
                }
            }

            if (freeSpaceNodeToUse == null)
            {
                throw new Exception("Not enough free space is available");
            }
            var potentialFreeSpacePoints = freeSpaceNodeToUse.Value.FreeSpaces.Where(isFreeSpaceAvailable).ToList();
            Point freeSpacePointToUse = potentialFreeSpacePoints.First();
            int maxDistance = int.MinValue;
            foreach (var FSP in potentialFreeSpacePoints)
            {
                var distancesMap = Precomputer.GetDistanceMap(sData.Level.Walls, FSP, false);
                int minDistance = int.MaxValue;
                foreach (var p in sData.RoutesUsed)
                {
                    int distance = distancesMap[p.Key.X, p.Key.Y];
                    minDistance = Math.Min(minDistance, distance == 0 ? int.MaxValue : distance);
                }
                if (maxDistance < minDistance)
                {
                    maxDistance = minDistance;
                    freeSpacePointToUse = FSP;
                    if (maxDistance >= howFarIntoFreeSpace + 1)
                    {
                        break;
                    }
                }

            }

            // If FreeSpacePoint was picked naively, then set place it somewhere closer.
            //if (freeSpacePointToUse == potentialFreeSpacePoints.First() && potentialFreeSpacePoints.Count > 5)
            //{
            //    int dist = int.MaxValue;
            //    var conflictDistMap = Precomputer.GetDistanceMap(sData.Level.Walls, conflict.Pos, false);
            //    foreach (var FSP in potentialFreeSpacePoints)
            //    {
            //        if (conflictDistMap[FSP.X, FSP.Y] < dist)
            //        {
            //            dist = conflictDistMap[FSP.X, FSP.Y];
            //            freeSpacePointToUse = FSP;
            //        }
            //    }
            //}

            return freeSpacePointToUse;

        }
    }
}
