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
            return Level.ReadLevel(levelLines, levelName);
        }

        internal static Level StringToOldFormatLevel(string levelString)
        {
            return Level.ReadLevel(levelString);
        }

        internal static string RemoveInvisibleCharacters(string text)
        {
            return text.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }
    }
}
