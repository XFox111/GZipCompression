using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    /// <summary>
    /// Delegate void used to inform UI thread about changed progress
    /// </summary>
    /// <param name="done">Amount of blocks that have been done</param>
    /// <param name="totalSegments">Amount of total blocks</param>
    public delegate void ProgressChangedEventHandler(long done, long totalSegments);

    public abstract class ProcessingModule : IProcessingModule
    {
        public event ProgressChangedEventHandler ProgressChanged;
        public event EventHandler Complete;
        public event ErrorEventHandler ErrorOccured;

        internal Thread readingThread, writingThread;
        internal Thread[] compressionThreads = new Thread[Math.Max(1, Environment.ProcessorCount - 2)];  //If we have ability to use more than 3 threads we add more threads that will proccess blocks (because this operation takes the biggest amount of resources)

        internal Semaphore processed;      //Semaphore will help us to maintain RAM and use minimum of it

        internal ConcurrentQueue<KeyValuePair<int, byte[]>> readBuffer = new ConcurrentQueue<KeyValuePair<int, byte[]>>();   //We use queue for reading and processing blocks since FIFO method is more efficient here
        internal ConcurrentDictionary<int, byte[]> processedBuffer = new ConcurrentDictionary<int, byte[]>();                //And use dictionary for writing since we need blocks to be placed in order

        //These variables are used for tracking progress
        internal long segmentCount = 0;
        internal long served = 0;
        internal long length;

        //Source and output file paths
        internal string source, result;

        /// <summary>
        /// Initializing workflow
        /// </summary>
        /// <param name="input">Source file path</param>
        /// <param name="output">Destination file path</param>
        public void Run(string input, string output)
        {
            //Setting files paths 
            source = input;
            result = output;

            //Instantiating threads
            readingThread = new Thread(Read);
            writingThread = new Thread(Write);

            for (int i = 0; i < compressionThreads.Length; i++)
                compressionThreads[i] = new Thread(Process);

            foreach (Thread i in compressionThreads)
                i.Priority = ThreadPriority.Highest;    //Since compression is the slowest operation it must be marked as high priority task

            //Semaphore will indicate how many blocks can be now written. 
            //There can be max 5 blocks for each compression thread because there's no reason for more. 
            //5 block in a row mean that compressing algorithm is faster than writing algorithm so there's no need to process more block until these are done
            processed = new Semaphore(compressionThreads.Length * 5, compressionThreads.Length * 5);

            //Starting threads
            readingThread.Start();
            foreach (Thread i in compressionThreads)
                i.Start();
            writingThread.Start();
        }

        /// <summary>
        /// Instantly terminates all threads and cleans up stuff
        /// </summary>
        public void Stop()
        {
            //Terminating threads
            readingThread.Abort();
            foreach (Thread thread in compressionThreads)
                thread.Abort();
            writingThread.Abort();

            //Collecting garbage (Yours' Cap)
            GC.Collect();
        }
        internal void ReportError(object sender, string message, Exception ex) => ErrorOccured?.Invoke(sender, new ErrorEventArgs(new Exception(message, ex)));

        /// <summary>
        /// Reading source file
        /// </summary>
        internal abstract void Read();

        /// <summary>
        /// Processes one block. This method is used in Read and Write threads
        /// </summary>
        internal abstract void ProcessOne();

        /// <summary>
        /// Processing read block
        /// </summary>
        internal void Process()
        {
            try
            {
                while (readingThread.IsAlive || readBuffer.Count > 0)                                               //The task will be alive as long as reading is in progress or there's stil any unprocessed blocks
                    ProcessOne();
            }
            catch (Exception e)
            {
                ReportError(this, $"Error occured in Compression thread. Served blocks: {served}", e);
            }
        }

        /// <summary>
        /// Writing processed block to disk
        /// </summary>
        internal void Write()
        {
            try
            {
                using (FileStream stream = new FileStream(result, FileMode.Create, FileAccess.Write))   //Instantiating writing stream
                {
                    while (compressionThreads.Any(i => i.IsAlive) || processedBuffer.Count > 0)         //The task will be alive as long as compression is in progress or there's stil any unwritten block
                    {
                        if (!processedBuffer.TryRemove((int)served, out byte[] block))                  //Extracting block that need to be written next 
                        {
                            if (readBuffer.Count > 0)                                                   //Helping processing thread to do its job
                                ProcessOne();
                            continue;
                        }

                        stream.Write(block, 0, block.Length);                                           //Writing block to the file
                        processed.Release();                                                            //Informing compression threads that they can continue

                        ProgressChanged?.Invoke(++served, segmentCount);                                //Updating progress bar
                    }
                }
                Complete?.Invoke(length / 1024 / 1024, null);
            }
            catch (Exception e)
            {
                ReportError(this, $"Error occured in writing thread. Blocks served: {served}", e);
            }
        }
    }
}
