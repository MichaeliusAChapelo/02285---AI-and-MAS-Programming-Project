using BoxProblems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoxTests
{
    internal static class TestTools
    {
        internal static Level LoadOldFormatLevel(string folder, string levelName)
        {
            string[] levelLines = File.ReadAllLines(Path.Combine("Levels/Old_Format", folder, levelName));
            return Level.ReadOldFormatLevel(levelLines, levelName);
        }

        internal static Level StringToOldFormatLevel(string levelstring)
        {
            return Level.ReadOldFormatLevel(levelstring.Replace("\r", string.Empty)
                                                       .Split('\n')
                                                       .ToList()
                                                       .Where(x => x.Length > 0)
                                                       .ToArray(), "default test level name");
        }
    }
}
