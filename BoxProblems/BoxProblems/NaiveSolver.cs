using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxProblems
{
    internal class NaiveSolver
    {
        // Find some way to make the agent deal with boxes.
        // Preferably by pushing from agent to box to goal in one whole path.

        public static int totalAgentCount;
        Level level;
        State currentState;
        List<AgentBoxGoalPairs?> priorities; // Box/Goal target for agents.
        List<EntityMapping?> mappings; // Agent paths.
        int[] waits; // How long each agent with a goal must wait.
        int g = 0; // Number of turns.
        Entity[] agents;

        public NaiveSolver(Level level) { this.level = level; }

        internal readonly struct AgentBoxGoalPairs
        {
            public readonly Entity Agent;
            public readonly Entity Box;
            public readonly Entity Goal;
            public readonly bool AgentIsNextToBox;

            public AgentBoxGoalPairs(Entity Agent, Entity Box, Entity Goal)
            {
                this.Agent = Agent;
                this.Box = Box;
                this.Goal = Goal;
                this.AgentIsNextToBox = (Point.ManhattenDistance(Agent.Pos, Box.Pos) == 1);
            }
        }

        internal struct EntityMapping
        {
            public readonly Entity Agent;
            public readonly int[,] DistanceMap;
            public List<Point> Solution; // Don't know if needed.

            public EntityMapping(Entity Agent, int[,] DistanceMap, List<Point> Solution)
            {
                this.Agent = Agent;
                this.DistanceMap = DistanceMap;
                this.Solution = Solution;
            }
        }

        // GOD FUNCTION
        public void Solve()
        {
            currentState = level.InitialState;
            agents = level.GetAgents().ToArray();

            while (true)
            {
                priorities = AssignGoals(); // Parte Uno: Assign agent goals. First in list should have highest priority.
                // Do NOT rely on indexing from this list. Agents with nothing to do are not included in list!

                mappings = InitialMappings(); // Parte Deux: Find path to destination

                if (IsGoalState()) break; // If no agent has any goals, we at goal state boys.

                waits = new int[agents.Length]; // Parte Trois: Locate agent conflicts. Is currently solved by waiting.
                // For each pair of agents, calculate how long to wait. Agents last in priority list waits the most.
                for (int i = 0; i < agents.Length - 1; ++i)
                    for (int j = i + 1; j < agents.Length; ++j)
                        waits[i] += LocateConflicts(mappings[i], mappings[j]);

                // Parte Quatre: Write commands:
                WriteCommands();

                // Parte Cinq: Replace current state, then restart algorithm.
                currentState = CreateCurrentState();
            }
            Console.Error.WriteLine("\n WE SOLVE, WE DONE");
        }

        // MULTI GOD FUNCTION
        public List<string[]> AsyncSolve()
        {
            currentState = level.InitialState;
            agents = level.GetAgents().ToArray();

            List<string[]> commands = new List<string[]>() { };

            while (true)
            {
                priorities = AssignGoals(); // Parte Uno: Assign agent goals. First in list should have highest priority.
                // Do NOT rely on indexing from this list. Agents with nothing to do are not included in list!

                mappings = InitialMappings(); // Parte Deux: Find path to destination

                if (IsGoalState()) break; // If no agent has any goals, we at goal state boys.

                waits = new int[agents.Length]; // Parte Trois: Locate agent conflicts. Is currently solved by waiting.
                // For each pair of agents, calculate how long to wait. Agents last in priority list waits the most.
                for (int i = 0; i < agents.Length - 1; ++i)
                    for (int j = i + 1; j < agents.Length; ++j)
                        waits[i] += LocateConflicts(mappings[i], mappings[j]);

                // Parte Quatre: Write commands:
                commands.AddRange(ReturnCommands());

                // Parte Cinq: Replace current state, then restart algorithm.
                currentState = CreateCurrentState();
            }

            return commands;
        }

        #region Zeroness: General purpose
        public bool IsNextTo(Entity a, Entity b) { return Point.ManhattenDistance(a.Pos, b.Pos) == 1; }

        public bool IsNextTo(Point a, Entity b) { return Point.ManhattenDistance(a, b.Pos) == 1; }

        public Direction PointToDirection(Point p1, Point p2)
        {
            Point delta = p2 - p1;

            if (delta.X > 0)
                return Direction.E;
            if (delta.X < 0)
                return Direction.W;
            if (delta.Y < 0)
                return Direction.N;
            if (delta.Y > 0)
                return Direction.S;
            throw new Exception("Pair of points could not resolve to a direction.");
        }
        #endregion

        #region Parte Uno: Assign agent goals

        // Gives all agents a goal.
        public List<AgentBoxGoalPairs?> AssignGoals()
        {
            var priorities = new List<AgentBoxGoalPairs?>();
            foreach (Entity agent in agents)
                priorities.Add(AssignAgentGoal(agent));
            return priorities;
        }

        //For assigning individual agents
        public AgentBoxGoalPairs? AssignAgentGoal(Entity agent)
        {
            foreach (Entity box in currentState.GetBoxes(level))
                if (agent.Color == box.Color)
                    foreach (Entity goal in level.Goals)
                        if (box.Type == goal.Type && box.Pos != goal.Pos)
                            return new AgentBoxGoalPairs(agent, box, goal);
            return null;
        }

        #endregion

        #region Parte Deux: BFS to solution.

        public List<EntityMapping?> InitialMappings()
        {
            var mappings = new List<EntityMapping?>();
            foreach (AgentBoxGoalPairs? priority in priorities)
                if (!priority.HasValue)
                    mappings.Add(null);
                else if (!priority.Value.AgentIsNextToBox)
                    mappings.Add(CreateEntityMap(priority.Value.Agent, priority.Value.Box.Pos)); // Agent move to box
                else
                {
                    mappings.Add(CreateEntityMap(priority.Value.Box, priority.Value.Goal.Pos)); // Push box to goal
                    mappings.Last().Value.Solution.Insert(0, priority.Value.Agent.Pos); // Add agent as first index.
                    mappings.Last().Value.Solution.Add(priority.Value.Goal.Pos);
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

            while (frontier.Count != 0) // Explores the entire map. Suboptimal but safer.
            //while (distanceMap[destination.X, destination.Y] != 0) // Stops once goal is found.
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
            }
            return distanceMap;
        }

        private void AddIfUnexplored(Point next, int[,] distanceMap, Queue<Point> frontier, Point start, ref int dist)
        {
            if (distanceMap[next.X, next.Y] == 0 && !next.Equals(start))
                frontier.Enqueue(next);
            else if (distanceMap[next.X, next.Y] < dist)
                dist = distanceMap[next.X, next.Y]; // TODO: Add breakpoint here because this case never happens due to BFS properties.
        }

        private List<Point> BacktrackSolution(Point destination, int[,] distanceMap)
        {
            var solutionPath = new List<Point>() { destination };

            Point current = destination;
            while (distanceMap[current.X, current.Y] != 0)
            {
                Point next = CheckNeighbours(current, distanceMap);
                solutionPath.Add(next);
                current = next;
            }
            solutionPath.RemoveAt(0);
            solutionPath.Reverse();
            return solutionPath;
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

        #region Parte Trois: Locate Conflicts

        // Strategy: Locate overlapping (corridor) positions
        public int LocateConflicts(EntityMapping? map1, EntityMapping? map2)
        {
            if (!map1.HasValue || !map2.HasValue) return 0;

            // Possible optimization: Give agents a path around a conflict zone.
            // You can make a solution in which an agent simply takes a path around the conflict.

            List<Point> overlaps = new List<Point>();
            foreach (Point p in map1.Value.Solution)
                //if (!overlaps.Contains(p) || IsCorridor(p))
                if (map2.Value.Solution.Contains(p))
                    overlaps.Add(p);

            // Current simple solution is to make an agent take no actions
            // for the number of conflicts in his path.
            return overlaps.Count;
        }

        public bool IsCorridor(Point p)
        {
            if (level.Walls[p.X + 1, p.Y] && level.Walls[p.X - 1, p.Y])
                return true; // Vertical corridor
            if (level.Walls[p.X, p.Y + 1] && level.Walls[p.X, p.Y - 1])
                return true; // Horizontal corridor
            return false; // No corridor
        }

        #endregion

        #region Parte Quatre: Command until agent needs new goals

        // Continually writes commands until any agent needs a new goal.
        public void WriteCommands()
        {
            while (!AgentReachedDestination())
            {
                g++; // Add turn count.
                string[] commands = new string[agents.Length];

                // Iterates through every agent and adds a command
                for (int i = 0; i < agents.Length; ++i)
                {
                    if (!priorities[i].HasValue) // Agent has no goal.
                    {
                        commands[i] = ServerCommunicator.NoOp();
                    }
                    else if (waits[i] != 0) // Agent should wait.
                    {
                        commands[i] = ServerCommunicator.NoOp();
                        waits[i]--;
                    }
                    else if (!priorities[i].Value.AgentIsNextToBox) // Agent moves to box
                    {
                        var solution = mappings[i].Value.Solution;
                        Direction d = PointToDirection(solution[0], solution[1]);
                        solution.RemoveAt(0);
                        commands[i] = ServerCommunicator.Move(d);
                    }
                    else // Push box to goal.
                    {
                        var solution = mappings[i].Value.Solution;
                        Direction d1 = PointToDirection(solution[0], solution[1]);
                        Direction d2 = PointToDirection(solution[1], solution[2]);
                        solution.RemoveAt(0);
                        commands[i] = ServerCommunicator.Push(d1, d2);
                    }
                }

                string response = ServerCommunicator.Command(commands); // Sends commands to server
                if (response.Contains("false"))
                    throw new Exception("Attempted illegal move.");
            }
        }

        // This is an asynchronous version of WriteCommands().
        public List<string[]> ReturnCommands()
        {
            var allCommands = new List<string[]>();
            while (!AgentReachedDestination())
            {
                g++; // Add turn count.
                string[] commands = new string[totalAgentCount];

                // Iterates through every agent and adds a command
                for (int i = 0; i < agents.Length; ++i)
                {
                    int agentIndex = (int)Char.GetNumericValue(agents[i].Type);

                    if (!priorities[i].HasValue) // Agent has no goal.
                    {
                        commands[agentIndex] = ServerCommunicator.NoOp();
                    }
                    else if (waits[i] != 0) // Agent should wait.
                    {
                        commands[agentIndex] = ServerCommunicator.NoOp();
                        waits[i]--;
                    }
                    else if (!priorities[i].Value.AgentIsNextToBox) // Agent moves to box
                    {

                        var solution = mappings[i].Value.Solution;
                        Direction d = PointToDirection(solution[0], solution[1]);
                        solution.RemoveAt(0);
                        commands[agentIndex] = ServerCommunicator.Move(d);
                    }
                    else // Push box to goal.
                    {
                        var solution = mappings[i].Value.Solution;
                        Direction d1 = PointToDirection(solution[0], solution[1]);
                        Direction d2 = PointToDirection(solution[1], solution[2]);
                        solution.RemoveAt(0);
                        commands[agentIndex] = ServerCommunicator.Push(d1, d2);
                    }
                }
                allCommands.Add(commands);
            }
            return allCommands;
        }

        public bool AgentReachedDestination()
        {
            for (int i = 0; i < priorities.Count; ++i)
                if (!priorities[i].HasValue)
                    continue;
                else if (priorities[i].Value.AgentIsNextToBox && mappings[i].Value.Solution.Count == 2)
                    return true;
                else if (!priorities[i].Value.AgentIsNextToBox && mappings[i].Value.Solution.Count == 1)
                    return true;
            return false;
        }

        public State CreateCurrentState()
        {
            // Set agents positions.
            for (int i = 0; i < agents.Length; ++i)
                if (mappings[i].HasValue)
                    agents[i] = new Entity(mappings[i].Value.Solution[0], agents[i].Color, agents[i].Type);

            // Set box positions.
            var boxes = currentState.GetBoxes(level);
            // The only boxes that can possibly have changed are those by our agents.
            for (int i = 0; i < agents.Length; ++i)
            {
                if (!priorities[i].HasValue || !priorities[i].Value.AgentIsNextToBox) continue;
                int boxIndex = MatchBox(i, boxes);
                boxes[boxIndex] = new Entity(mappings[i].Value.Solution[1], boxes[boxIndex].Color, boxes[boxIndex].Type);
            }

            return new State(currentState, agents.Concat(boxes.ToArray()).ToArray(), g);
        }

        public int MatchBox(int index, Span<Entity> boxes)
        {
            for (int i = 0; i < boxes.Length; ++i)
                if (priorities[index].Value.Box.Equals(boxes[i]))
                    return i;
            throw new Exception("Box search failed.");
        }

        public bool IsGoalState()
        {
            foreach (AgentBoxGoalPairs? pairs in priorities)
                if (pairs.HasValue)
                    return false;
            return true;
        }

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
