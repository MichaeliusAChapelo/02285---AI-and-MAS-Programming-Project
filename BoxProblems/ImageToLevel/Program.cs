using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace ImageToLevel
{
    class Program
    {
        static void Main(string[] args)
        {
            const string imagePath = @"C:\SALeo.bmp";
            const string outputPath = @"C:\Meine Items\Coding Ambitions\8. Semester\t.txt";

            Bitmap b = new Bitmap(imagePath);

            string[] content = new string[50];

            for (int y = 0; y < b.Height; y++)
            {
                char[] str = new char[50];
                for (int x = 0; x < b.Width; x++)
                    if (b.GetPixel(x, y).R == 0)
                        str[x] = '+';
                    else str[x] = 'L';
                content[y] = new string(str);
            }

            File.WriteAllLines(outputPath, content);

        }
    }

}
