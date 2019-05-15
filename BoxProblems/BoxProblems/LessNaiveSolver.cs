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
        private List<HighlevelMove> Plan;
        private Entity[] Agents;
        private Entity[] Boxes;

        public LessNaiveSolver(Level wholeLevel, Level partialLevel, List<HighlevelMove> plan)
        {
            this.Level = partialLevel;
            this.Plan = plan;
            this.Agents = wholeLevel.GetAgents().ToArray();
            this.Boxes = wholeLevel.GetBoxes().ToArray();
        }

        // GOD FUNCTION DEUX
        public List<AgentCommands> Solve()
        {
            List<AgentCommands> solution = new List<AgentCommands>();

            State currentState = Level.InitialState;
            foreach (HighlevelMove plan in Plan)
            {
                int agentIndex = Array.IndexOf(Agents, plan.UsingThisAgent.HasValue ? plan.UsingThisAgent.Value : plan.MoveThis);
                if (agentIndex == -1)
                {
                    throw new Exception("Failed to find the agent.");
                }

                var commands = CreateOnlyFirstSolutionCommand(plan, currentState, agentIndex);

                currentState = plan.CurrentState;

                if (plan.UsingThisAgent.HasValue)
                {
                    if (Agents[agentIndex] != plan.UsingThisAgent.Value.Move(plan.AgentFinalPos.Value))
                    {
                        throw new Exception("reee");
                    }
                }
                else
                {
                    if (Agents[agentIndex].Pos != plan.ToHere)
                    {
                        throw new Exception("reeee");
                    }
                }

                solution.Add(new AgentCommands(commands, agentIndex));
            }

            return solution;
        }

        #region Abstract Moves to Specific Commands

        public List<AgentCommand> CreateOnlyFirstSolutionCommand(HighlevelMove move, State currentState, int agentIndex)
        {
            var box = move.MoveThis;

            // Make all blockages into "fake" walls
            foreach (Entity e in currentState.Entities)
                Level.AddWall(e.Pos);

            Level.RemoveWall(box.Pos);
            Level.RemoveWall(move.ToHere);

            List<AgentCommand> result = new List<AgentCommand>();

            if (move.UsingThisAgent.HasValue)
            {
                int boxIndex = Array.IndexOf(Boxes, move.MoveThis);
                Level.RemoveWall(move.UsingThisAgent.Value.Pos);
                result = CreateSolutionCommands(move, move.ToHere, agentIndex, boxIndex);
            }
            else
            {
                var agentToPos = RunAStar(move.MoveThis.Pos, move.ToHere);
                MoveToLocation(agentToPos, result, agentIndex);
            }

            Level.ResetWalls();
            return result;
        }

        public List<AgentCommand> CreateSolutionCommands(HighlevelMove plan, Point goalPos, int agentIndex, int boxIndex)
        {
            List<AgentCommand> commands = new List<AgentCommand>();

            var agent = plan.UsingThisAgent.Value;
            var agentEndPos = plan.AgentFinalPos.Value;
            var box = plan.MoveThis;

            var agentToBox = RunAStar(agent.Pos, box.Pos);
            //Remove box location from the path as the path should end next to the box
            agentToBox.RemoveAt(agentToBox.Count - 1);
            MoveToLocation(agentToBox, commands, agentIndex);
            Point agentNextToBox = agentToBox[agentToBox.Count - 1];

            var boxToAgentEnd = RunAStar(box.Pos, agentEndPos);

            bool startPull = boxToAgentEnd.Contains(agentNextToBox);
            bool endPull = boxToAgentEnd.Contains(goalPos);

            List<Point> firstPart = null;
            List<Point> secondPart = null;
            if (startPull != endPull)
            {
                //Find somewhere to turn around
                Point? turnPoint = null;
                int count = 0;

                int skipFirst = startPull ? 1 : 0;
                foreach (var pos in boxToAgentEnd.Skip(skipFirst))
                {
                    count++;
                    if (!IsCorridor(pos))
                    {
                        turnPoint = pos;
                        break;
                    }
                }
                count += skipFirst;

                if (!turnPoint.HasValue)
                {
                    throw new Exception("Failed to find a turn point on the route.");
                }

                Point turnIntoPoint = FindSpaceToTurn(boxToAgentEnd, turnPoint.Value, goalPos);

                firstPart = new List<Point>();
                firstPart.AddRange(boxToAgentEnd.Take(count));
                firstPart.Add(turnIntoPoint);

                secondPart = new List<Point>();
                secondPart.Add(turnIntoPoint);
                secondPart.AddRange(boxToAgentEnd.Skip(count - 1));
                if (!endPull)
                {
                    secondPart.Add(goalPos);
                }
                if (startPull && !endPull)
                {
                    secondPart.RemoveAt(0);
                }
            }
            else
            {
                firstPart = new List<Point>();
                firstPart.AddRange(boxToAgentEnd);
                if (!startPull)
                {
                    firstPart.Add(goalPos);
                }
            }

            //if (firstPart != null)
            //{
            //    Console.WriteLine("first part");
            //    State state = new State(null, Agents.Concat(Boxes).ToArray(), 0);
            //    LevelVisualizer.PrintPath(Level, state, firstPart);
            //}

            Point? firstPartAgentEndPos = null;
            if (startPull)
            {
                PullOnPath(firstPart, commands, agentIndex, boxIndex);
                if (secondPart != null)
                {
                    firstPartAgentEndPos = firstPart.Last();
                }
            }
            else
            {
                PushOnPath(firstPart, agentNextToBox, commands, agentIndex, boxIndex);
                if (secondPart != null)
                {
                    firstPartAgentEndPos = firstPart[firstPart.Count - 2];
                }
            }

            //if (secondPart != null)
            //{
            //    Console.WriteLine("second part");
            //    State state = new State(null, Agents.Concat(Boxes).ToArray(), 0);
            //    LevelVisualizer.PrintPath(Level, state, secondPart);
            //}
            if (secondPart != null)
            {
                if (endPull)
                {
                    PullOnPath(secondPart, commands, agentIndex, boxIndex);
                }
                else
                {
                    PushOnPath(secondPart, firstPartAgentEndPos.Value, commands, agentIndex, boxIndex);
                }
            }

            return commands;

            /*
            int agentIndex = Array.IndexOf(Agents, agent);

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

            if (newAgentPosition != plan.AgentFinalPos)
            {
                commands.AddRange(MoveToLocation(newAgentPosition.Value, plan.AgentFinalPos.Value));
                newAgentPosition = plan.AgentFinalPos;
            }

            Agents[agentIndex] = agent.Move(newAgentPosition.Value);

            Level.ResetWalls();

            return commands;
            */
        }

        private void PushOnPath(List<Point> path, Point agentPos, List<AgentCommand> commandList, int agentIndex, int boxIndex)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Point currentBoxPos = path[i];
                Point nextBoxPos = path[i + 1];

                Direction agentDir = PointsToDirection(agentPos, currentBoxPos);
                Direction boxDir = PointsToDirection(currentBoxPos, nextBoxPos);

                commandList.Add(AgentCommand.CreatePush(agentDir, boxDir));
                agentPos = currentBoxPos;

                MoveBox(nextBoxPos, boxIndex);
                MoveAgent(currentBoxPos, agentIndex);
            }
        }

        private void PullOnPath(List<Point> path, List<AgentCommand> commandList, int agentIndex, int boxIndex)
        {
            for (int i = 0; i < path.Count - 2; i++)
            {
                Point currentBoxPos = path[i];
                Point nextBoxPos = path[i + 1];

                Point agentPos = path[i + 1];
                Point nextAgentPos = path[i + 2];

                Direction agentDir = PointsToDirection(agentPos, nextAgentPos);
                Direction boxDir = PointsToDirection(nextBoxPos, currentBoxPos);

                commandList.Add(AgentCommand.CreatePull(agentDir, boxDir));

                MoveAgent(nextAgentPos, agentIndex);
                MoveBox(nextBoxPos, boxIndex);
            }
        }

        #region Solution Command Generation

        private void MoveToLocation(List<Point> path, List<AgentCommand> commands, int agentIndex)
        {
            for (int i = 0; i < path.Count - 1; ++i)
            {
                Point agentPos = path[i];
                Point nextAgentPos = path[i + 1];

                Direction agentDir = PointsToDirection(agentPos, nextAgentPos);

                commands.Add(AgentCommand.CreateMove(agentDir));

                MoveAgent(nextAgentPos, agentIndex);
            }
        }

        private void MoveAgent(Point nextAgentPos, int agentIndex)
        {
            VerifyIsMoveValid(nextAgentPos);
            Agents[agentIndex] = Agents[agentIndex].Move(nextAgentPos);
        }

        private void MoveBox(Point nextBoxPos, int boxIndex)
        {
            VerifyIsMoveValid(nextBoxPos);
            Boxes[boxIndex] = Boxes[boxIndex].Move(nextBoxPos);
        }

        private void VerifyIsMoveValid(Point nextPos)
        {
            if (nextPos.X <= 0 || nextPos.X >= Level.Width  - 1 ||
                nextPos.Y <= 0 || nextPos.Y >= Level.Height - 1)
            {
                throw new Exception("Can't move outside the bounds of the level.");
            }

            if (Level.IsWall(nextPos))
            {
                throw new Exception("Can't move into a wall.");
            }

            foreach (var agent in Agents)
            {
                if (agent.Pos == nextPos)
                {
                    //Console.WriteLine(Level.StateToString(new State(null, Agents.Concat(Boxes).ToArray(), 0)));
                    throw new Exception("Can't move into an agent.");
                }
            }

            foreach (var box in Boxes)
            {
                if (box.Pos == nextPos)
                {
                    //Console.WriteLine(Level.StateToString(new State(null, Agents.Concat(Boxes).ToArray(), 0)));
                    throw new Exception("Can't move into a box.");
                }
            }
        }

        public static Direction PointsToDirection(Point start, Point end)
        {
            Point delta = end - start;

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

        public List<Point> RunAStar(Point start, Point end)
        {
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

        private Point FindSpaceToTurn(List<Point> solutionPath, Point turnPos, Point goalPos)
        {
            foreach (Point dirDelta in Direction.NONE.DirectionDeltas())
            {
                var p = turnPos + dirDelta;
                if (!Level.Walls[p.X, p.Y] && !solutionPath.Contains(p) && p != goalPos)
                {
                    return p;
                }
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }

        #endregion


    }
}