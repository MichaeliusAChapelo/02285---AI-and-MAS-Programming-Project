using PriorityQueue;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    internal class StateSearchSolver
    {
        private Level level;
        private List<AgentCommand> allPossible;

        public StateSearchSolver(Level level)
        {
            this.level = level;
            CreateAllPossibleMoves();
        }

        private void CreateAllPossibleMoves()
        {
            if (allPossible != null) return;
            var all = new List<AgentCommand>();

            Direction[] directions = new Direction[4] { Direction.N, Direction.E, Direction.S, Direction.W };
            foreach (Direction d in directions)
            {
                all.Add(AgentCommand.CreateMove(d));
                foreach (Direction d2 in directions)
                {
                    if (d == d2)
                        all.Add(AgentCommand.CreatePush(d, d2));
                    else if (d2.Opposite() == d)
                        all.Add(AgentCommand.CreatePull(d, d2));
                    else
                    {
                        all.Add(AgentCommand.CreatePull(d, d2));
                        all.Add(AgentCommand.CreatePush(d, d2));
                    }
                }
            }
            allPossible = all;
        }

        private List<SearchState> GetExtendedStates(SearchState state)
        {
            var newStates = new List<SearchState>();

            foreach (AgentCommand c in allPossible)
            {
                Point newAgentPos = state.state.GetAgents(level)[0].Pos + c.AgentDir.DirectionDelta();

                if (c.CType == CommandType.MOVE)
                { // if newpos is free
                    foreach (Entity box in state.state.GetBoxes(level))
                        level.AddWall(box.Pos);
                    if (!level.IsWall(newAgentPos))
                    {
                        State n = state.state.GetCopy();
                        n.GetAgents(level)[0] = n.GetAgents(level)[0].Move(newAgentPos);
                        newStates.Add(new SearchState(n, c, state, state.depth + 1));
                    }
                    level.ResetWalls();
                }
                else if (c.CType == CommandType.PUSH)
                {
                    // get box at new pos
                    Entity? box = null;
                    foreach (Entity b in state.state.GetBoxes(level))
                        if (b.Pos == newAgentPos)
                            box = b;

                    if (box.HasValue)
                    {
                        Point newBoxPos = box.Value.Pos + c.BoxDir.DirectionDelta();

                        foreach (Entity b in state.state.GetBoxes(level))
                            level.AddWall(b.Pos);

                        // if found free spot
                        if (!level.IsWall(newBoxPos))
                        {
                            State n = state.state.GetCopy();
                            n.GetAgents(level)[0] = n.GetAgents(level)[0].Move(newAgentPos);

                            for (int i = 0; i < n.GetBoxes(level).Length; ++i)
                                if (n.GetBoxes(level)[i].Pos == box.Value.Pos)
                                {
                                    n.GetBoxes(level)[i] = n.GetBoxes(level)[i].Move(newBoxPos);
                                    break;
                                }
                            newStates.Add(new SearchState(n, c, state, state.depth + 1));
                        }
                        level.ResetWalls();
                    }
                }
                else if (c.CType == CommandType.PULL)
                {
                    // if new pos is free
                    foreach (Entity box in state.state.GetBoxes(level))
                        level.AddWall(box.Pos);
                    if (!level.IsWall(newAgentPos))
                    {
                        level.ResetWalls();

                        // if boxDir has box
                        Point boxPos = state.state.GetAgents(level)[0].Pos + c.BoxDir.DirectionDelta();
                        for (int i = 0; i < state.state.GetBoxes(level).Length; ++i)
                        {
                            if (state.state.GetBoxes(level)[i].Pos == boxPos)
                            {
                                State n = state.state.GetCopy();
                                n.GetAgents(level)[0] = n.GetAgents(level)[0].Move(newAgentPos);
                                n.GetBoxes(level)[i] = n.GetBoxes(level)[i].Move(boxPos);
                                newStates.Add(new SearchState(n, c, state, state.depth + 1));
                            }
                        }
                    }
                    level.ResetWalls();
                }
                level.ResetWalls();

            }

            return newStates;
        }

        internal class SearchState
        {
            public readonly State state;
            public readonly AgentCommand? agentCommand;
            public readonly SearchState cameFrom;
            public readonly int depth;

            public SearchState(State state, AgentCommand? agentCommand, SearchState cameFrom, int depth)
            {
                this.state = state;
                this.agentCommand = agentCommand;
                this.cameFrom = cameFrom;
                this.depth = depth;
            }

            public override string ToString()
            {
                return agentCommand.Value.ToString();
            }
        }

        public List<AgentCommands> Solve(int agentIndex)
        {
            return new List<AgentCommands>() { new AgentCommands(RunBFS(), agentIndex) };
        }

        private List<AgentCommand> RunBFS()
        {
            SearchState start = new SearchState(level.InitialState, null, null, 0);
            var ClosedSet = new List<State>();
            //var OpenSet = new List<SearchState>();
            var OpenSet = new Dictionary<int, List<SearchState>>();

            for (int i = 0; i <= level.Goals.Length; ++i)
                OpenSet.Add(i, new List<SearchState>());

            OpenSet[level.Goals.Length].Add(start);

            while (OpenSet.Count != 0)
            {
                SearchState current = null;
                int goalsNotResolved = level.Goals.Length;

                for (goalsNotResolved = 0; goalsNotResolved <= level.Goals.Length; goalsNotResolved++)
                {
                    if (OpenSet[goalsNotResolved].Count == 0) continue;
                    current = OpenSet[goalsNotResolved][0];
                    break;
                }
                if (current == null)
                    return null;

                //var current = OpenSet[0];

                if (IsGoalState(current.state))
                    return ReconstructPath(current);

                OpenSet[goalsNotResolved].RemoveAt(0);
                ClosedSet.Add(current.state);
                foreach (SearchState searchState in GetExtendedStates(current))
                {
                    if (ClosedSet.Contains(searchState.state))
                        continue;
                    foreach (SearchState ss in OpenSet[goalsNotResolved])
                        if (ss.state == searchState.state)
                            continue;

                    OpenSet[GoalsCompleted(searchState.state)].Add(searchState);
                }
            }
            return null;
        }

        private List<AgentCommand> ReconstructPath(SearchState start)
        {
            var totalPath = new List<AgentCommand>() { start.agentCommand.Value };
            SearchState next = start;
            while (next.cameFrom != null)
            {
                next = next.cameFrom;
                if (next.agentCommand.HasValue)
                    totalPath.Add(next.agentCommand.Value);
            }
            totalPath.Reverse();
            return totalPath;
        }

        private int GoalsCompleted(State s)
        {
            int goalCount = level.Goals.Length;

            foreach (Goal g in level.Goals)
                foreach (Entity e in s.Entities)
                    if (g.Ent.Pos == e.Pos && g.Ent.Type == e.Type && g.Ent.Color == e.Color)
                        goalCount--;
            return goalCount;
        }

        private bool IsGoalState(State s)
        {
            int goalCount = level.Goals.Length;

            foreach (Goal g in level.Goals)
                foreach (Entity e in s.Entities)
                    if (g.Ent.Pos == e.Pos && g.Ent.Type == e.Type && g.Ent.Color == e.Color)
                        goalCount--;
            return goalCount == 0;
        }
    }
}
