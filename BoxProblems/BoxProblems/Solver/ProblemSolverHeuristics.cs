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

        private static Point GetFreeSpaceToMoveConflictTo(Entity conflict, SolverData sData, Dictionary<Point, int> freePath)
        {
            //See if there is even 1 free space.
            int avaiableFreeSpacesCount = 0;
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is FreeSpaceNode freeSpaceNode)
                {
                    avaiableFreeSpacesCount += freeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x)).Count();
                }
            }
            if (avaiableFreeSpacesCount < 1)
            {
                throw new Exception("No free space is available");

            }
            //Get the node to start the BFS in the conflict graph, probably an easier way to do this, but not sure how this works
            INode startnode = sData.CurrentConflicts.GetNodeFromPosition(conflict.Pos); //Had to initalize it to something
            int howFarIntoFreeSpace = 0;
            var boxes = sData.Level.GetBoxes();
            foreach (var p in freePath)
            {
                if (sData.CurrentConflicts.PositionHasNode(p.Key))
                {
                    if (sData.CurrentConflicts.GetNodeFromPosition(p.Key) is BoxConflictNode boxnode && boxnode.Value.EntType==EntityType.BOX)
                    {
                        howFarIntoFreeSpace += 1;
                    }
                }

            }
            FreeSpaceNode freeSpaceNodeToUse = null;
            var visitedNodes = new HashSet<INode>();
            (INode node,int extraBoxes,int numFreeSpaces, int repeatBoxes) starttuple = (startnode, 0, 0, 0);
            var bfsQueue = new Queue<(INode node, int extraBoxes, int numFreeSpaces, int repeatBoxes)>();
            int minExtraBoxes = int.MaxValue;
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
                    if (currentBoxNode.Value.EntType==EntityType.BOX)//Maybe it should hold for agents aswell, past the 1 agent, but idk
                    {
                        if (!freePath.ContainsKey(currentBoxNode.Value.Ent.Pos))
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
                    currentNumFreeSpaces += currentFreeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x)).Count();
                    if (currentNumFreeSpaces >= howFarIntoFreeSpace + currentExtraBoxes)
                    {
                        if(currentExtraBoxes+currentRepeatBoxes< minExtraBoxes)
                        {
                            freeSpaceNodeToUse = currentFreeSpaceNode;
                            minExtraBoxes = currentExtraBoxes+currentRepeatBoxes;
                        }
                        if (currentExtraBoxes == 0 && currentRepeatBoxes ==0)
                        {
                            break;
                        }

                    }

                }
                foreach (var edge in currentNode.GetNodeEnds())
                {
                    var newNode = edge;
                    (INode node, int extraBoxes, int numFreeSpaces, int repeatBoxes) newTuple = (newNode, currentExtraBoxes, currentNumFreeSpaces, currentRepeatBoxes);
                    bfsQueue.Enqueue(newTuple);
                }
                if (bfsQueue.Count==0)
                {
                    howFarIntoFreeSpace += currentExtraBoxes;
                }
            }

            if (freeSpaceNodeToUse==null)
            {
               throw new Exception("Not enough free space is available");
            }
            var potentialFreeSpacePoints = freeSpaceNodeToUse.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x));
            Point freeSpacePointToUse = potentialFreeSpacePoints.First();
            int maxDistance = 0;
            foreach (var FSP in potentialFreeSpacePoints)
            {
                var distancesMap = Precomputer.GetDistanceMap(sData.Level.Walls, FSP, false);
                int minDistance = int.MaxValue;
                foreach (var p in freePath)
                {
                    minDistance = Math.Min(minDistance, distancesMap[p.Key.X, p.Key.Y]);
                }
                if (maxDistance<minDistance)
                {
                    maxDistance = minDistance;
                    freeSpacePointToUse = FSP;
                    if (maxDistance >= howFarIntoFreeSpace)
                    {
                        break;
                    }
                }

            }
            return freeSpacePointToUse;

        }
    }
}
