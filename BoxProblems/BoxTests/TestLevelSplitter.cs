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
++++++
+a  A+
+CCCC+
+b  B+
++++++";
            string[] expectedLevels = new string[]
            {
                @"
++++++
++++++
++++++
+b  B+
++++++",
                @"
++++++
+a  A+
++++++
++++++
++++++"
            };

            VerifyLevelSplittedCorrectly(levelString, expectedLevels);
        }

        private static void VerifyLevelSplittedCorrectly(string levelToSplit, string[] expectedLevelStrings)
        {
            Level level = Level.ReadOldFormatLevel(levelToSplit, "default test level name");
            Level[] actualLevels = LevelSplitter.SplitLevel(level).ToArray();

            foreach (var splittedLevel in expectedLevelStrings)
            {
                Assert.IsTrue(actualLevels.Any(x => x.ToString() == splittedLevel), $"Failed to find the expected splitted level. Expected: {Environment.NewLine}{splittedLevel}{Environment.NewLine}{Environment.NewLine}Level was split up into the following levels: {string.Join(Environment.NewLine + Environment.NewLine, actualLevels.Select(x => x.ToString()))}");
            }
        }
    }
}
