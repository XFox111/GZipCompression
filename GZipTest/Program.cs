using System;
using System.IO;

namespace GZipTest
{
    class Program
    {
        static DateTime start = DateTime.Now;
        static IProcessingModule module;

        static int Main(string[] args)
        {
            try
            {
                //Validating input parameters
                if (args.Length != 3)
                    throw new InvalidDataException("Invalid parameters set.\nUsage: NewWinRar.exe [compress|decompress] [source file name] [destination file name]");
                if (!File.Exists(args[1]))
                    throw new FileNotFoundException("The source file not found. Make sure it is place in the program's directory and has the same name. Stating extension is required");

                //Instatiating module
                switch (args[0].ToLower())
                {
                    case "compress":
                        Console.WriteLine("Compressing file...");
                        module = new CompressionModule();
                        break;
                    case "decompress":
                        Console.WriteLine("Unpacking file...");
                        module = new DecompressionModule();
                        break;
                    default:
                        throw new InvalidDataException("Invalid parameter. The first parameter must be 'compress' or 'decompress'");
                }

                //Subscribing to events
                module.ProgressChanged += SetProgress;
                module.Complete += Complete;
                module.ErrorOccured += Module_ErrorOccured;

                //Executing module
                module.Run(args[1], args[2]);

                return 0;
            }
            //Catching errors and displaying them
            catch (Exception e)
            {
                Console.Error.WriteLine($"\n\n{e.ToString()}\n" + e.InnerException != null && e.InnerException != e ? $"\n{e.InnerException.ToString()}\n" : "");
                return 1;
            }
        }

        private static void Module_ErrorOccured(object sender, ErrorEventArgs e)
        {
            Console.Error.WriteLine("Error has occured. Threads tremination initiated");
            Console.Error.WriteLine($"\n\n{e.GetException().ToString()}\n");
            module.Complete -= Complete;
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            module.Stop();
        }

        /// <summary>
        /// Displays complete message and post analysis
        /// </summary>
        /// <param name="size">Represents original file size in MB</param>
        /// <param name="e">Not used</param>
        private static void Complete(object size, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - start;
            Console.WriteLine($"\nDone\nProcessed {size} MB within {elapsed.Minutes} minutes {elapsed.Seconds} seconds\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Displaying progress bar which represents current workflow position
        /// </summary>
        /// <param name="percentage">Integer from 0 to 100. Represents amount of completed work</param>
        public static void SetProgress(long done, long totalSegments)
        {
            TimeSpan elapsed = DateTime.Now - start;
            //Border braces
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.CursorLeft = 21;
            Console.Write("]");

            //Progress bar
            for (int i = 0; i < done * 20 / totalSegments; i++)
            {
                Console.CursorLeft = i + 1;
                Console.Write("■");
            }

            //Percentage
            Console.CursorLeft = 23;
            Console.Write($"{done * 100 / totalSegments}%   {done} of {totalSegments} blocks [{elapsed.ToString(@"hh\:mm\:ss")}]");
        }
    }
}
