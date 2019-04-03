using BoxProblems;
using BoxProblems.Graphing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxTests
{
    [TestClass]
    public class TestGoalPriority
    {
        [TestMethod]
        public void TestGoalPrioritySACrunch()
        {
            string levelString = @"
++++++++
+0+  cb+
+ A+ ad+
+ +B+  +
+  DC+ +
+ ++++ +
+      +
++++++++
";
            Point[] priority = new Point[]
            {
                new Point(5, 1),
                new Point(6, 1),
                new Point(5, 2),
                new Point(6, 2)
            };

            VerifyPriority(levelString, priority);
        }

        [TestMethod]
        public void TestGoalPriority2()
        {
            string levelString = @"
+++++++++++
+++++++++++
+++++a+++++
+B c e b C+
+++ +d+ +++
+++  +  +++
++++ d ++++
+++++0+++++
+++++A+++++
++++D D++++
+++++E+++++
+++++++++++
";
            Point[] priority = new Point[]
            {
                new Point(5, 2),
                new Point(5, 4),
                new Point(5, 3),
                new Point(4, 3),
                new Point(6, 3),
                new Point(5, 6)
            };

            VerifyPriority(levelString, priority);
        }

        private static void VerifyPriority(string levelString, Point[] expectedGoalPriority)
        {
            Level level = TestTools.StringToOldFormatLevel(levelString);
            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            var actualGoalPriority = GoalPriority2.GetGoalPriority(level, goalGraph);

            for (int i = 0; i < expectedGoalPriority.Length - 1; i++)
            {
                Point higher = expectedGoalPriority[i + 1];
                Point lessOrEqual = expectedGoalPriority[i];

                GoalNode higherNode = (GoalNode)goalGraph.Nodes.Single(x => x.Value.Ent.Pos == higher);
                GoalNode lessOrEqualNode = (GoalNode)goalGraph.Nodes.Single(x => x.Value.Ent.Pos == lessOrEqual);

                var higherPriority = actualGoalPriority[higherNode];
                var lessOrEqualPriority = actualGoalPriority[lessOrEqualNode];

                Assert.IsTrue(higherPriority >= lessOrEqualPriority, $"Expected goal AZ to have a higher priority than Goal BZ{Environment.NewLine}" +
                    $"AZ: Goal: {higherNode}, Position: {higher}, Priority: {higherPriority}{Environment.NewLine}" +
                    $"BZ: Goal: {lessOrEqualNode}, Position: {lessOrEqual}, Priority: {lessOrEqualPriority}");
            }
        }
    }
}
