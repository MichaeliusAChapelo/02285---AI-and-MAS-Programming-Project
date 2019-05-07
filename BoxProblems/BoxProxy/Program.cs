using System;
using ProcessCommunication;

namespace BoxProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected one arguments as input");
            }

            TwoWayCommunication com = TwoWayCommunication.StartClientFirst(args[0]);
            while (true)
            {
                Console.WriteLine(com.ReadLine());
                com.WriteLine(Console.ReadLine());
            }
        }
    }
}
