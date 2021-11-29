using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Spark;

namespace Butter
{
    internal static class Program
    {
        // private const string ReplayFile = @"C:\Users\Anton\Sync\Milk Testing\rec_2021-11-13_00-00-32.echoreplay";
        // private const string ReplayFile = @"C:\Users\Anton\Sync\Milk Testing\rec_2021-11-12_23-48-58.echoreplay";
        //
        // private const string OutputFolder = @"C:\Users\Anton\Sync\Milk Testing";

        private static void Main(string[] args)
        {
            string GetArgument(IEnumerable<string> a, string option) =>
                a.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault();

            string replayFile = GetArgument(args, "-i");
            string outputFolder = GetArgument(args, "-o");
            if (string.IsNullOrEmpty(replayFile)) return;
            if (string.IsNullOrEmpty(outputFolder)) return;

            Console.WriteLine($"Reading file: {replayFile}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            StreamReader reader = new StreamReader(replayFile);

            List<g_Instance> file = ReadReplayFile(reader);
            // Thread loadThread = new Thread(() => ReadReplayFile(reader));
            // loadThread.Start();
            sw.Stop();
            Console.WriteLine($"Finished reading {file.Count:N0} lines in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();

            Milk milk = new Milk(file[0]);
            file.ForEach(frame => { milk.AddFrame(frame); });
            byte[] milkBytes = milk.GetBytes();

            sw.Stop();
            Console.WriteLine($"Finished converting to Milk in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();

            Butter butter = new Butter();
            file.ForEach(frame => { butter.AddFrame(frame); });
            byte[] butterBytes = butter.GetBytes();

            sw.Stop();
            Console.WriteLine($"Finished converting to Butter in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();

            File.WriteAllBytes(
                Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".milk"),
                milkBytes
            );
            File.WriteAllBytes(
                Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".butter"),
                butterBytes
            );

            sw.Stop();
            Console.WriteLine($"Finished writing to file in {sw.Elapsed.TotalSeconds:N3} seconds");
            sw.Restart();
        }


        static List<g_Instance> ReadReplayFile(StreamReader fileReader)
        {
            bool fileFinishedReading = false;
            List<g_Instance> readFrames = new List<g_Instance>();

            using (fileReader = OpenOrExtract(fileReader))
            {
                while (!fileFinishedReading)
                {
                    if (fileReader == null) continue;

                    string rawJson = fileReader.ReadLine();
                    if (rawJson == null)
                    {
                        fileFinishedReading = true;
                        fileReader.Close();
                    }
                    else
                    {
                        string[] splitJson = rawJson.Split('\t');
                        string onlyJson, onlyTime;
                        if (splitJson.Length == 2)
                        {
                            onlyJson = splitJson[1];
                            onlyTime = splitJson[0];
                        }
                        else
                        {
                            Console.WriteLine("Row doesn't include both a time and API JSON");
                            continue;
                        }

                        DateTime frameTime = DateTime.Parse(onlyTime);

                        // if this is actually valid arena data
                        if (onlyJson.Length <= 300) continue;

                        try
                        {
                            g_Instance foundFrame = JsonConvert.DeserializeObject<g_Instance>(onlyJson);
                            if (foundFrame != null)
                            {
                                foundFrame.recorded_time = frameTime;
                                readFrames.Add(foundFrame);
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Couldn't read frame. File is corrupted.");
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
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            if (buffer[0] != 'P' || buffer[1] != 'K') return reader;
            ZipArchive archive = new ZipArchive(reader.BaseStream);
            StreamReader ret = new StreamReader(archive.Entries[0].Open());
            return ret;
        }
    }
}