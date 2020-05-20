using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GZipTest
{
	abstract class ProcessingModuleBase : IProcessingModule
	{
		#region Variables
		Thread readingThread, writingThread;
		readonly Thread[] compressionThreads = new Thread[Math.Max(1, Environment.ProcessorCount - 2)];  // If we have ability to use more than 3 threads we add more threads that will proccess blocks (because this operation takes the biggest amount of resources)

		protected Semaphore processed;      // Semaphore will help us to maintain RAM and use minimum of it

		protected ConcurrentQueue<KeyValuePair<int, byte[]>> readBuffer = new ConcurrentQueue<KeyValuePair<int, byte[]>>();   // We use queue for reading and processing blocks since FIFO method is more efficient here
		protected ConcurrentDictionary<int, byte[]> processedBuffer = new ConcurrentDictionary<int, byte[]>();                // And use dictionary for writing since we need blocks to be placed in order

		// These variables are used for tracking progress
		protected long segmentCount = 1;
		long served = 0;

		// Source and output file paths
		protected string source, result;

		readonly DateTime start = DateTime.Now;

		public bool IsWorking { get; private set; }
		#endregion

		#region Methods
		/// <summary>
		/// Initializing workflow
		/// </summary>
		/// <param name="input">Source file path</param>
		/// <param name="output">Destination file path</param>
		public void Run(string input, string output)
		{
			IsWorking = true;
			// Setting files paths 
			source = input;
			result = output;

			// Instantiating threads
			readingThread = new Thread(Read);
			writingThread = new Thread(Write);

			for (int i = 0; i < compressionThreads.Length; i++)
				compressionThreads[i] = new Thread(Process)
				{
					Priority = ThreadPriority.Highest   // Since compression is the slowest operation it must be marked as high priority task
				};

			// Semaphore will indicate how many blocks can be now written. 
			// There can be max 5 blocks for each compression thread because there's no reason for more. 
			// 5 block in a row mean that compressing algorithm is faster than writing algorithm so there's no need to process more block until these are done
			processed = new Semaphore(compressionThreads.Length * 5, compressionThreads.Length * 5);

			// Starting threads
			readingThread.Start();
			foreach (Thread i in compressionThreads)
				i.Start();
			writingThread.Start();
		}

		/// <summary>
		/// Reads source file
		/// </summary>
		protected abstract void Read();

		/// <summary>
		/// Processes one block. This method is used in Read and Write threads
		/// </summary>`
		protected abstract void ProcessOne();

		/// <summary>
		/// Processing read block
		/// </summary>
		void Process()
		{
			while (readingThread.IsAlive || readBuffer.Count > 0)                                               // The task will be alive as long as reading is in progress or there's stil any unprocessed blocks
				ProcessOne();
		}

		/// <summary>
		/// Writes processed block to disk
		/// </summary>
		void Write()
		{
			using (FileStream stream = new FileStream(result, FileMode.Create, FileAccess.Write))   // Instantiating writing stream
			{
				while (compressionThreads.Any(i => i.IsAlive) || processedBuffer.Count > 0)         // The task will be alive as long as compression is in progress or there's stil any unwritten block
				{
					if (!processedBuffer.TryRemove((int)served, out byte[] block))                  // Extracting block that need to be written next 
					{
						if (readBuffer.Count > 0)                                                   // Helping processing thread to do its job
							ProcessOne();
						continue;
					}

					stream.Write(block, 0, block.Length);                                           // Writing block to the file
					processed.Release();                                                            // Informing compression threads that they can continue

					served++;                                                                       // Updating counter

					SetProgress();
				}
			}

			TimeSpan elapsed = DateTime.Now - start;
			Console.WriteLine($"\nDone\nFile processing is completed within within {elapsed.Minutes} minutes {elapsed.Seconds} seconds");
			IsWorking = false;
		}

		/// <summary>
		/// Draws a progress bar in output console and displays some information
		/// </summary>
		void SetProgress()
		{
			TimeSpan elapsed = DateTime.Now - start;
			//Border braces
			Console.CursorLeft = 0;
			Console.Write("[");
			Console.CursorLeft = 21;
			Console.Write("]");

			//Progress bar
			for (int i = 0; i < served * 20 / segmentCount; i++)
			{
				Console.CursorLeft = i + 1;
				Console.Write("■");
			}

			//Percentage
			Console.CursorLeft = 23;
			Console.Write($"{served * 100 / segmentCount}%	{served} ({segmentCount * 5} MB) of {segmentCount} ({segmentCount * 5} MB) blocks [{elapsed:hh\\:mm\\:ss}]");
		}
		#endregion
	}
}