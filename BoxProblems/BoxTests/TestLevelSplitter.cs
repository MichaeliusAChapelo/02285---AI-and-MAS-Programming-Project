using BoxProblems;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxTests
{
    [TestClass]
    public class TestLevelSplitter
    {
        [TestMethod]
        public void TestSplitLevel1()
        {
            string levelString = @"
red: 0, A
blue: 1, B
green: C
+++++++
+a  A0+
+CCCCC+
+b  B1+
+++++++";
            string[] expectedLevels = new string[]
            {
                @"
+++++++
+++++++
+++++++
+b  B1+
+++++++",
                @"
+++++++
+a  A0+
+++++++
+++++++
+++++++"
            };

            VerifyLevelSplittedCorrectly(levelString, expectedLevels);
        }

        private static void VerifyLevelSplittedCorrectly(string levelToSplit, string[] expectedLevelStrings)
        {
            Level level = Level.ReadLevel(levelToSplit);
            Level[] actualLevels = LevelSplitter.SplitLevel(level).ToArray();
            string[] actuallevelStrings = actualLevels.Select(x => TestTools.RemoveInvisibleCharacters(x.ToString())).ToArray();

            foreach (var splittedLevel in expectedLevelStrings)
            {
                string expected = TestTools.RemoveInvisibleCharacters(splittedLevel);
                int index = Array.IndexOf(actuallevelStrings, expected);
                Assert.IsTrue(index != -1, $"Failed to find the expected splitted level. Expected: {Environment.NewLine}{splittedLevel}{Environment.NewLine}{Environment.NewLine}Level was split up into the following levels: {string.Join(Environment.NewLine + Environment.NewLine, actualLevels.Select(x => x.ToString()))}");
            }
        }
    }
}
