using BoxProblems.Solver;
using System;
using System.Collections.Generic;
using System.Linq;

//#nullable enable

namespace BoxProblems
{
    internal class LessNaiveSolver
    {
        private readonly Level Level;
        private State CurrentState;
        private List<HighlevelMove> Plan;
        //int[] Waits;
        //private int g = 0;
        private Entity[] Agents;
        public static bool Solved { get; private set; }

        public LessNaiveSolver(Level level, List<HighlevelMove> plan)
        {
            this.Level = level;
            Solved = false;
            this.Plan = plan;
            Agents = Level.GetAgents().ToArray();
        }

        // GOD FUNCTION DEUX
        public void Solve()
        {

            foreach (HighlevelMove plan in Plan)
            //while (Plan.Count != 0)
            {
                // 1) Create current state
                //CurrentState = plan.CurrentState;

                //CurrentState = plan.CurrentState;
                //Agents = CurrentState.GetAgents(Level).ToArray();

                // 2) Manhatten A* to solution; This time, try to break the search
                // 3) Convert to high level moves to low level commands
                // Includes U-turning boxes (from pull to push)
                var commands = CreateOnlyFirstSolutionCommand(plan);

                // 4) Conflicts
                // Use the multiple solution paths to create a data structure mapping conflicts, then modify solutions.

                // 5) Loop through commands
                //int index = Array.IndexOf(Agents, plan.UsingThisAgent);
                int index = Array.IndexOf(Agents, plan.UsingThisAgent);
                OutputCommands(commands, index);

                // 6) Set agent positions proper.
                //SetAgentPosition();
                if (plan.UsingThisAgent == null)
                    Agents[index] = Agents[index].Move(plan.ToHere);

                //sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);

            }
            Solved = true;
        }

        // Temporary: Only moving one agent at a time.
        public void OutputCommands(List<string> commands, int agentIndex)
        {
            string[] output = new string[Agents.Length];

            foreach (string c in commands)
            {
                Array.Fill(output, ServerCommunicator.NoOp());
                output[agentIndex] = c;

                string response = ServerCommunicator.Command(output);
                if (response.Contains("false")) throw new Exception("Sent illegal command to server.");
            }

            //while (commands.Count != 0)
            //{
            //    for (int i = 0; i < Agents.Length; ++i)
            //        if (i == agentIndex)
            //        {
            //            output[i] = commands[0];
            //            commands.RemoveAt(0);
            //        }
            //        else
            //            output[i] = ServerCommunicator.NoOp();

            //    string response = ServerCommunicator.Command(output);
            //    if (response.Contains("false")) throw new Exception("Sent illegal command to server.");
            //}

        }

        #region Abstract Moves to Specific Commands

        public List<string> CreateOnlyFirstSolutionCommand(HighlevelMove move)
        {
            var box = move.MoveThis;
            //Plan.Remove(move);

            //Console.Error.WriteLine("MOVING TO LOCATION" + move.ToHere);
            //Console.Error.WriteLine("");

            // Make all blockages into "fake" walls
            foreach (Entity e in move.CurrentState.Entities)
                Level.AddWall(e.Pos);

            List<string> result;


            if (move.UsingThisAgent.HasValue)
                result = CreateSolutionCommands(agent: move.UsingThisAgent.Value, box, goal: new Entity(move.ToHere, box.Color, box.Type));
            else
                result = MoveToLocation(box.Pos, move.ToHere);


            Level.ResetWalls();
            return result;

            //for (agentIndex = 0; agentIndex < Agents.Length; ++agentIndex)
            //    if (move.UsingThisAgent == Agents[agentIndex])
            //        return CreateSolutionCommands(agent: Agents[agentIndex], box: box, goal: new Entity(move.ToHere, box.Color, box.Type));

            throw new Exception("High Level Move did not specify which agent to use.");
        }

        //public void CreateAllSolutionCommands() // Don't use until you're ready to run multiple agents, calculate conflicts, and perhaps parallellize.
        //{
        //    foreach (Entity agent in Agents)
        //        foreach (HighlevelMove move in Plan)
        //            if (agent == move.UsingThisAgent)
        //            {
        //                var box = move.MoveThis;
        //                CreateSolutionCommands(move.UsingThisAgent.Value, box,
        //                    new Entity(move.ToHere, box.Color, box.Type));
        //                break;
        //            }
        //}

        public List<string> CreateSolutionCommands(Entity agent, Entity box, Entity goal)
        {
            Level.RemoveWall(agent.Pos);
            Level.RemoveWall(box.Pos);
            Level.RemoveWall(goal.Pos);

            var commands = new List<string>();
            DistanceMap = new int[Level.Width, Level.Height];
            DistanceMapOffset = 0;

            // Does agent need to move to box position first?
            Point agentPosNextToBox;
            if (Point.ManhattenDistance(agent.Pos, box.Pos) != 1)
                agentPosNextToBox = MoveToBox(agent, box, ref commands);
            else
                agentPosNextToBox = agent.Pos;

            var toGoal = RunAStar(box.Pos, goal.Pos);
            bool ShouldPush = !toGoal.Contains(agentPosNextToBox); // always true right now
            if (ShouldPush)
                toGoal.Insert(0, agentPosNextToBox);

            // Strategy: Check if need to pull: If yes, keep pulling until I can U-turn into pushing. Then push until end.
            if (!ShouldPush)
            {
                List<Point> parteUn = new List<Point>();
                List<Point> parteDeux = new List<Point>();

                for (int i = 0; i < toGoal.Count; ++i)
                    if (!IsCorridor(toGoal[i]))
                    {
                        Point turnTo = FindSpaceToTurn(toGoal, toGoal[i]);
                        parteUn.AddRange(toGoal.Take(i + 1));
                        parteUn.Add(turnTo);

                        for (int j = 2; j < parteUn.Count; ++j)
                            commands.Add(Pull(parteUn, j));

                        // Push
                        parteDeux.Add(turnTo);
                        parteDeux.AddRange(toGoal.Skip(i));

                        for (int j = 2; j < parteDeux.Count; ++j)
                            commands.Add(Push(parteDeux, j));
                        break;
                    }
            }
            else
                for (int i = 2; i < toGoal.Count; i++)
                    commands.Add(Push(toGoal, i));

            Point newAgentPosition = toGoal[toGoal.Count - 2];

            Level.AddWall(goal.Pos);
            //SetAgentPosition(agent, newAgentPosition); // Much more preferable, but troublesome. Wait till heuristics improve.
            MoveToLocation(newAgentPosition, agent.Pos, ref commands); // thats just how it is rite now i aint making them heuristics dawg

            Level.ResetWalls();

            return commands;
        }

        #region Solution Command Generation

        private Point MoveToBox(Entity agent, Entity box, ref List<string> commands)
        {
            var toBox = RunAStar(agent.Pos, box.Pos);

            toBox.Remove(toBox.Last()); // Remove box' position from solution list
            for (int i = 1; i < toBox.Count; ++i)
                commands.Add(ServerCommunicator.Move(NaiveSolver.PointToDirection(toBox[i - 1], toBox[i])));
            DistanceMapOffset = commands.Count;
            return toBox.Last(); // Return agent's destination, so next to box.
        }

        private List<string> MoveToLocation(Point agentFrom, Point destination)
        {
            var commands = new List<string>();
            MoveToLocation(agentFrom, destination, ref commands);
            return commands;
        }

        private void MoveToLocation(Point agentFrom, Point destination, ref List<string> commands)
        {
            var toDest = RunAStar(agentFrom, destination);
            for (int i = 1; i < toDest.Count; ++i)
                commands.Add(ServerCommunicator.Move(NaiveSolver.PointToDirection(toDest[i - 1], toDest[i])));
        }

        private string Push(List<Point> toGoal, int index)
        {
            Direction moveDirAgent = NaiveSolver.PointToDirection(toGoal[index - 2], toGoal[index - 1]);
            Direction moveDirBox = NaiveSolver.PointToDirection(toGoal[index - 1], toGoal[index]);
            return ServerCommunicator.Push(moveDirAgent, moveDirBox);
        }

        private string Pull(List<Point> toGoal, int index)
        {
            Direction currDirBox = NaiveSolver.PointToDirection(toGoal[index - 1], toGoal[index - 2]);
            Direction moveDirAgent = NaiveSolver.PointToDirection(toGoal[index - 1], toGoal[index]);
            return ServerCommunicator.Pull(moveDirAgent, currDirBox);
        }

        #endregion

        #region A* Search Algorithm

        private int[,] DistanceMap;
        private int DistanceMapOffset = 0;
        private readonly Point[] neighboursPoints = new Point[4] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };

        public List<Point> RunAStar(Point start, Point end)
        {
            return Precomputer.GetPath(Level, start, end, false).ToList();

            // LOL


            //var frontier = new PriorityQueue<Point, int>();
            //var cameFrom = new Dictionary<Point, Point>();
            //frontier.Enqueue(start, Point.ManhattenDistance(start, end));

            //Point currentPoint;
            //while (frontier.Count != 0)
            //{
            //    currentPoint = frontier.Dequeue();
            //    if (currentPoint == end)
            //        return ReconstructPath(cameFrom, currentPoint);

            //    foreach (Point neighbourPoint in neighboursPoints)
            //    {
            //        var p = currentPoint + neighbourPoint;
            //        if (cameFrom.ContainsValue(p) || cameFrom.ContainsKey(p))
            //            continue;
            //        else if (!IsWall(p))
            //        {
            //            frontier.Enqueue(p, Point.ManhattenDistance(p, end));
            //            cameFrom.Add(p, currentPoint);
            //        }
            //        else
            //            DistanceMap[p.X, p.Y] = int.MaxValue; // Set to high value to indicate wall.
            //    }
            //}
            //throw new Exception("Could not find path to desired location.");
            ////return null;


        }

        public List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
        {
            var TotalPath = new List<Point>() { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                TotalPath.Add(current);
            }
            TotalPath.Reverse();

            for (int i = 0; i < TotalPath.Count; ++i)
                DistanceMap[TotalPath[i].X, TotalPath[i].Y] = i + DistanceMapOffset;

            return TotalPath;
        }

        private bool IsWall(Point p) { return Level.Walls[p.X, p.Y]; }

        #endregion


        public bool IsCorridor(Point p)
        { // This works for corridor corners too.
            int walls = 0;
            if (Level.Walls[p.X + 1, p.Y]) walls++;
            if (Level.Walls[p.X - 1, p.Y]) walls++;
            if (Level.Walls[p.X, p.Y + 1]) walls++;
            if (Level.Walls[p.X, p.Y - 1]) walls++;
            return (2 <= walls);
        }

        private Point FindSpaceToTurn(List<Point> solutionPath, Point agentPos)
        {
            foreach (Point testDir in neighboursPoints)
            {
                var p = agentPos + testDir;
                if (!solutionPath.Contains(p) && !Level.Walls[p.X, p.Y]) return p;
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }

        private Point FindSpaceToTurn(Point agentPos, Point boxPos, Point nextPoint)
        {
            foreach (Point testDir in neighboursPoints)
            {
                var p = agentPos + testDir;
                if (p != boxPos && p != nextPoint && !Level.Walls[p.X, p.Y]) return p;
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }

        #endregion


    }
}