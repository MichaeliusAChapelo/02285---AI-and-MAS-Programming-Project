﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems
{
    internal static class LevelSplitter
    {
        public List<Level> SplitLevel(Level level)
        {
            List<Level> levels = new List<Level>();

            foreach (Entity box in level.GetBoxes())
            {
                level.Walls[box.Pos.X, box.Pos.Y] = true;
            }

            List<Entity> goalEntities = new List<Entity>();
            goalEntities.AddRange(level.GetBoxes().ToArray());
            while (true)
            {
                bool foundNewBox = false;
                foreach (Entity agent in level.GetAgents())
                {
                    List<Point> goals = new List<Point>();
                    foreach (Entity box in goalEntities)
                    {
                        if (agent.Color == box.Color)
                        {
                            goals.Add(box.Pos);
                        }
                    }
                    List<Point> newBoxesFound = GraphSearcher.GetReachedGoalsBFS(level, agent.Pos, goals);
                    if (newBoxesFound.Count > 0)
                    {
                        foundNewBox = true;
                    }

                    goalEntities.RemoveAll(x => newBoxesFound.Contains(x.Pos));
                    foreach (Point foundBoxPos in newBoxesFound)
                    {
                        level.Walls[foundBoxPos.X, foundBoxPos.Y] = false;
                    }
                }

                if (!foundNewBox)
                {
                    break;
                }
            }

            List<Point> airGoals = new List<Point>();
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (!level.Walls[x, y])
                    {
                        airGoals.Add(new Point(x, y));
                    }
                }
            }

            List<HashSet<Point>> levelParts = new List<HashSet<Point>>();
            foreach (Entity agent in level.GetAgents())
            {
                List<Point> foundAir = GraphSearcher.GetReachedGoalsBFS(level, agent.Pos, airGoals);
                bool levelPartAlreadyFound = false;
                foreach (HashSet<Point> levelPart in levelParts)
                {
                    if (levelPart.Contains(foundAir[0]))
                    {
                        levelPartAlreadyFound = true;
                        break;
                    }
                }

                if (!levelPartAlreadyFound)
                {
                    levelParts.Add(foundAir.ToHashSet());
                }
            }

            foreach (var levelPart in levelParts)
            {
                bool[,] walls = new bool[level.Width, level.Height];
                for (int y = 0; y < level.Height; y++)
                {
                    for (int x = 0; x < level.Width; x++)
                    {
                        if (!levelPart.Contains(new Point(x, y)))
                        {
                            walls[x, y] = true;
                        }
                    }
                }

                List<Entity> agents = new List<Entity>();
                List<Entity> boxes = new List<Entity>();
                List<Goal> goals = new List<Goal>();

                foreach (var agent in level.GetAgents())
                {
                    if (levelPart.Contains(agent.Pos))
                    {
                        agents.Add(agent);
                    }
                }
                foreach (var box in level.GetBoxes())
                {
                    if (levelPart.Contains(box.Pos))
                    {
                        boxes.Add(box);
                    }
                }
                foreach (var goal in level.Goals)
                {
                    if (levelPart.Contains(goal.Pos))
                    {
                        goals.Add(goal);
                    }
                }

                Entity[] entities = new Entity[agents.Count + boxes.Count];
                agents.CopyTo(entities);
                boxes.CopyTo(entities, agents.Count);

                State initial = new State(null, entities, 0);
                levels.Add(new Level(walls, goals.ToArray(), initial, level.Width, level.Height, agents.Count, boxes.Count);
            }

            foreach (Entity box in level.GetBoxes())
            {
                level.Walls[box.Pos.X, box.Pos.Y] = false;
            }

            return levels;
        }
    }
}