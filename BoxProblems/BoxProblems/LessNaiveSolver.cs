using BoxProblems.Solver;
using System;
using System.Collections.Generic;
using System.Linq;

//#nullable enable

namespace BoxProblems
{
    internal partial class LessNaiveSolver
    {
        private readonly Level Level;
        //private State CurrentState;
        private List<HighlevelMove> Plan;
        //int[] Waits;
        //private int g = 0;
        private Entity[] Agents;
        public static bool Solved { get; private set; }

        public LessNaiveSolver(Level wholeLevel, Level partialLevel, List<HighlevelMove> plan)
        {
            this.Level = partialLevel;
            Solved = false;
            this.Plan = plan;
            Agents = wholeLevel.GetAgents().ToArray();
        }

        // GOD FUNCTION DEUX
        public List<AgentCommands> Solve()
        {
            List<AgentCommands> solution = new List<AgentCommands>();

            State currentState = Level.InitialState;
            foreach (HighlevelMove plan in Plan)
            {
                int agentIndex = Array.IndexOf(Agents, plan.UsingThisAgent.HasValue ? plan.UsingThisAgent : plan.MoveThis);

                var commands = CreateOnlyFirstSolutionCommand(plan, currentState);

                currentState = plan.CurrentState;
                solution.Add(new AgentCommands(commands, agentIndex));

                //sData.CurrentState.Entities[toMoveIndex] = sData.CurrentState.Entities[toMoveIndex].Move(goal);

            }
            Solved = true;

            return solution;
        }

        #region Abstract Moves to Specific Commands

        public List<AgentCommand> CreateOnlyFirstSolutionCommand(HighlevelMove move, State currentState)
        {
            var box = move.MoveThis;

            // Make all blockages into "fake" walls
            foreach (Entity e in currentState.Entities)
                Level.AddWall(e.Pos);

            Level.RemoveWall(box.Pos);
            Level.RemoveWall(move.ToHere);

            List<AgentCommand> result;

            if (move.UsingThisAgent.HasValue)
            {
                Level.RemoveWall(move.UsingThisAgent.Value.Pos);
                result = CreateSolutionCommands(move, goal: new Entity(move.ToHere, box.Color, box.Type));
            }
            else
            {
                int agentIndex = Array.IndexOf(Agents, move.MoveThis);
                Agents[agentIndex] = move.MoveThis.Move(move.ToHere);
                result = MoveToLocation(box.Pos, move.ToHere);

            }


            Level.ResetWalls();
            return result;

            throw new Exception("High Level Move did not specify which agent to use.");
        }

        public List<AgentCommand> CreateSolutionCommands(HighlevelMove plan, Entity goal)
        {
            var agent = plan.UsingThisAgent.Value;
            var box = plan.MoveThis;

            int agentIndex = Array.IndexOf(Agents, agent);
            //if (agentIndex == -1) throw new Exception("WRONG NUMBER");

            var commands = new List<AgentCommand>();

            bool OnlyPullToGoal = false;

            // Does agent need to move to box position first?
            List<Point> toBox = null;
            Point agentPosNextToBox;
            if (Point.ManhattenDistance(agent.Pos, box.Pos) != 1)
            {
                toBox = RunAStar(agent.Pos, box.Pos);
                agentPosNextToBox = MoveToBox(toBox, commands, goal, out OnlyPullToGoal);
            }
            else
                agentPosNextToBox = agent.Pos;


            var toGoal = RunAStar(box.Pos, plan.AgentFinalPos.Value);
            if (!toGoal.Contains(goal.Pos))
                toGoal.Add(goal.Pos);

            bool ShouldPush = !toGoal.Contains(agentPosNextToBox); // always true right now

            //Console.WriteLine(Level.WorldToString(Level.GetWallsAsWorld()));


            if (ShouldPush && !toGoal.Contains(agentPosNextToBox))
                toGoal.Insert(0, agentPosNextToBox);

            Point? newAgentPosition = null;

            // Strategy: Check if need to pull: If yes, keep pulling until I can U-turn into pushing. Then push until end.
            if (!ShouldPush)
            {
                List<Point> parteUn = new List<Point>();
                List<Point> parteDeux = new List<Point>();

                if (OnlyPullToGoal)
                {

                    int index = toBox.IndexOf(goal.Pos);

                    if (index == -1)
                        newAgentPosition = agent.Pos;
                    else
                        newAgentPosition = toBox[index - 1];
                    if (!toGoal.Contains(newAgentPosition.Value))
                        toGoal.Add(newAgentPosition.Value);

                    for (int i = 2; i < toGoal.Count; ++i)
                        commands.Add(Pull(toGoal, i));
                }
                else
                    for (int i = 1; i < toGoal.Count; ++i)
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

            if (!newAgentPosition.HasValue)
                newAgentPosition = toGoal[toGoal.Count - 2];

            Level.AddWall(goal.Pos);
            //SetAgentPosition(agent, newAgentPosition); // Much more preferable, but troublesome. Wait till heuristics improve.

            Agents[agentIndex] = agent.Move(newAgentPosition.Value);

            //MoveToLocation(newAgentPosition.Value, agent.Pos, commands); // thats just how it is rite now i aint making them heuristics dawg

            Level.ResetWalls();

            return commands;
        }

        #region Solution Command Generation

        private Point MoveToBox(List<Point> toBox, List<AgentCommand> commands, Entity goal, out bool OnlyPullToGoal)
        {
            toBox.Remove(toBox.Last()); // Remove box' position from solution list

            OnlyPullToGoal = false;
            for (int i = 1; i < toBox.Count; ++i)
            {
                if (toBox[i] == goal.Pos)
                    OnlyPullToGoal = true;
                commands.Add(AgentCommand.CreateMove(PointToDirection(toBox[i - 1], toBox[i])));
            }
            return toBox.Last(); // Return agent's destination, so next to box.
        }

        private List<AgentCommand> MoveToLocation(Point agentFrom, Point destination)
        {
            var commands = new List<AgentCommand>();
            MoveToLocation(agentFrom, destination, commands);

            return commands;
        }

        private void MoveToLocation(Point agentFrom, Point destination, List<AgentCommand> commands)
        {
            if (agentFrom == destination) return; //Jeez, dawg. This ain't cool.

            var toDest = RunAStar(agentFrom, destination);
            for (int i = 1; i < toDest.Count; ++i)
                commands.Add(AgentCommand.CreateMove(PointToDirection(toDest[i - 1], toDest[i])));
        }

        private AgentCommand Push(List<Point> toGoal, int index)
        {
            Direction moveDirAgent = PointToDirection(toGoal[index - 2], toGoal[index - 1]);
            Direction moveDirBox = PointToDirection(toGoal[index - 1], toGoal[index]);
            return AgentCommand.CreatePush(moveDirAgent, moveDirBox);
        }

        private AgentCommand Pull(List<Point> toGoal, int index)
        {
            Direction currDirBox = PointToDirection(toGoal[index - 1], toGoal[index - 2]);
            Direction moveDirAgent = PointToDirection(toGoal[index - 1], toGoal[index]);
            return AgentCommand.CreatePull(moveDirAgent, currDirBox);
        }

        public static Direction PointToDirection(Point p1, Point p2)
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
        internal static readonly Point[] NeighboursPoints = new Point[4] { new Point(0, 1), new Point(-1, 0), new Point(1, 0), new Point(0, -1) };

        public List<Point> RunAStar(Point start, Point end)
        {
            if (start == end) return new List<Point>() { end }; //Jeez, dawg. This ain't cool.

            return Precomputer.GetPath(Level, start, end, false).ToList();
        }


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
            foreach (Point testDir in NeighboursPoints)
            {
                var p = agentPos + testDir;
                if (!solutionPath.Contains(p) && !Level.Walls[p.X, p.Y]) return p;
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }

        private Point FindSpaceToTurn(Point agentPos, Point boxPos, Point nextPoint)
        {
            foreach (Point testDir in NeighboursPoints)
            {
                var p = agentPos + testDir;
                if (p != boxPos && p != nextPoint && !Level.Walls[p.X, p.Y]) return p;
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }

        #endregion


    }
}