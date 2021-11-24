using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Threading;
using Newtonsoft.Json;

namespace Milk2
{
    class Program
    {
        public static string replayFile = @"C:\Users\Anton\Sync\rec_2021-11-13_00-00-32.echoreplay";
        
        static void Main(string[] args)
        {
            Console.WriteLine($"Reading file: {replayFile}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            StreamReader reader = new StreamReader(replayFile);

            var file = ReadReplayFile(reader);
            // Thread loadThread = new Thread(() => ReadReplayFile(reader));
            // loadThread.Start();
            sw.Stop();

            Console.WriteLine($"Finished reading {file.Count:N0} lines in {sw.Elapsed.TotalSeconds:N3} seconds");
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