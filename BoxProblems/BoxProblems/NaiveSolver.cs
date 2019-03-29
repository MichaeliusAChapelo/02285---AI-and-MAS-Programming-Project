using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal class NaiveSolver
    {
        public static Level level;


        internal readonly struct AgentBoxGoalPairs
        {
            public readonly Entity Agent;
            public readonly Entity Box;
            public readonly Entity Goal;

            public AgentBoxGoalPairs(Entity Agent, Entity Box, Entity Goal)
            {
                this.Agent = Agent;
                this.Box = Box;
                this.Goal = Goal;
            }
        }

        internal readonly struct EntityMapping
        {
            public readonly Entity Agent;
            public readonly int[,] DistanceMap;
            public readonly Point[] Solution; // Don't know if needed.

            public EntityMapping(Entity Agent, int[,] DistanceMap, Point[] Solution)
            {
                this.Agent = Agent;
                this.DistanceMap = DistanceMap;
                this.Solution = Solution;
            }
        }

        //const string levelPath = "MAExample.lvl"; // MABahaMAS.lvl
        //NaiveSolver.level = Level.ReadLevel(File.ReadAllLines(levelPath));
        // var solver = new NaiveSolver();
        //solver.Solve();
        // return

        public void Solve()
        { // Do your magic!
            // Part 1
            List<AgentBoxGoalPairs> priorities = AssignGoals();


            // Part Deux
            List<EntityMapping> mappings = InitialMappings(priorities);

            int x;
            x = 1;
            // Part Trois


        }

        // Use State.GetAgents and State.GetBoxes to find relevant data.

        #region 1) Assign agent goals
        // Gives all agents a goal.
        public List<AgentBoxGoalPairs> AssignGoals()
        {
            var agents = level.GetAgents();
            var priorities = new List<AgentBoxGoalPairs>();

            foreach (Entity agent in agents)
            {
                var pairs = AssignAgentGoal(agent);
                if (pairs.HasValue)
                    priorities.Add(pairs.Value);
            }

            return priorities;
        }

        //For assigning individual agents
        public AgentBoxGoalPairs? AssignAgentGoal(Entity agent)
        {
            var boxes = level.GetBoxes();
            var goals = level.Goals;
            foreach (Entity box in boxes)
                if (agent.Color == box.Color)
                    foreach (Entity goal in goals)
                        if (box.Type == goal.Type)
                            return new AgentBoxGoalPairs(agent, box, goal);
            return null;
        }

        #endregion

        #region 2) BFS to destination + distance maps

        public List<EntityMapping> InitialMappings(List<AgentBoxGoalPairs> priorities)
        {
            List<EntityMapping> mappings = new List<EntityMapping>();
            foreach (AgentBoxGoalPairs priority in priorities)
            {
                if (Point.ManhattenDistance(priority.Agent.Pos, priority.Box.Pos) > 1)
                    mappings.Add(CreateEntityMap(priority.Agent, priority.Box.Pos)); // Head to box
                else
                    mappings.Add(CreateEntityMap(priority.Box, priority.Goal.Pos));
            }
            return mappings;
        }

        public EntityMapping CreateEntityMap(Entity entity, Point destination)
        {
            int[,] distanceMap = BFS(entity.Pos, destination);
            return new EntityMapping(entity, distanceMap, BacktrackSolution(destination, distanceMap));
        }

        public int[,] BFS(Point start, Point destination)
        {
            var distanceMap = new int[level.Width, level.Height];

            var frontier = new Queue<Point>();
            frontier.Enqueue(start);

            //int currentDistance = 0;
            while (frontier.Count != 0)
            {
                Point p = frontier.Dequeue();

                if (level.Walls[p.X, p.Y])
                {
                    distanceMap[p.X, p.Y] = 1337;
                    continue;
                }

                int dist = 1337;

                // Only adds if not explored.
                AddIfUnexplored(new Point(p.X + 1, p.Y), distanceMap, frontier, start, ref dist);
                AddIfUnexplored(new Point(p.X - 1, p.Y), distanceMap, frontier, start, ref dist);
                AddIfUnexplored(new Point(p.X, p.Y + 1), distanceMap, frontier, start, ref dist);
                AddIfUnexplored(new Point(p.X, p.Y - 1), distanceMap, frontier, start, ref dist);

                // Set distance at point.
                distanceMap[p.X, p.Y] = (p == start) ? 0 : dist + 1;
                //currentDistance++;
            }
            return distanceMap;
        }

        private void AddIfUnexplored(Point next, int[,] distanceMap, Queue<Point> frontier, Point start, ref int dist)
        {
            if (distanceMap[next.X, next.Y] == 0 && !next.Equals(start))
                frontier.Enqueue(next);
            else if (distanceMap[next.X, next.Y] < dist)
                dist = distanceMap[next.X, next.Y];
        }

        private Point[] BacktrackSolution(Point destination, int[,] distanceMap)
        {
            var solutionPath = new List<Point>() { destination };

            Point current = destination;
            while (distanceMap[current.X, current.Y] != 0)
            {
                Point next = CheckNeighbours(current, distanceMap);
                solutionPath.Add(next);
                current = next;
            }

            solutionPath.Reverse();
            return solutionPath.ToArray();
        }

        private Point CheckNeighbours(Point p, int[,] DistanceMap)
        {
            if (DistanceMap[p.X + 1, p.Y] < DistanceMap[p.X, p.Y])
                return new Point(p.X + 1, p.Y);

            if (DistanceMap[p.X - 1, p.Y] < DistanceMap[p.X, p.Y])
                return new Point(p.X - 1, p.Y);

            if (DistanceMap[p.X, p.Y + 1] < DistanceMap[p.X, p.Y])
                return new Point(p.X, p.Y + 1);

            if (DistanceMap[p.X, p.Y - 1] < DistanceMap[p.X, p.Y])
                return new Point(p.X, p.Y - 1);

            throw new Exception("Invalid BFS solution path.");
        }

        #endregion

        #region 3) Locate Conflicts


        #endregion


    }

    #region Old A* algorithm, 02148
    //public class AStar
    //{
    //    public static List<Node> Run(Node start, Node goal)
    //    {
    //        var ClosedSet = new List<Node>();
    //        var OpenSet = new List<Node>() { start };
    //        var CameFrom = new Dictionary<Node, Node>();
    //        start.SetGScore(0);
    //        start.SetFScore(start.CalculateCosts(goal));

    //        while (OpenSet.Count != 0)
    //        {
    //            Node current = OpenSet[0];
    //            foreach (Node n in OpenSet)
    //                if (n.FScore < current.FScore)
    //                    current = n;
    //            if (current.IsGoalState(goal))
    //                return ReconstructPath(CameFrom, current);

    //            OpenSet.Remove(current);
    //            ClosedSet.Add(current);

    //            foreach (Node neighbor in current.Edges)
    //            {
    //                if (ClosedSet.Contains(neighbor))
    //                    continue;
    //                double tentative_GScore = current.GScore + current.CalculateCosts(neighbor);
    //                if (!OpenSet.Contains(neighbor))
    //                    OpenSet.Add(neighbor);
    //                else if (tentative_GScore >= neighbor.GScore)
    //                    continue;
    //                CameFrom.Add(neighbor, current);
    //                neighbor.SetGScore(tentative_GScore);
    //                neighbor.SetFScore(neighbor.GScore + neighbor.CalculateCosts(goal));
    //            }
    //        }
    //        return null;
    //    }

    //    private static List<Node> ReconstructPath(Dictionary<Node, Node> CameFrom, Node current)
    //    {
    //        var TotalPath = new List<Node>() { current };
    //        while (CameFrom.ContainsKey(current))
    //        {
    //            current = CameFrom[current];
    //            TotalPath.Add(current);
    //        }
    //        TotalPath.Reverse();
    //        return TotalPath;
    //    }

    //    public interface INode
    //    {
    //        double CalculateCosts(Node n);
    //        bool IsGoalState(Node n);
    //        void SetFScore(double i);
    //        void SetGScore(double i);
    //    }

    //    public abstract class Node : INode
    //    {
    //        public List<Node> Edges = new List<Node>();
    //        public double FScore = 0;
    //        public double GScore = 0;

    //        public abstract double CalculateCosts(Node n);
    //        public abstract bool IsGoalState(Node n);
    //        public abstract void SetFScore(double i);
    //        public abstract void SetGScore(double i);
    //    }

    //}
    #endregion
}
