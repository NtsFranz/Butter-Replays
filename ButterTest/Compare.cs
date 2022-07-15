using System;
using System.Collections.Generic;
using System.IO;
using ButterReplays;
using EchoVRAPI;

namespace ButterTest
{
	public static class Compare
	{
		public static float CompareFiles(string file1, string file2)
		{
			int fieldCount = 0;
			float dist = 0;

			
			StreamReader r1 = new StreamReader(file1);
			List<Frame> f1 = EchoReplay.ReadReplayFile(r1);
			
			StreamReader r2 = new StreamReader(file2);
			List<Frame> f2 = EchoReplay.ReadReplayFile(r2);

			if (f1.Count != f2.Count)
			{
				return -1;
			}

			for (int i = 0; i < f1.Count; i++)
			{
				fieldCount++;
				dist += MathF.Abs(f1[i].game_clock - f2[i].game_clock);
			}

			return dist / fieldCount;
		}
	}
}