﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace BoxProblems
{
    internal enum Direction : byte
    {
        N = 0,
        W = 1,
        E = 2,
        S = 3,
        NONE = 4,
    }

    class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    string strategy = "-astar";
            //    string level = "MAExample.lvl";
            //    System.Diagnostics.Process.Start("cmd.exe", $"/c start powershell.exe java -jar server.jar -l {level} -c 'dotnet BoxProblems.dll {strategy}' -g 150 -t 300");
            //}

            //Level leavel = Level.ReadLevel(File.ReadAllLines("SplitExample2.lvl"));
            Level level = Level.ReadOldFormatLevel(File.ReadAllLines("Levels/Old_Format/initial_levels/SADangerBot.lvl"), "asdas");

            GoalGraph graph = new GoalGraph(level.InitialState, level);
            GoalPriority goalPriority = new GoalPriority();
            var goalPriorities = goalPriority.GetGoalPrioity(graph);
            foreach (GoalPriority.PriorityGoal gp in goalPriorities)
            {
                Console.WriteLine("First goal priority: " + gp.Type + " " + gp.Priority);
            }

            //GraphShower.ShowGraph(graph);


            //Console.WriteLine(leavel);
            ////Console.WriteLine("Hello World!");
            //var levels = LevelSplitter.SplitLevel(leavel);
            //levels.ForEach(x => Console.WriteLine(x));
            Console.Read();
        }
    }
}
