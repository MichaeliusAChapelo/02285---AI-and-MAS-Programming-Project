using BoxProblems;
using BoxProblems.Graphing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxTests
{
    [TestClass]
    public class TestNaiveSolver
    {
        // Increasing the threshold means more time to solve levels, that is,
        // Testing will take longer, but more levels will pass.
        // Adjust as necessary for your PC.
        private readonly int TimeoutThreshold = 50;// in miliseconds

        [TestMethod]
        public void TestSimpleLevels()
        {
            ServerCommunicator.SkipConsoleRead = true;
            TestLevel(@"Levels\New_Format\MAExample.lvl");
            TestLevel(@"Levels\New_Format\MAPullPush.lvl");
            TestLevel(@"Levels\New_Format\MAPullPush2.lvl");
            TestLevel(@"Levels\New_Format\MAFiveWalls.lvl", 200); // Setting specific time threshold.
            //TestLevel(@"Levels\New_Format\SAlabyrinthOfStBertin.lvl");
        }

        public void TestLevel(string levelPath) { TestLevel(levelPath, TimeoutThreshold); }
        public void TestLevel(string levelPath, int timeoutThreshold)
        {
            //ManualResetEvent mre = new ManualResetEvent(false);
            //Task task = Task.Factory.StartNew(() =>
            //{
            //    var solver = new NaiveSolver(Level.ReadLevel(File.ReadAllLines(levelPath)));
            //    solver.Solve(); // A most convenient function.
            //});

            //mre.WaitOne(timeoutThreshold);
            //if (!mre.WaitOne(0))
            //{
            //    task.Dispose();
            //    if (!NaiveSolver.Solved)
            //        Assert.Fail("Timeout");
            //}
            //task.Dispose();
        }
    }
}
