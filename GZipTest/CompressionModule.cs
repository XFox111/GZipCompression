using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
	class CompressionModule : ProcessingModuleBase
	{
		/// <summary>
		/// Reading uncompressed source file
		/// </summary>
		protected override void Read()
		{
			using FileStream input = File.OpenRead(source);                                         // Opening reading stream
			segmentCount = (long)Math.Ceiling((double)input.Length / 1048576);                      // segmentCount field will be used to display progress bar

			for (int i = 0; input.Position < input.Length; i++)
			{
				if (readBuffer.Count >= 5 * Environment.ProcessorCount)                             // Helping compression thread if there's too many unprocessed blocks
				{
					ProcessOne();
					i--;
					continue;
				}

				int blockSize = (int)Math.Min(1048576, input.Length - input.Position);              // Determining new block size. Either 1MB or count of the last bytes

				byte[] block = new byte[blockSize];                                                 // Instantiating empty block
				input.Read(block, 0, blockSize);                                                    // Reading next block

				readBuffer.Enqueue(new KeyValuePair<int, byte[]>(i, block));                        // Adding read block to compression queue. Each block must contain its position number since compression is multi thread
			}
		}

		protected override void ProcessOne()
		{
			if (!readBuffer.TryDequeue(out KeyValuePair<int, byte[]> block))                                // Extracting read block
				return;

			processed.WaitOne();                                                                            // Waiting for empty place for compressed block

			using MemoryStream stream = new MemoryStream();                                                 // Instatiating memory stream which will contain compressed block
			using GZipStream compressor = new GZipStream(stream, CompressionMode.Compress);                 // Instantiating compression stream

			compressor.Write(block.Value, 0, block.Value.Length);                                           // Compressing block
			compressor.Close();

			byte[] compressedBlock = stream.ToArray();                                                      // Getting compressed block
			byte[] fileMeta = block.Key == 0 ? BitConverter.GetBytes(segmentCount) : new byte[0];           // If it's the first block in a file we write info about total block count (that will be used to count progress)
			byte[] zippedMeta = BitConverter.GetBytes(compressedBlock.Length);                              // Creating compressed block length info

			byte[] newBlock = new byte[fileMeta.Length + 4 + compressedBlock.Length];                       // Merging arrays
			fileMeta.CopyTo(newBlock, 0);
			zippedMeta.CopyTo(newBlock, fileMeta.Length);
			compressedBlock.CopyTo(newBlock, fileMeta.Length + 4);

			processedBuffer.TryAdd(block.Key, newBlock);                                                    // Processing block and adding it to write queue keeping its position number
		}
	}
}