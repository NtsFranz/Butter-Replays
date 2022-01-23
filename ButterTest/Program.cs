using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using ButterReplays;
using ButterTest;
using EchoVRAPI;

string GetArgument(IEnumerable<string> a, string option) =>
	a.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault() ?? string.Empty;

string convert = GetArgument(args, "-c");
if (!string.IsNullOrEmpty(convert))
{
	Converter.Convert(convert);
	return;
}

string replayFile = GetArgument(args, "-i");
string outputFolder = GetArgument(args, "-o");
if (string.IsNullOrEmpty(replayFile))
{
	Console.WriteLine("No input file specified");
	return;
}

if (string.IsNullOrEmpty(outputFolder))
{
	Console.WriteLine("No output folder specified");
	return;
}

List<Frame> file = ReadFile(replayFile);

ButterFile bf = new ButterFile();
bf.AddFrame(file[100]);
byte[] bytes = bf.GetBytes();

List<Frame> converted = ButterFile.FromBytes(bytes);


// ConvertToMilk(file);

// ConvertToButter(file);
StreamToButter(file);

ReconstructFromButter(outputFolder, replayFile);

// TestQuaternions();


List<Frame> ReadFile(string fileName)
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

void ConvertToMilk(List<Frame> frames)
{
	Stopwatch sw = new Stopwatch();
	sw.Start();
	Milk milk = new Milk(frames[0]);
	frames.ForEach(frame => { milk.AddFrame(frame); });
	byte[] milkBytes = milk.GetBytes();

	File.WriteAllBytes(
		Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".milk"),
		milkBytes
	);

	Console.WriteLine($"Finished converting to Milk in {sw.Elapsed.TotalSeconds:N3} seconds");
}

void StreamToButter(List<Frame> frames)
{
	Stopwatch sw = new Stopwatch();
	sw.Start();
	ButterFile butter = new ButterFile(compressionFormat: ButterFile.CompressionFormat.gzip);

	int lastNumChunks = 0;
	foreach (Frame f in frames)
	{
		butter.AddFrame(f);
		if (lastNumChunks != butter.NumChunks())
		{
			byte[] intermediateBytes = butter.GetBytes();

			File.WriteAllBytes(
				Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".butter"),
				intermediateBytes
			);
		}

		lastNumChunks = butter.NumChunks();
	}

	byte[] butterBytes = butter.GetBytes();

	File.WriteAllBytes(
		Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".gzip.butter"),
		butterBytes
	);
	
	Console.WriteLine($"Finished converting to Butter in {sw.Elapsed.TotalSeconds:N3} seconds");
}

void ConvertToButter(List<Frame> frames)
{
	Stopwatch sw = new Stopwatch();
	sw.Start();


	sw.Stop();
	Console.WriteLine($"Finished adding frames to Butter in {sw.Elapsed.TotalSeconds:N3} seconds");

	List<ButterFile.CompressionFormat> compressionLevels = new List<ButterFile.CompressionFormat>()
	{
		// -1, 0, 1, 3, 7, 13, 22
		ButterFile.CompressionFormat.none,
		ButterFile.CompressionFormat.gzip,
		ButterFile.CompressionFormat.zstd_7,
		ButterFile.CompressionFormat.zstd_22
	};
	List<ushort> chunkSizes = new List<ushort>()
	{
		// 1, 2, 4, 8, 15, 30, 60, 120, 240, 480, 960, 1920
		60, 300
	};

	Dictionary<string, Dictionary<string, double>> combinedSizes = new Dictionary<string, Dictionary<string, double>>();
	foreach (ButterFile.CompressionFormat compressionLevel in compressionLevels)
	{
		foreach (ushort chunkSize in chunkSizes)
		{
			// const int compressionFormat = 7;
			// const ushort chunkSize = 300;
			// const bool useDict = false;

			sw.Restart();
			ButterFile butter = new ButterFile(chunkSize, compressionLevel);
			frames.ForEach(frame => { butter.AddFrame(frame); });
			byte[] butterBytes = butter.GetBytes(out Dictionary<string, double> sizes);

			string k = $"{compressionLevel}_{chunkSize}";
			combinedSizes[k] = sizes;

			// Directory.CreateDirectory(Path.Combine(outputFolder, "Sizes_" + k));
			// foreach (string key in sizes.Keys)
			// {
			// 	File.WriteAllBytesAsync(
			// 		Path.Combine(outputFolder, "Sizes_" + k, key + ".bytes"), 
			// 		new byte[(uint) sizes[key]]
			// 	);
			// }

			File.WriteAllBytes(
				Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + "_" + k + ".butter"),
				butterBytes
			);

			File.WriteAllBytes(
				Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".butter"),
				butterBytes
			);

			sw.Stop();
			Console.WriteLine($"Finished converting to Butter in {sw.Elapsed.TotalSeconds:N3} seconds");
		}
	}

	File.WriteAllText(Path.Combine(outputFolder, "butter_sizes.json"), JsonConvert.SerializeObject(combinedSizes));
}

void ReconstructFromButter(string s, string replayFilename)
{
	Stopwatch sw = new Stopwatch();
	sw.Start();

	BinaryReader binaryReader = new BinaryReader(File.OpenRead(Path.Combine(s, Path.GetFileNameWithoutExtension(replayFilename) + ".butter")));
	List<Frame> rereadReplay = ButterFile.FromBytes(binaryReader);

	List<string> originalJSON = new List<string>();
	bool fileFinishedReading = false;

	StreamReader fileReader = new StreamReader(replayFilename);
	using (fileReader = EchoReplay.OpenOrExtract(fileReader))
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
					originalJSON.Add(onlyJson);
				}
				else
				{
					Console.WriteLine("Row doesn't include both a time and API JSON");
					continue;
				}
			}
		}
	}

	sw.Stop();
	Console.WriteLine($"\nFinished reading from butter file in {sw.Elapsed.TotalSeconds:N3} seconds");
	sw.Restart();

	EchoReplay.SaveReplay(
		Path.Combine(s, Path.GetFileNameWithoutExtension(replayFilename) + "_processed.echoreplay"),
		rereadReplay);

	List<int> frames = new List<int>()
	{
		0, 2, 9, 299, 300, 301
	};
	foreach (int frame in frames)
	{
		File.WriteAllText(
			Path.Combine(s, Path.GetFileNameWithoutExtension(replayFilename) + "_reconstructed_frame_" + frame + ".json"),
			JsonConvert.SerializeObject(rereadReplay[frame]));
		File.WriteAllText(Path.Combine(s, Path.GetFileNameWithoutExtension(replayFilename) + "_frame_" + frame + ".json"), originalJSON[frame]);
	}

	sw.Stop();
	Console.WriteLine($"Finished writing to .echoreplay file in {sw.Elapsed.TotalSeconds:N3} seconds");
	sw.Restart();
}

void TestQuaternions()
{
	// Vector3 forward = new Vector3(-0.047000002f, -0.98400003f, -0.171f);
	// Vector3 left = new Vector3(-0.89300007f, -0.035f, 0.44800001f);
	// Vector3 up = new Vector3(-0.44700003f, 0.17300001f, -0.87700003f);

	// vr_player
	// Vector3 forward = new Vector3(-0.49500003f,-0.052000001f,0.86700004f);
	// Vector3 left = new Vector3(0.86300004f, 0.089000002f,0.49800003f);
	// Vector3 up = new Vector3(-0.10300001f,0.99500006f,0.001f);

	// rhand
	Vector3 forward = new Vector3(
		0.43200001f,
		-0.77700001f,
		-0.45900002f);
	Vector3 left = new Vector3(
		-0.80300003f,
		-0.56300002f,
		0.19600001f);
	Vector3 up = new Vector3(
		-0.41000003f,
		0.28400001f,
		-0.86700004f);

	Quaternion q = QuaternionLookRotation(forward, up);

	Console.WriteLine("Forward");
	Console.WriteLine(forward);
	Console.WriteLine(q.Forward());
	Console.WriteLine(forward - q.Forward());
	Console.WriteLine("Left");
	Console.WriteLine(left);
	Console.WriteLine(q.Left());
	Console.WriteLine(left - q.Left());
	Console.WriteLine("Up");
	Console.WriteLine(up);
	Console.WriteLine(q.Up());
	Console.WriteLine(up - q.Up());
	Console.WriteLine();


	// Quaternion before = new Quaternion(-0.15847519f, 0.18832426f, -0.0826531f, 0.965706f);
	Quaternion before = q;
	// before = Quaternion.Inverse(q);
	// Quaternion before = new Quaternion(0,0,0,1);
	byte[] smallestThree = ButterFrame.SmallestThree(before);
	using BinaryReader rd = new BinaryReader(new MemoryStream(smallestThree));
	Quaternion after = rd.ReadSmallestThree();
	Console.WriteLine($"{before}\n{after}\n{before - after}");

	// static Vector3 Left(Quaternion q)
	// {
	// 	return Vector3.Cross(q.Up(), q.Forward());
	// }
	//
	// static Vector3 Forward(Quaternion q) => new Vector3(
	// 	(float) (2.0 * ((double) q.X * q.Z + (double) q.W * q.Y)),
	// 	(float) (2.0 * ((double) q.Y * q.Z - (double) q.W * q.X)),
	// 	(float) (1.0 - 2.0 * ((double) q.X * q.X + (double) q.Y * q.Y))
	// );

	static Quaternion QuaternionLookRotation(Vector3 forward, Vector3 up)
	{
		forward /= forward.Length();
		Vector3 vector3_1 = Vector3.Normalize(forward);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector3_1));
		Vector3 vector3_2 = Vector3.Cross(vector3_1, vector2);
		float x1 = vector2.X;
		float y1 = vector2.Y;
		float z1 = vector2.Z;
		float x2 = vector3_2.X;
		float y2 = vector3_2.Y;
		float z2 = vector3_2.Z;
		float x3 = vector3_1.X;
		float y3 = vector3_1.Y;
		float z3 = vector3_1.Z;
		float num1 = x1 + y2 + z3;
		Quaternion quaternion = new Quaternion();
		if ((double)num1 > 0.0)
		{
			float num2 = (float)Math.Sqrt((double)num1 + 1.0);
			quaternion.W = num2 * 0.5f;
			float num3 = 0.5f / num2;
			quaternion.X = (z2 - y3) * num3;
			quaternion.Y = (x3 - z1) * num3;
			quaternion.Z = (y1 - x2) * num3;
			return quaternion;
		}

		if ((double)x1 >= (double)y2 && (double)x1 >= (double)z3)
		{
			float num4 = (float)Math.Sqrt(1.0 + (double)x1 - (double)y2 - (double)z3);
			float num5 = 0.5f / num4;
			quaternion.X = 0.5f * num4;
			quaternion.Y = (y1 + x2) * num5;
			quaternion.Z = (z1 + x3) * num5;
			quaternion.W = (z2 - y3) * num5;
			return quaternion;
		}

		if ((double)y2 > (double)z3)
		{
			float num6 = (float)Math.Sqrt(1.0 + (double)y2 - (double)x1 - (double)z3);
			float num7 = 0.5f / num6;
			quaternion.X = (x2 + y1) * num7;
			quaternion.Y = 0.5f * num6;
			quaternion.Z = (y3 + z2) * num7;
			quaternion.W = (x3 - z1) * num7;
			return quaternion;
		}

		float num8 = (float)Math.Sqrt(1.0 + (double)z3 - (double)x1 - (double)y2);
		float num9 = 0.5f / num8;
		quaternion.X = (x3 + z1) * num9;
		quaternion.Y = (y3 + z2) * num9;
		quaternion.Z = 0.5f * num8;
		quaternion.W = (y1 - x2) * num9;
		return quaternion;
	}
}