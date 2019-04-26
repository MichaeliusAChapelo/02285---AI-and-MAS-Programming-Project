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
            if (avaiableFreeSpacesCount==1)
            {
                foreach (var iNode in sData.CurrentConflicts.Nodes)
                {
                    if (iNode is FreeSpaceNode freeSpaceNode)
                    {
                        if (freeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x)).Count()==1) {
                            return freeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x)).First();
                        }
                    }
                }    
            }
            //Get the node to start the BFS in the conflict graph, probably an easier way to do this, but not sure how this works
            INode startnode = sData.CurrentConflicts.Nodes.First(); //Had to initalize it to something
            foreach (var iNode in sData.CurrentConflicts.Nodes)
            {
                if (iNode is BoxConflictNode node)
                {
                    if (node.Value.Ent.Pos == conflict.Pos)
                    {
                        startnode = node;
                        break;
                    }
                }
            }
            int howFarIntoFreeSpace = 0;
            var boxes = sData.Level.GetBoxes();
            foreach (var p in freePath)
            {
                if (sData.GetEntityAtPos(p.Key) != null && sData.GetEntityAtPos(p.Key).Value.Type > 64 && sData.GetEntityAtPos(p.Key).Value.Type < 91)
                {
                    Console.Write(p.Key);

                    howFarIntoFreeSpace += 1;
                }

            }
            Console.WriteLine();
            if (sData.SolutionGraphs.Count - 1 > 0)
            {
                PrintLatestStateDiff(sData.Level, sData.SolutionGraphs, sData.SolutionGraphs.Count - 1);
            }
            Console.WriteLine(howFarIntoFreeSpace);
            if (howFarIntoFreeSpace <= 0)
            {
                howFarIntoFreeSpace = 1;
            }
            FreeSpaceNode freeSpaceNodeToUse = null;
            Point freeSpacePointToUse;
            var visitedNodes = new HashSet<INode>();
            Tuple<INode, int, int> starttuple = new Tuple<INode, int, int>(startnode, howFarIntoFreeSpace, 0);
            var bfsQueue = new Queue<Tuple<INode, int, int>>();
            bfsQueue.Enqueue(starttuple);
            while (bfsQueue.Count > 0)
            {
                var tuple = bfsQueue.Dequeue();
                var currentNode = tuple.Item1;
                var boxesToMove = tuple.Item2;
                var freeSpacesNotOnPath = tuple.Item3;
                if (visitedNodes.Contains(currentNode))
                {
                    continue;
                }
                visitedNodes.Add(currentNode);
                if (currentNode is BoxConflictNode)
                {
                    var currentBoxNode = (BoxConflictNode)currentNode;
                    if (currentBoxNode.Value.Ent.Type>64 && currentBoxNode.Value.Ent.Type < 91 && !freePath.ContainsKey(currentBoxNode.Value.Ent.Pos))//Maybe it should hold for agents aswell, past the 1 agent, but idk
                    {
                        boxesToMove += 1;
                    }
                    foreach (var edge in currentBoxNode.Edges)
                    {
                        var newNode = edge.End;
                        Tuple<INode, int, int> newTuple = new Tuple<INode, int, int>(newNode, boxesToMove, freeSpacesNotOnPath);
                        bfsQueue.Enqueue(newTuple);
                    }
                }
                if (currentNode is FreeSpaceNode)
                {
                    var currentFreeSpaceNode = (FreeSpaceNode)currentNode;
                    freeSpacesNotOnPath += currentFreeSpaceNode.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x)).Count();
                    Console.WriteLine(boxesToMove +" "+ freeSpacesNotOnPath);
                    if (freeSpacesNotOnPath >= boxesToMove)
                    {
                        freeSpaceNodeToUse = currentFreeSpaceNode;
                        howFarIntoFreeSpace = boxesToMove;
                        break;
                    }

                    foreach (var edge in currentFreeSpaceNode.Edges)
                    {
                        var newNode = edge.End;
                        Tuple<INode, int, int> newTuple = new Tuple<INode, int, int>(newNode, boxesToMove, freeSpacesNotOnPath);
                        bfsQueue.Enqueue(newTuple);
                    }

                }

            }

            if (freeSpaceNodeToUse==null)
            {
               throw new Exception("Not enough free space is available");
            }
            var potentialFreeSpacePoints = freeSpaceNodeToUse.Value.FreeSpaces.Where(x => !freePath.ContainsKey(x));
            freeSpacePointToUse = potentialFreeSpacePoints.First();
            int maxDistance = 0;
            foreach (var FSP in potentialFreeSpacePoints)
            {
                var distancesMap = Precomputer.GetDistanceMap(sData.Level.Walls, FSP, false);
                int minDistance = 10000;
                foreach (var p in freePath)
                {
                    minDistance = Math.Min(minDistance, distancesMap[p.Key.X, p.Key.Y]);
                }
                if (maxDistance<minDistance)
                {
                    maxDistance = minDistance;
                    freeSpacePointToUse = FSP;
                }
                if (maxDistance>= howFarIntoFreeSpace)
                {
                    break;
                }
            }
            return freeSpacePointToUse;

        }
    }
}
