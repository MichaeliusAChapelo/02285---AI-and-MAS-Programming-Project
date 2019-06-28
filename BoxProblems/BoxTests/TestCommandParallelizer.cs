using BoxProblems;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxTests
{
    [TestClass]
    public class TestCommandParallelizer
    {
        [TestMethod]
        public void TestMoveAgent()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E)
                }, 0)
            };

            string[] expectedPar = new string[]
            {
                AgentCommand.CreateMove(Direction.E).ToString(),
                AgentCommand.CreateMove(Direction.E).ToString(),
                AgentCommand.CreateMove(Direction.E).ToString()
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0') });
        }

        [TestMethod]
        public void TestMoveAgentBackAndForth()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E)
                }, 0),
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.W),
                    AgentCommand.CreateMove(Direction.W),
                    AgentCommand.CreateMove(Direction.W)
                }, 0)
            };

            string[] expectedPar = new string[]
            {
                AgentCommand.CreateMove(Direction.E).ToString(),
                AgentCommand.CreateMove(Direction.E).ToString(),
                AgentCommand.CreateMove(Direction.E).ToString(),
                AgentCommand.CreateMove(Direction.W).ToString(),
                AgentCommand.CreateMove(Direction.W).ToString(),
                AgentCommand.CreateMove(Direction.W).ToString()
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0') });
        }

        [TestMethod]
        public void TestMoveBox()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreatePush(Direction.E, Direction.E),
                    AgentCommand.CreatePush(Direction.E, Direction.E),
                    AgentCommand.CreatePush(Direction.E, Direction.E)
                }, 0)
            };

            string[] expectedPar = new string[]
            {
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString(),
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString(),
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString()
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0') });
        }

        [TestMethod]
        public void TestMoveBoxBackAndForth()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreatePush(Direction.E, Direction.E),
                    AgentCommand.CreatePush(Direction.E, Direction.E),
                    AgentCommand.CreatePush(Direction.E, Direction.E)
                }, 0),
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreatePull(Direction.W, Direction.E),
                    AgentCommand.CreatePull(Direction.W, Direction.E),
                    AgentCommand.CreatePull(Direction.W, Direction.E)
                }, 0)
            };

            string[] expectedPar = new string[]
            {
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString(),
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString(),
                AgentCommand.CreatePush(Direction.E, Direction.E).ToString(),
                AgentCommand.CreatePull(Direction.W, Direction.E).ToString(),
                AgentCommand.CreatePull(Direction.W, Direction.E).ToString(),
                AgentCommand.CreatePull(Direction.W, Direction.E).ToString()
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0') });
        }

        [TestMethod]
        public void TestMoveAgentsNoCollision()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E)
                }, 0),
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E),
                    AgentCommand.CreateMove(Direction.E)
                }, 1)
            };

            string[] expectedPar = new string[]
            {
                ToCommand(AgentCommand.CreateMove(Direction.E), AgentCommand.CreateMove(Direction.E)),
                ToCommand(AgentCommand.CreateMove(Direction.E), AgentCommand.CreateMove(Direction.E)),
                ToCommand(AgentCommand.CreateMove(Direction.E), AgentCommand.CreateMove(Direction.E))
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0'), new Entity(0, 1, 0, '1') });
        }

        [TestMethod]
        public void TestMoveAgentsBehindEachOther()
        {
            List<AgentCommands> commands = new List<AgentCommands>()
            {
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    //AgentCommand.CreateMove(Direction.E),
                    //AgentCommand.CreateMove(Direction.E)
                }, 0),
                new AgentCommands(new List<AgentCommand>()
                {
                    AgentCommand.CreateMove(Direction.E),
                    //AgentCommand.CreateMove(Direction.E),
                    //AgentCommand.CreateMove(Direction.E)
                }, 1)
            };

            string[] expectedPar = new string[]
            {
                ToCommand(AgentCommand.CreateMove(Direction.E), AgentCommand.NoOp()),
                ToCommand(AgentCommand.NoOp(), AgentCommand.CreateMove(Direction.E))
            };

            VarifyParallelization(commands, expectedPar, new Entity[] { new Entity(0, 0, 0, '0'), new Entity(-1, 0, 0, '1') });
        }

        private static void VarifyParallelization(List<AgentCommands> commands, string[] expectedPar, Entity[] agents)
        {
            string[] actualPar = CommandParallelizer.Parallelize2(commands, agents, new Entity[0]);
            CollectionAssert.AreEqual(expectedPar, actualPar);
        }

        private static string ToCommand(params object[] cmds)
        {
            return ServerCommunicator.CreateCommand(cmds.Select(x => x.ToString()).ToArray());
        }
    }
}
