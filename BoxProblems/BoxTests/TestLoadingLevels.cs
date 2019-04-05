using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using BoxProblems;

namespace BoxTests
{
    [TestClass]
    public class TestLoadingLevels
    {
        [TestMethod]
        public void TestLoadingSACrunch()
        {
            Level level = TestTools.LoadOldFormatLevel("initial_levels", "SACrunch.lvl");
            //Entity[] entities = new Entity[]
            //{
            //    new Entity()
            //};

            //VerifyInitialEntitiesAreSame(level, new E)
        }

        private static void VerifyInitialEntitiesAreSame(Level level, Entity[] entitties)
        {

        }
    }
}
