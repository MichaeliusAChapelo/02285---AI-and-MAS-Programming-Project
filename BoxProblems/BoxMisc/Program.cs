using BoxProblems;
using System;
using System.IO;

namespace BoxMisc
{
    class Program
    {
        public static void ConvertFiles(string startPath, string path, string savePath)
        {
            string[] files = Directory.GetFiles(Path.Combine(startPath, path));
            string[] directories = Directory.GetDirectories(Path.Combine(startPath, path));

            if (!Directory.Exists(Path.Combine(savePath, path)))
            {
                Directory.CreateDirectory(Path.Combine(savePath, path));
            }

            foreach (var file in files)
            {
                string[] oldFormat = File.ReadAllLines(file);
                string[] newFormat = Level.ConvertToNewFormat(oldFormat, Path.GetFileNameWithoutExtension(file));
                string fileSavePath = Path.Combine(savePath, path, Path.GetFileName(file));
                File.WriteAllLines(fileSavePath, newFormat);
                Console.WriteLine($"Converted {Path.GetFileName(file)}");
            }

            foreach (var directory in directories)
            {
                string directoryName = Path.GetFileName(directory);
                ConvertFiles(startPath, Path.Combine(path, directoryName), savePath);
            }
        }

        static void Main(string[] args)
        {
            string oldFormatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Levels", "Old_Format");
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Levels", "Old_To_New_Format");

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            ConvertFiles(oldFormatPath, string.Empty, savePath);
            Console.WriteLine("Converting done");
            Console.Read();
            return;
        }
    }
}
