using BoxProblems.Graphing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems.Solver
{
    public static partial class ProblemSolver
    {
        private static Entity GetGoalToSolve(HashSet<Entity> goals, GoalGraph goalGraph, BoxConflictGraph currentConflicts)
        {
            if (goals.Count == 1)
            {
                return goals.First();
            }
            return GetEntityToSolveProblem(currentConflicts, null, goals);
        }

        private static Entity GetBoxToSolveProblem(BoxConflictGraph currentConflicts, Entity goal)
        {
            Entity returnEntity = new Entity();
            int numBoxes = 0;
            foreach (var iNode in currentConflicts.Nodes)
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
                return GetEntityToSolveProblem(currentConflicts, goal);
        }

        private static Entity GetAgentToSolveProblem(BoxConflictGraph currentConflicts, Entity toMove)
        {
            Entity returnEntity =new Entity();
            int NumAgents = 0;
            foreach (var iNode in currentConflicts.Nodes)
            {
                if(iNode is BoxConflictNode boxNode && boxNode.Value.EntType==EntityType.AGENT && boxNode.Value.Ent.Color== toMove.Color)
                {
                    NumAgents += 1;
                    returnEntity = boxNode.Value.Ent;
                }
            }
            if (NumAgents == 0) {
                throw new Exception("No agent exists that can solve this problem.");
            }
            if (NumAgents==1)
            {
                return returnEntity;
            }
            return GetEntityToSolveProblem(currentConflicts, toMove);
        }

        private static Entity GetEntityToSolveProblem(BoxConflictGraph currentConflicts, Entity? entity=null, HashSet<Entity> goals =null)
        {
            int minimumConflict = int.MaxValue;
            Entity minimumConflictEntity = new Entity();
            if (entity == null)
            {
                foreach (var goal in goals)
                {
                    INode startnode = currentConflicts.GetNodeFromPosition(goal.Pos);
                    (int numConflicts, Entity goalEntity)=calculateMinimumConflict(currentConflicts, startnode, goal);
                    if (minimumConflict>numConflicts)
                    {
                        minimumConflict = numConflicts;
                        minimumConflictEntity = goal;
                        if (minimumConflict==0) {
                            break;
                        }
                    }

                }
            }
            else
            {
                INode startnode = currentConflicts.GetNodeFromPosition(entity.Value.Pos);
                (minimumConflict, minimumConflictEntity) = calculateMinimumConflict(currentConflicts, startnode, (Entity)entity);
            }
            return minimumConflictEntity;

        }
        private static (int, Entity) calculateMinimumConflict(BoxConflictGraph currentConflicts, INode startnode, Entity entity )
        {
            EntityType bfsGoalEntType;
            if (startnode is BoxConflictNode boxNode && boxNode.Value.EntType == EntityType.BOX)
            {
                bfsGoalEntType = EntityType.AGENT;
            }
            else
            {
                bfsGoalEntType = EntityType.BOX;
            }
            var visitedNodes = new HashSet<INode>();
            var bfsQueue = new Queue<(INode node, int numConflicts)>();
            (INode node, int numConflicts) starttuple = (startnode, 0);
            int minimumConflict=int.MaxValue;
            Entity minimumConflictEntity = new Entity();
            bfsQueue.Enqueue(starttuple);
            while (bfsQueue.Count>0)
            {
                var tuple = bfsQueue.Dequeue();
                var currentNode = tuple.node;
                var currentNumConflicts = tuple.numConflicts;
                if (visitedNodes.Contains(currentNode))
                {
                    continue;
                }
                visitedNodes.Add(currentNode);
                if (currentNode is BoxConflictNode currentBoxNode)
                {
                    if (currentBoxNode.Value.EntType == EntityType.BOX)
                    {
                        currentNumConflicts += 1;
                    }
                    if (currentBoxNode.Value.EntType == bfsGoalEntType) {
                        if (startnode is BoxConflictNode startBoxNode)
                        {
                            if((bfsGoalEntType == EntityType.AGENT && currentBoxNode.Value.Ent.Color == startBoxNode.Value.Ent.Color) || (bfsGoalEntType == EntityType.BOX && entity.Type == currentBoxNode.Value.Ent.Type))
                               {
                                if (currentNumConflicts< minimumConflict)
                                {
                                    minimumConflict = currentNumConflicts;
                                    minimumConflictEntity = currentBoxNode.Value.Ent;
                                }
                               }
                        }
                        if (startnode is FreeSpaceNode startFreeSpaceNode && entity.Type == currentBoxNode.Value.Ent.Type && currentNumConflicts< minimumConflict)
                        {
                            minimumConflict = currentNumConflicts;
                            minimumConflictEntity = currentBoxNode.Value.Ent;
                        }
                        if (minimumConflict==0)
                        {
                            break;
                        }
                    }
                }

                    foreach (var edge in currentNode.GetNodeEnds())
                {
                    var newNode = edge;
                    (INode node, int numConflicts) newTuple = (newNode, currentNumConflicts);
                    bfsQueue.Enqueue(newTuple);
                }

            }
            return (minimumConflict, minimumConflictEntity);
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
            int howFarIntoFreeSpace = -1;// -1 for the box that should be moved into the goal as it is always on the path;
            var boxes = sData.Level.GetBoxes();
            foreach (var p in freePath)
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
                    if (currentBoxNode.Value.EntType==EntityType.BOX)
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
