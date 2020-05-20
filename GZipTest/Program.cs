using System;
using System.IO;

namespace GZipTest
{
	class Program
	{
		static IProcessingModule module;

		static int Main(string[] args)
		{
			// Validating input parameters
			if (args.Length < 1)	// If there's no parameters provided, display help
			{
				DisplayHelp();
				return 0;
			}

			// Instatiating module
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
				case "help":
					DisplayHelp();
					return 0;
				default:
					throw new InvalidDataException("Invalid parameter. The first parameter must be 'compress', 'decompress' or 'help'");
			}

			if (args.Length < 3)
				throw new InvalidDataException("Target file or destination file path missing. Type 'help' to get usage information");
			if (!File.Exists(args[1]))
				throw new FileNotFoundException("The source file not found. Check provided path and try again. Stating extension is required");

			//Executing module
			module.Run(input: args[1],
					   output: args[2]);

			while (module.IsWorking);	// Get UI thread busy while in progress

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();

			return 0;
		}

		/// <summary>
		/// Displays program descriptions and usage instructions
		/// </summary>
		static void DisplayHelp()
		{
			Console.WriteLine("Compresses or decompresses files. Compressed archives cannot be opened with other archivers.\n");
			Console.WriteLine("USAGE:\n" +
				"GZipTest [OPERATION] [SOURCE] [TARGET]\n");

			Console.WriteLine("Parameters:");
			Console.WriteLine("OPERATION \t Operation which will be executed by the program - compression or decompression. Required.");
			Console.WriteLine("\t Valid values: compress | decompress | help");

			Console.WriteLine("\nSOURCE \t\t Relative or absolute path to file which will be processed by the program. Required.");

			Console.WriteLine("\nTARGET \t\t Relative or absolute path to destination file which will be created after the program work. Required.");

			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
		}
	}
}