using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ButterReplays;
using EchoVRAPI;

namespace ButterTest
{
	public static class Converter
	{
		public static string? Convert(string filename, string? outputFolder = null)
		{
			if (filename.EndsWith(".butter"))
			{
				BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename));
				List<Frame> frames = ButterFile.FromBytes(binaryReader);
				outputFolder ??= Path.GetDirectoryName(filename) ?? throw new InvalidOperationException();
				string outputFilename = Path.Combine(outputFolder,
					Path.GetFileNameWithoutExtension(filename) + "_processed.echoreplay");
				EchoReplay.SaveReplay(outputFilename, frames);
				return outputFilename;
			}

			if (filename.EndsWith(".echoreplay"))
			{
				List<Frame> frames = ReadFile(filename);
				outputFolder ??= Path.GetDirectoryName(filename) ?? throw new InvalidOperationException();
				string outputFilename =
					Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(filename) + ".butter");
				StreamToButter(frames, outputFilename);
				return outputFilename;
			}

			Console.WriteLine("Can't convert. Unrecognized file type.");
			return null;
		}

		public static List<Frame> ReadFile(string fileName)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Console.WriteLine($"Reading file: {fileName}");

			sw.Restart();
			StreamReader reader = new StreamReader(fileName);

			List<Frame> frames = EchoReplay.ReadReplayFile(reader);
			// Thread loadThread = new Thread(() => ReadReplayFile(reader));
			// loadThread.Start();
			sw.Stop();
			Console.WriteLine($"Finished reading {frames.Count:N0} lines in {sw.Elapsed.TotalSeconds:N3} seconds");
			return frames;
		}

		private static void StreamToButter(List<Frame> frames, string outputFilename)
		{
			const bool saveIntermediate = false;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			ButterFile butter = new ButterFile(compressionFormat: ButterFile.CompressionFormat.none);

			int lastNumChunks = 0;
			foreach (Frame f in frames)
			{
				butter.AddFrame(f);
				if (saveIntermediate && lastNumChunks != butter.NumChunks())
				{
					byte[] intermediateBytes = butter.GetBytes();

					File.WriteAllBytes(outputFilename, intermediateBytes);
				}

				lastNumChunks = butter.NumChunks();
			}

			byte[] butterBytes = butter.GetBytes();

			File.WriteAllBytes(outputFilename, butterBytes);

			Console.WriteLine($"Finished converting to Butter in {sw.Elapsed.TotalSeconds:N3} seconds");
		}
	}
}