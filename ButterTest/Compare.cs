using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using ButterReplays;
using EchoVRAPI;
using Newtonsoft.Json;

namespace ButterTest
{
	public static class Compare
	{
		public static string CompareFiles(string file1, string file2)
		{
			Frame diffFrame = Frame.CreateEmpty();
			Frame maxFrame = Frame.CreateEmpty();

			Console.WriteLine("Reading File 1");
			StreamReader r1 = new StreamReader(file1);
			List<Frame> f1 = EchoReplay.ReadReplayFile(r1);

			Console.WriteLine("Reading File 2");
			StreamReader r2 = new StreamReader(file2);
			List<Frame> f2 = EchoReplay.ReadReplayFile(r2);

			Console.WriteLine("Comparing");
			if (f1.Count != f2.Count)
			{
				return "Frame count not the same";
			}

			for (int i = 0; i < f1.Count; i++)
			{
				ProgrammaticDiff(f1[i], f2[i], diffFrame, maxFrame);
			}

			diffFrame.err_code = f1.Count;
			maxFrame.err_code = f1.Count;
			return JsonConvert.SerializeObject(maxFrame, Formatting.Indented);
		}

		private static void ProgrammaticDiff<T>(T f1, T f2, T diffFrame, T maxFrame)
		{
			foreach (FieldInfo field in f1.GetType().GetFields())
			{
				if (Attribute.IsDefined(field, typeof(JsonIgnoreAttribute))) continue;
				CompareField(
					f1, f2, diffFrame, maxFrame,
					field.GetValue(f1), field.GetValue(f2), field.GetValue(diffFrame), field.GetValue(maxFrame),
					field
				);
			}

			foreach (PropertyInfo field in f1.GetType().GetProperties())
			{
				if (Attribute.IsDefined(field, typeof(JsonIgnoreAttribute))) continue;
				CompareField(
					f1, f2, diffFrame, maxFrame,
					field.GetValue(f1), field.GetValue(f2), field.GetValue(diffFrame), field.GetValue(maxFrame),
					field
				);
			}
		}

		private static void CompareField<T>(T f1, T f2, T diffFrame, T maxFrame,
			object f1Val, object f2Val, object diffFrameVal, object maxFrameVal,
			MemberInfo field)
		{
			if (f1.IsNumber())
			{
				double diff = Math.Abs(f1Val.ToDouble() - f2Val.ToDouble());
				double max = maxFrameVal.ToDouble();
				if (diff > max)
				{
					if (field is PropertyInfo fInfo1)
					{
						fInfo1.SetValue(maxFrame, Convert.ChangeType(diff, fInfo1.PropertyType));
					}
					else if (field is FieldInfo fInfo3)
					{
						fInfo3.SetValue(maxFrame, Convert.ChangeType(diff, fInfo3.FieldType));
					}
				}

				if (field is PropertyInfo fInfo2)
				{
					fInfo2.SetValue(diffFrame, Convert.ChangeType(diff, fInfo2.PropertyType));
				}
				else if (field is FieldInfo fInfo4)
				{
					fInfo4.SetValue(diffFrame, Convert.ChangeType(diff, fInfo4.FieldType));
				}
			}

			if (f1Val is string)
			{
				if (f1Val as string != f2Val as string)
				{
					if (
						f1Val != "BLUE TEAM" &&
						f1Val != "ORANGE TEAM" &&
						f1Val != "SPECTATORS" &&
						(f1Val != "[INVALID]" && f2Val != null)
					)
					{
						Console.WriteLine($"{f1Val as string} != {f2Val as string}");
					}
				}
			}

			if (f1Val.IsSubType())
			{
				ProgrammaticDiff(f1Val, f2Val, diffFrameVal, maxFrameVal);
			}

			if (f1Val is List<Team>)
			{
				ListDiff(f1Val as List<Team>, f2Val as List<Team>, diffFrameVal as List<Team>, maxFrameVal as List<Team>);
			}

			if (f1Val is List<Player>)
			{
				ListDiff(f1Val as List<Player>, f2Val as List<Player>, diffFrameVal as List<Player>, maxFrameVal as List<Player>);
			}

			if (f1Val is List<float>)
			{
				ListDiff(f1Val as List<float>, f2Val as List<float>, diffFrameVal as List<float>, maxFrameVal as List<float>);
			}
		}

		private static void ListDiff<T>(List<T> f1, List<T> f2, List<T> diffFrame, List<T> maxFrame)
		{
			// if (f1.Count > 0 && f1[0].GetType().IsNumber())
			{
				for (int i = 0; i < f1.Count; i++)
				{
					if (typeof(T) == typeof(Team))
					{
						if (maxFrame.Count <= i) maxFrame.Add((T)Convert.ChangeType(Team.CreateEmpty(), typeof(T)));
						if (diffFrame.Count <= i) diffFrame.Add((T)Convert.ChangeType(Team.CreateEmpty(), typeof(T)));
						ProgrammaticDiff(f1[i], f2[i], diffFrame[i], maxFrame[i]);
					}

					if (typeof(T) == typeof(Player))
					{
						if (maxFrame.Count <= i) maxFrame.Add((T)Convert.ChangeType(Player.CreateEmpty(), typeof(T)));
						if (diffFrame.Count <= i) diffFrame.Add((T)Convert.ChangeType(Player.CreateEmpty(), typeof(T)));
						ProgrammaticDiff(f1[i], f2[i], diffFrame[i], maxFrame[i]);
					}

					if (f1[0].IsNumber())
					{
						double diff = Math.Abs(f1[i].ToDouble() - f2[i].ToDouble());
						double max = maxFrame[i].ToDouble();
						if (diff > max)
						{
							maxFrame[i] = (T)Convert.ChangeType(diff, typeof(T));
						}

						double prevDiff = diffFrame[i].ToDouble();
						diffFrame[i] = (T)Convert.ChangeType(prevDiff + max, typeof(T));
					}
				}
			}
		}

		public static bool IsSubType(this object? value)
		{
			return value is Disc
			       || value is LastScore
			       || value is LastThrow
			       || value is VRPlayer
			       || value is Pause
			       || value is Team
			       || value is Player
			       || value is Transform
			       || value is Stats;
		}

		public static bool IsNumber(this object? value)
		{
			return value is sbyte
			       || value is byte
			       || value is short
			       || value is ushort
			       || value is int
			       || value is uint
			       || value is long
			       || value is ulong
			       || value is float
			       || value is double
			       || value is decimal;
		}

		public static double ToDouble(this object? expression)
		{
			return double.Parse(Convert.ToString(expression, CultureInfo.InvariantCulture) ?? throw new InvalidOperationException(), NumberStyles.Any, NumberFormatInfo.InvariantInfo);
		}
	}
}