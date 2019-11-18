using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class DecompressionModule : ProcessingModule
    {
        /// <summary>
        /// Reading compressed source file
        /// </summary>
        internal override void Read()
        {
            try
            {
                using (FileStream input = File.OpenRead(source))                                       //Opening reading stream
                {
                    byte[] segmentMeta = new byte[8];                                                   //Reading first 8 bytes to determine total count of blocks
                    input.Read(segmentMeta, 0, 8);
                    segmentCount = BitConverter.ToInt64(segmentMeta, 0);                                //segmentCount field will be used to display progress bar

                    for (int i = 0; input.Position < input.Length; i++)
                    {
                        if (readBuffer.Count >= 5 * Environment.ProcessorCount)                         //Helping decompression thread if there's too many unprocessed blocks
                        {
                            ProcessOne();
                            i--;
                            continue;
                        }

                        byte[] meta = new byte[4];                                                      //Reading first 4 bytes to determine block's length
                        input.Read(meta, 0, 4);
                        int blockSize = BitConverter.ToInt32(meta, 0);

                        byte[] block = new byte[blockSize];                                             //Instantiating empty block
                        input.Read(block, 0, blockSize);                                                //Reading next block

                        readBuffer.Enqueue(new KeyValuePair<int, byte[]>(i, block));                    //Adding read block to compression queue. Each block must contain its position number since compression is multi thread
                    }
                }
            }
            catch (Exception e)
            {
                ReportError(this, $"Error occured in Reading thread. Served blocks: {served}", e);
            }
        }

        internal override void ProcessOne()
        {
            if (!readBuffer.TryDequeue(out KeyValuePair<int, byte[]> block))                    //Extracting read block
                return;

            processed.WaitOne();                                                                //Waiting for empty place for compressed block

            using (MemoryStream stream = new MemoryStream(block.Value))                         //Instantiating memory stream with compressed block data
            using (GZipStream compressor = new GZipStream(stream, CompressionMode.Decompress))  //Instantiating decompressor stream
            using (MemoryStream destination = new MemoryStream())                               //Instantiating memory stream which will contain decompressed block
            {
                compressor.CopyTo(destination);                                                 //Decompressing block

                processedBuffer.TryAdd(                                                         //Processing block and adding it to write queue keeping its position number
                    block.Key,
                    destination.ToArray());
            }
        }
    }
}
