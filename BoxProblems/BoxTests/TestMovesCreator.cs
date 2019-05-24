using BoxProblems;
using BoxProblems.Solver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxTests
{
    [TestClass]
    public class TestMovesCreator
    {
        [TestMethod]
        public void TestPushPull()
        {
            string levelString = @"
++++++++++
+0B    GF+
+++++ ++++
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestPullPush()
        {
            string levelString = @"
++++++++++
+B0    FG+
+++++ ++++
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.S, Direction.W),
                AgentCommand.CreatePush(Direction.N, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestPushPush()
        {
            string levelString = @"
++++++++++
+0B    FG+
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestPullPull()
        {
            string levelString = @"
++++++++++
+B0    GF+
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestPushPullTurnLast()
        {
            string levelString = @"
++++++++++
+0B    GF+
+++++++ ++
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.S),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestPullPushTurnLast()
        {
            string levelString = @"
++++++++++
+B0    FG+
+++++++ ++
++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.S, Direction.W),
                AgentCommand.CreatePush(Direction.N, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToBoxPushPull()
        {
            string levelString = @"
++++++++++++
+0  B    GF+
+++++++ ++++
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPullPush()
        {
            string levelString = @"
++++++++++++
+B  0    FG+
+++++++ ++++
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.S, Direction.W),
                AgentCommand.CreatePush(Direction.N, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPushPush()
        {
            string levelString = @"
++++++++++++
+0  B    FG+
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPullPull()
        {
            string levelString = @"
++++++++++++
+B  0    GF+
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToBoxPushPullBent()
        {
            string levelString = @"
++++++++++++
+0++++++++++
+ ++++++++++
+ ++++++++++
+   B    GF+
+++++++ ++++
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.S),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPullPushBent()
        {
            string levelString = @"
++++++++++++
+B       FG+
+++++++ ++++
+++++++ ++++
+++++++0++++
++++++++++++
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.N),
                AgentCommand.CreateMove(Direction.N),
                AgentCommand.CreateMove(Direction.N),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.S, Direction.W),
                AgentCommand.CreatePush(Direction.N, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPushPushBent()
        {
            string levelString = @"
++++++++++++
+0++++++++++
+ ++++++++++
+ ++++++++++
+   B    FG+
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPullPullBent()
        {
            string levelString = @"
++++++++++++
++++0+++++++
++++ +++++++
++++ +++++++
+B       GF+
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
                AgentCommand.CreatePull(Direction.E, Direction.W),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentToPushPushUTurn()
        {
            string levelString = @"
++++++
+  0 +
+ BFG+
++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.W),
                AgentCommand.CreatePush(Direction.S, Direction.E),
                AgentCommand.CreatePush(Direction.E, Direction.E),
            };

            VerifyMoveBoxToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentMoveStraightPath()
        {
            string levelString = @"
++++++++++++
+          +
+0       G +
+          +
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
            };

            VerifyMoveAgentToGoalCreator(levelString, commands);
        }

        [TestMethod]
        public void TestAgentMoveBentLine()
        {
            string levelString = @"
++++++++++++
+ +     G  +
+0+++++++  +
+          +
++++++++++++";

            List<AgentCommand> commands = new List<AgentCommand>()
            {
                AgentCommand.CreateMove(Direction.S),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.E),
                AgentCommand.CreateMove(Direction.N),
                AgentCommand.CreateMove(Direction.N),
                AgentCommand.CreateMove(Direction.W),
            };

            VerifyMoveAgentToGoalCreator(levelString, commands);
        }

        private static void VerifyMoveBoxToGoalCreator(string levelString, List<AgentCommand> expectedCommands)
        {
            Level level = TestTools.StringToLevel(levelString);
            Level clearedLevel = TestTools.StringToLevel(levelString.Replace('G', 'b').Replace('F', ' '));

            Entity agent = level.InitialState.Entities.Single(x => x.Type == '0');
            Entity box = level.InitialState.Entities.Single(x => x.Type == 'B');
            Entity goal = level.InitialState.Entities.Single(x => x.Type == 'G');
            Point agentFinalPos = level.InitialState.Entities.Single(x => x.Type == 'F').Pos;

            HighlevelMove expectedMove = new HighlevelMove(clearedLevel.InitialState, box, goal.Pos, agent, agentFinalPos);

            var solution = ProblemSolver.SolveLevel(clearedLevel, TimeSpan.FromSeconds(3), false);
            Assert.AreEqual(1, solution.Count);
            Assert.AreEqual(1, solution.First().SolutionMovesParts.Count);
            HighlevelMove actualMove = solution.First().SolutionMovesParts.First();
            Assert.IsTrue(expectedMove == actualMove, $"Expected:{Environment.NewLine}{expectedMove}{Environment.NewLine}Actual:{Environment.NewLine}{actualMove}");

            VerifyCommands(clearedLevel, expectedMove, expectedCommands);
        }

        private static void VerifyMoveAgentToGoalCreator(string levelString, List<AgentCommand> expectedCommands)
        {
            Level level = TestTools.StringToLevel(levelString);
            Level clearedLevel = TestTools.StringToLevel(levelString.Replace('G', ' '));

            Entity agent = level.InitialState.Entities.Single(x => x.Type == '0');
            Entity goal = level.InitialState.Entities.Single(x => x.Type == 'G');

            HighlevelMove expectedMove = new HighlevelMove(clearedLevel.InitialState, agent, goal.Pos, null, null);

            VerifyCommands(clearedLevel, expectedMove, expectedCommands);
        }

        private static void VerifyCommands(Level level, HighlevelMove move, List<AgentCommand> expectedCommands)
        {
            List<AgentCommands> agentCommands = new LessNaiveSolver(level, level, new List<HighlevelMove>() { move }).Solve();
            List<AgentCommand> actualCommands = agentCommands.First().Commands;

            CollectionAssert.AreEqual(expectedCommands, actualCommands);
        }
    }
}
