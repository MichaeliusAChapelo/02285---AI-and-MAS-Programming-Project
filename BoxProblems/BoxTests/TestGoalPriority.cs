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
++++++++";

            string levelPriorityString = @"
++++++++
+ +  11+
+  + 22+
+ + +  +
+    + +
+ ++++ +
+      +
++++++++";

            VerifyPriority(levelString, levelPriorityString);
        }

        [TestMethod]
        public void TestGoalPriority2()
        {
            string levelString = @"
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
+++++++++++";

            string levelPriorityString = @"
+++++++++++
+++++1+++++
+  3 2 3  +
+++ +1+ +++
+++  +  +++
++++ 4 ++++
+++++ +++++
+++++ +++++
++++   ++++
+++++ +++++
+++++++++++";

            VerifyPriority(levelString, levelPriorityString);
        }

        [TestMethod]
        public void TestGoalPriority3()
        {
            string levelString = @"
+++++
++B++
++ ++
++ ++
++ ++
+Aba+
+++++";

            string levelPriorityString = @"
+++++
++ ++
++ ++
++ ++
++ ++
+ 21+
+++++";

            VerifyPriority(levelString, levelPriorityString);
        }

        private static void VerifyPriority(string levelString, string levelPriorityString)
        {
            levelPriorityString = levelPriorityString.Substring(2, levelPriorityString.Length - 2);

            Level level = TestTools.StringToOldFormatLevel(levelString);
            GoalGraph goalGraph = new GoalGraph(level.InitialState, level);
            var actualGoalPriority = GoalPriority2.GetGoalPriority(level, goalGraph);

            //foreach (var item in actualGoalPriority)
            //{
            //    Console.WriteLine(item.Key.ToString() + "  " + item.Value);
            //}

            List<Entity> priorityEntities = new List<Entity>();
            int priority = 1;
            for (int i = 0; i < actualGoalPriority.Count; i++)
            {
                priorityEntities.AddRange(actualGoalPriority[i].Select(x => new Entity(x.Value.Ent.Pos, 0, (char)(priority + '0'))));
                priority++;
            }

            State priorityState = new State(null, priorityEntities.ToArray(), 0);
            string actualPriorityString = level.StateToString(priorityState);
            Assert.IsTrue(levelPriorityString == actualPriorityString, $"{Environment.NewLine}Expected:{Environment.NewLine}{levelPriorityString}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{actualPriorityString}");
        }
    }
}
