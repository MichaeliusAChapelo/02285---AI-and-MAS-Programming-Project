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
            Level level = TestTools.StringToOldFormatLevel(levelString);

            Entity agent = level.InitialState.Entities.Single(x => x.Type == '0');
            Entity box = level.InitialState.Entities.Single(x => x.Type == 'B');
            Entity goal = level.InitialState.Entities.Single(x => x.Type == 'G');
            Point agentFinalPos = level.InitialState.Entities.Single(x => x.Type == 'F').Pos;

            HighlevelMove move = new HighlevelMove(level.InitialState, box, goal.Pos, agent, agentFinalPos);

            VerityCommands(level, move, expectedCommands);
        }

        private static void VerifyMoveAgentToGoalCreator(string levelString, List<AgentCommand> expectedCommands)
        {
            Level level = TestTools.StringToOldFormatLevel(levelString);

            Entity agent = level.InitialState.Entities.Single(x => x.Type == '0');
            Entity goal = level.InitialState.Entities.Single(x => x.Type == 'G');

            HighlevelMove move = new HighlevelMove(level.InitialState, agent, goal.Pos, null, null);

            VerityCommands(level, move, expectedCommands);
        }

        private static void VerityCommands(Level level, HighlevelMove move, List<AgentCommand> expectedCommands)
        {
            List<AgentCommands> agentCommands = new LessNaiveSolver(level, level, new List<HighlevelMove>() { move }).Solve();
            List<AgentCommand> actualCommands = agentCommands.First().Commands;

            CollectionAssert.AreEqual(expectedCommands, actualCommands);
        }
    }
}
