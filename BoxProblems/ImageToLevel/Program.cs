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
                    switch ((uint) b.GetPixel(x, y).ToArgb())
                    {
                        // 0xAARRGGBB
                        case 0xFF000000: // Black
                            str[x] = '+';
                            break;

                        case 0xFF7F3300: // Brown
                            str[x] = 'L';
                            break;

                        case 0xFFFF6A00: // Orange
                            str[x] = 'E';
                            break;

                        case 0xFFFF0000: // Red
                            str[x] = 'O';
                            break;

                        case 0xFFFFFFFF: // White
                            str[x] = ' ';
                            break;
                    }
                content[y] = new string(str);
            }
            File.WriteAllLines(outputPath, content);
        }
    }

}
