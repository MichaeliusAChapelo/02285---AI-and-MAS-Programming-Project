using BoxProblems.Solver;
using PriorityQueue;
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
            if (Level == BoxSwimmingSolver.Level) return BoxSwimmingSolver.EndSolution;

            List<AgentCommands> solution = new List<AgentCommands>();

            State currentState = Level.InitialState;
            foreach (HighlevelMove plan in Plan)
            {
                int agentIndex = Array.IndexOf(Agents, plan.UsingThisAgent.HasValue ? plan.UsingThisAgent.Value : plan.MoveThis);
                if (agentIndex == -1)
                {
                    throw new Exception("Failed to find the agent.");
                }

                //if (plan == Plan[39])
                //{ return solution; }

                var commands = CreateOnlyFirstSolutionCommand(plan, currentState, agentIndex);

                currentState = plan.CurrentState;

                if (plan.UsingThisAgent.HasValue)
                {
                    var a = plan.UsingThisAgent.Value.Move(plan.AgentFinalPos.Value);
                    var b = Agents[agentIndex];
                    if (b != a)
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
            //if (plan == Plan[75])
            //{ }

            List<AgentCommand> commands = new List<AgentCommand>();

            var agent = plan.UsingThisAgent.Value;
            var agentEndPos = plan.AgentFinalPos.Value;
            var box = plan.MoveThis;
            List<Point> agentToBox = null;

            // Avoids stupid square problems.
            if (Point.ManhattenDistance(box.Pos, goalPos) == 1)
            {
                if (IsStraightCorridor(goalPos) || IsStraightCorridor(box.Pos))
                {
                    // Do nothing
                }
                else if (box.Pos == agentEndPos)
                    // Force agent's pathfinding to box to avoid the goal.
                    Level.AddWall(goalPos);
                else if (Point.ManhattenDistance(agentEndPos, goalPos) == 1)
                { // Force agent's position next to box onto the goal.
                    Level.AddWall(box.Pos);
                    agentToBox = RunAStar(agent.Pos, goalPos);
                    agentToBox.Add(box.Pos);
                }
            }

            if (agentToBox == null)
            {
                agentToBox = RunAStar(agent.Pos, box.Pos);
                if (agentToBox.Count == 1 && 4 < Point.ManhattenDistance(agent.Pos, box.Pos))
                    throw new Exception("LessNaiveSolver could not find path to location.");
            }

            Level.RemoveWall(goalPos);
            Level.RemoveWall(box.Pos);

            //Remove box location from the path as the path should end next to the box
            agentToBox.RemoveAt(agentToBox.Count - 1);
            MoveToLocation(agentToBox, commands, agentIndex);
            Point agentNextToBox = agentToBox[agentToBox.Count - 1];

            var boxToAgentEnd = RunAStar(box.Pos, agentEndPos);

            if (boxToAgentEnd.Count == 3 && agentNextToBox == goalPos && boxToAgentEnd[1] != goalPos)
                boxToAgentEnd[1] = goalPos; // If pathfinding dun goofed, then force agent to pull through goal position when it OBVIOUSLY should.

            bool startPull = boxToAgentEnd.Contains(agentNextToBox);
            bool endPull = boxToAgentEnd.Contains(goalPos);

            if (agentNextToBox == goalPos) // If agent blocks goal position
                startPull = true; // Forces agent to locate distant U-turn location, pull at U-turn, and push to goal.

            List<Point> firstPart = null;
            List<Point> secondPart = null;

            if (startPull != endPull)
            {
                Point? turnPoint = null;
                Point turnIntoPoint = new Point();

                //Find somewhere to turn around
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


                // If we failed to find a turn point on the route
                if (!turnPoint.HasValue)
                    FindDistantTurningPoint(plan, ref agentNextToBox, agentEndPos, ref turnPoint, ref turnIntoPoint, box.Pos, goalPos, commands, agentIndex, boxIndex, ref boxToAgentEnd, ref startPull, ref endPull, ref count, ref skipFirst);

                turnIntoPoint = FindSpaceToTurn(boxToAgentEnd, turnPoint.Value, goalPos, agentNextToBox);

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
        }

        private void FindDistantTurningPoint(
            HighlevelMove plan,
            ref Point agentNextToBox,
            Point agentEndPos,
            ref Point? turnPoint,
            ref Point turnIntoPoint,
            Point boxPos,
            Point goalPos,
            List<AgentCommand> commands,
            int agentIndex,
            int boxIndex,
            ref List<Point> boxToAgentEnd,
            ref bool startPull,
            ref bool endPull,
            ref int count,
            ref int skipFirst
            )
        {
            //throw new Exception("Failed to find a turn point on the route.");
            var distMap = Precomputer.GetDistanceMap(Level.Walls, goalPos, false);

            var turningPoints = new PriorityQueue<(int x, int y), int>();
            int x, y;
            for (x = 1; x < Level.Width - 1; x++)
                for (y = 1; y < Level.Height - 1; y++)
                    if (!Level.Walls[x, y] && !IsCorridor(x, y))
                        turningPoints.Enqueue((x, y), distMap[x,y]);

            if (turningPoints.Count == 0)
            {
                // Try disable blocking agents
                foreach (Entity e in plan.CurrentState.GetAgents(Level))
                    Level.RemoveWall(e.Pos);

                for (x = 1; x < Level.Width - 1; x++)
                    for (y = 1; y < Level.Height - 1; y++)
                        if (!Level.Walls[x, y] && !IsCorridor(x, y))
                            turningPoints.Enqueue((x, y), distMap[x, y]);

                if (turningPoints.Count == 0)
                    throw new Exception("Failed to find any turning points in level, probably due to boxes.");
                else
                    throw new Exception("Failed to find any possible distant turning point, because of blockage by agents.");
            }
            (x, y) = turningPoints.DequeueWithPriority().Value;
            turnPoint = new Point(x, y);

            // Find spot beside turning point.
            foreach (Point dirDelta in Direction.NONE.DirectionDeltas())
            {
                Point dirP = turnPoint.Value + dirDelta;
                if (!Level.IsWall(dirP) && dirP != agentNextToBox)
                {
                    turnIntoPoint = turnPoint.Value + dirDelta;
                    break;
                }
            }

            // Reach turning point.
            var pathToTurnIntoPoint = RunAStar(boxPos, turnIntoPoint);

            // If we found no path to this turning point; Pick another.
            while (pathToTurnIntoPoint.Count == 1)
            { 
                if (turningPoints.Count == 0)
                    throw new Exception("Distant turning point exists, but none can be reached due to blockage by other boxes.");
                (x, y) = turningPoints.DequeueWithPriority().Value;
                turnPoint = new Point(x, y);

                // Find spot beside turning point.
                foreach (Point dirDelta in Direction.NONE.DirectionDeltas())
                    if (!Level.IsWall(turnPoint.Value + dirDelta))
                    {
                        turnIntoPoint = turnPoint.Value + dirDelta;
                        break;
                    }

                pathToTurnIntoPoint = RunAStar(boxPos, turnIntoPoint);
            }

            // If A* doesn't go through selected turn(into) points
            if (pathToTurnIntoPoint[pathToTurnIntoPoint.Count - 2] != turnPoint)
            {
                List<Point> blocks = new List<Point>();
                while (pathToTurnIntoPoint[pathToTurnIntoPoint.Count - 2] != turnPoint)
                {
                    Point p = pathToTurnIntoPoint[pathToTurnIntoPoint.Count - 2];
                    Level.AddWall(p);
                    blocks.Add(p);
                    pathToTurnIntoPoint = RunAStar(boxPos, turnIntoPoint);
                }
                foreach (Point p in blocks)
                    Level.RemoveWall(p);
            }




            startPull = !pathToTurnIntoPoint.Contains(agentNextToBox);

            if (startPull)
            {

                //if (!pathToTurnIntoPoint.Contains(agentNextToBox))
                PushOnPath(pathToTurnIntoPoint, agentNextToBox, commands, agentIndex, boxIndex);
                Agents[agentIndex] = Agents[agentIndex].Move(turnPoint.Value);
                boxToAgentEnd = RunAStar(turnIntoPoint, agentEndPos);
            }
            else
            {

                // SARegExAZ is an excellent example... of a mess.
                PullOnPath(pathToTurnIntoPoint, commands, agentIndex, boxIndex);
                Agents[agentIndex] = Agents[agentIndex].Move(turnIntoPoint);
                boxToAgentEnd = RunAStar(turnPoint.Value, agentEndPos);
            }

            agentNextToBox = Agents[agentIndex].Pos; // Agent is always next to box.
            endPull = boxToAgentEnd.Contains(goalPos);
            startPull = !endPull;

            count = 0;
            skipFirst = startPull ? 1 : 0;
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
            if (nextPos.X <= 0 || nextPos.X >= Level.Width - 1 ||
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
                    Console.Error.WriteLine(Level.StateToString(new State(null, Agents.Concat(Boxes).ToArray(), 0)));
                    throw new Exception("Can't move into an agent.");
                }
            }

            foreach (var box in Boxes)
            {
                if (box.Pos == nextPos)
                {
                    //Console.Error.WriteLine(Level.StateToString(new State(null, Agents.Concat(Boxes).ToArray(), 0)));
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

        public bool IsCorridor(int x, int y)
        {
            int walls = 0;
            if (Level.Walls[x + 1, y]) walls++;
            if (Level.Walls[x - 1, y]) walls++;
            if (Level.Walls[x, y + 1]) walls++;
            if (Level.Walls[x, y - 1]) walls++;
            return (2 <= walls);
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

        public bool IsStraightCorridor(Point p)
        {
            return (Level.Walls[p.X + 1, p.Y] && Level.Walls[p.X - 1, p.Y])
                || (Level.Walls[p.X, p.Y + 1] && Level.Walls[p.X, p.Y - 1]);
        }

        private Point FindSpaceToTurn(List<Point> solutionPath, Point turnPos, Point goalPos, Point agentNextToBox)
        {
            foreach (Point dirDelta in Direction.NONE.DirectionDeltas())
            {
                var p = turnPos + dirDelta;

                // Cannot U-turn box into agent's position next to box.
                if (p == agentNextToBox)
                    continue;

                if (!Level.Walls[p.X, p.Y] && !solutionPath.Contains(p) && p != goalPos)
                {
                    return p;
                }
            }
            throw new Exception("Agent pos was corridor, but no extra space was found.");
        }



    }
}