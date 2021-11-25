using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Spark;

namespace Milk2
{
    internal static class Program
    {
        // private const string ReplayFile = @"C:\Users\Anton\Sync\Milk Testing\rec_2021-11-13_00-00-32.echoreplay";
        private const string ReplayFile = @"C:\Users\Anton\Sync\Milk Testing\rec_2021-11-12_23-48-58.echoreplay";

        private const string OutputFolder = @"C:\Users\Anton\Sync\Milk Testing";

        private static void Main(string[] args)
        {
            Console.WriteLine($"Reading file: {ReplayFile}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            StreamReader reader = new StreamReader(ReplayFile);

            List<g_Instance> file = ReadReplayFile(reader);
            // Thread loadThread = new Thread(() => ReadReplayFile(reader));
            // loadThread.Start();
            sw.Stop();
            Console.WriteLine($"Finished reading {file.Count:N0} lines in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();
            
            Milk milkV1 = new Milk(file[0]);
            file.ForEach(frame => { milkV1.AddFrame(frame); });
            byte[] milkV1Bytes = milkV1.GetBytes();
            
            sw.Stop();
            Console.WriteLine($"Finished converting to Milk V1 in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();

            MilkV2 milkV2 = new MilkV2();
            file.ForEach(frame => { milkV2.AddFrame(frame); });
            byte[] milkV2Bytes = milkV2.GetBytes();
            
            sw.Stop();
            Console.WriteLine($"Finished converting to Milk V2 in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();
            
            File.WriteAllBytes(
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(ReplayFile) + ".milk"),
                milkV1Bytes
            );File.WriteAllBytes(
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(ReplayFile) + ".milk2"),
                milkV2Bytes
            );
            
            sw.Stop();
            Console.WriteLine($"Finished writing to file in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();

        }

        static List<g_Instance> ReadReplayFile(StreamReader fileReader)
        {
            bool fileFinishedReading = false;
            List<g_Instance> readFrames = new List<g_Instance>();
            Game readGame = new Game();

            using (fileReader = OpenOrExtract(fileReader))
            {
                while (!fileFinishedReading)
                {
                    if (fileReader != null)
                    {
                        string rawJSON = fileReader.ReadLine();
                        if (rawJSON == null)
                        {
                            fileFinishedReading = true;
                            fileReader.Close();
                        }
                        else
                        {
                            string[] splitJSON = rawJSON.Split('\t');
                            string onlyJSON, onlyTime;
                            if (splitJSON.Length == 2)
                            {
                                onlyJSON = splitJSON[1];
                                onlyTime = splitJSON[0];
                            }
                            else
                            {
                                Console.WriteLine("Row doesn't include both a time and API JSON");
                                continue;
                            }

                            DateTime frameTime = DateTime.Parse(onlyTime);

                            // if this is actually valid arena data
                            if (onlyJSON.Length > 300)
                            {
                                try
                                {
                                    g_Instance foundFrame = JsonConvert.DeserializeObject<g_Instance>(onlyJSON);
                                    foundFrame.recorded_time = frameTime;
                                    readFrames.Add(foundFrame);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Couldn't read frame. File is corrupted.");
                                }
                            }
                        }
                    }
                }
            }

            return readFrames;
        }

        private static StreamReader OpenOrExtract(StreamReader reader)
        {
            char[] buffer = new char[2];
            reader.Read(buffer, 0, buffer.Length);
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            if (buffer[0] == 'P' && buffer[1] == 'K')
            {
                ZipArchive archive = new ZipArchive(reader.BaseStream);
                StreamReader ret = new StreamReader(archive.Entries[0].Open());
                return ret;
            }

            return reader;
        }
    }
}