using System.Diagnostics;
using ButterReplays;
using EchoVRAPI;

namespace ButterTest;

public class Converter
{
	public static bool Convert(string filename)
	{
		if (filename.EndsWith(".butter"))
		{
			BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename));
			List<Frame> frames = ButterFile.FromBytes(binaryReader);
			string outputFilename = Path.Combine(
				Path.GetDirectoryName(filename) ?? throw new InvalidOperationException(), 
				Path.GetFileNameWithoutExtension(filename) + "_processed.echoreplay");
			EchoReplay.SaveReplay(outputFilename, frames);
			return true;
		}

		if (filename.EndsWith(".echoreplay"))
		{
			List<Frame> frames = ReadFile(filename);
			string outputFilename = Path.Combine(
				Path.GetDirectoryName(filename) ?? throw new InvalidOperationException(), 
				Path.GetFileNameWithoutExtension(filename) + ".gzip.butter");
			StreamToButter(frames, outputFilename);
			return true;
		}

		Console.WriteLine("Can't convert. Unrecognized file type.");
		return false;
	}

	private static List<Frame> ReadFile(string fileName)
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
		ButterFile butter = new ButterFile(compressionFormat: ButterFile.CompressionFormat.gzip);

		int lastNumChunks = 0;
		foreach (Frame f in frames)
		{
			butter.AddFrame(f);
			if (lastNumChunks != butter.NumChunks())
			{
				byte[] intermediateBytes = butter.GetBytes();

				File.WriteAllBytes(outputFilename, intermediateBytes);
			}

			lastNumChunks = butter.NumChunks();
		}

		byte[] butterBytes = butter.GetBytes();

		File.WriteAllBytes(outputFilename, butterBytes);
	}
}