using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace NewWinRar
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                    throw new Exception("Invalid parameters set.\nUsage: NewWinRar.exe [compress|decompress] [source file] [destination file]");

                for (int i = 0; i < args.Length; i++)
                    args[i] = args[i].ToLower();

                switch (args[0])
                {
                    case "compress":
                        Compress(args[1], args[2]);
                        break;
                    case "decompress":
                        Decompress(args[1], args[2]);
                        break;
                    default:
                        throw new Exception("Invalid parameter. The first parameter must be 'compress' or 'decompress'");
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n\n{e.ToString()}\n");
                return 1;
            }
        }

        public static void Compress(string source, string result)
        {
            
        }

        public static void Decompress(string source, string result)
        {

        }
    }
}
