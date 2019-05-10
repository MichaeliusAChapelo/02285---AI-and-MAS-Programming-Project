using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BoxProblems
{
    internal static class LevelSplitter
    {
        public static List<Level> SplitLevel(Level level)
        {
            List<Level> levels = new List<Level>();

            foreach (Entity box in level.GetBoxes())
            {
                level.AddWall(box.Pos);
            }

            GraphSearchData gsData = new GraphSearchData(level);
            Dictionary<Point, Entity> goalEntities = new Dictionary<Point, Entity>();
            foreach (var box in level.GetBoxes())
            {
                goalEntities.Add(box.Pos, box);
            }
            while (true)
            {
                bool foundNewBox = false;
                foreach (Entity agent in level.GetAgents())
                {
                    HashSet<Point> goals = new HashSet<Point>();
                    foreach (var box in goalEntities)
                    {
                        if (agent.Color == box.Value.Color)
                        {
                            goals.Add(box.Key);
                        }
                    }
                    List<Point> newBoxesFound = GraphSearcher.GetReachedGoalsBFS(gsData, level, agent.Pos, x => new GraphSearcher.GoalFound<Point>(x.pos, goals.Contains(x.pos)));
                    if (newBoxesFound.Count > 0)
                    {
                        foundNewBox = true;
                    }
                    
                    foreach (Point foundBoxPos in newBoxesFound)
                    {
                        level.RemoveWall(foundBoxPos);
                        goalEntities.Remove(foundBoxPos);
                    }
                }

                if (!foundNewBox)
                {
                    break;
                }
            }

            HashSet<Point> airGoals = new HashSet<Point>();
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
                List<Point> foundAir = GraphSearcher.GetReachedGoalsBFS(gsData, level, agent.Pos, x => new GraphSearcher.GoalFound<Point>(x.pos, airGoals.Contains(x.pos)));
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
                List<Entity> goals = new List<Entity>();

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
                levels.Add(new Level(walls, goals.ToArray(), initial, level.Width, level.Height, agents.Count, boxes.Count));
            }

            level.ResetWalls();
            return levels;
        }
    }
}
