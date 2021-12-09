using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ButterReplays;
using EchoVRAPI;

string GetArgument(IEnumerable<string> a, string option) =>
	a.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault() ?? string.Empty;

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

Console.WriteLine($"Reading file: {replayFile}");
Stopwatch sw = new Stopwatch();
sw.Start();
StreamReader reader = new StreamReader(replayFile);

List<Frame> file = EchoReplay.ReadReplayFile(reader);
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

ButterFile butter = new ButterFile();
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

BinaryReader binaryReader =
	new BinaryReader(
		File.OpenRead(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + ".butter")));
List<Frame> rereadReplay = ButterFile.FromBytes(binaryReader);

sw.Stop();
Console.WriteLine($"Finished reading from butter file in {sw.Elapsed.TotalSeconds:N3} seconds");
sw.Restart();

EchoReplay.SaveReplay(
	Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + "_processed.echoreplay"),
	rereadReplay);

File.WriteAllText(
	Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + "_reconstructed_frame_1.json"),
	JsonConvert.SerializeObject(rereadReplay[0]));
File.WriteAllText(
	Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + "_reconstructed_frame_2.json"),
	JsonConvert.SerializeObject(rereadReplay[1]));
File.WriteAllText(
	Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(replayFile) + "_reconstructed_frame_10.json"),
	JsonConvert.SerializeObject(rereadReplay[9]));

sw.Stop();
Console.WriteLine($"Finished writing to .echoreplay file in {sw.Elapsed.TotalSeconds:N3} seconds");
sw.Restart();


// Quaternion before = new Quaternion(-0.15847519f,0.18832426f,-0.0826531f,0.965706f);
// // Quaternion before = new Quaternion(0,0,0,1);
// byte[] smallestThree = Butter.ButterFrame.SmallestThree(before);
// using (BinaryReader rd = new BinaryReader(new MemoryStream(smallestThree))) {
//     Quaternion after = rd.ReadSmallestThree();
//     Console.WriteLine($"{before}\n{after}");
// }