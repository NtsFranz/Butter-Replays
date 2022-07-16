using System;
using System.Collections.Generic;
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

			StreamReader r1 = new StreamReader(file1);
			List<Frame> f1 = EchoReplay.ReadReplayFile(r1);

			StreamReader r2 = new StreamReader(file2);
			List<Frame> f2 = EchoReplay.ReadReplayFile(r2);

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
				if (field.GetValue(f1).IsNumber())
				{
					double diff = Math.Abs(field.GetValue(f1).ToDouble() - field.GetValue(f2).ToDouble());
					double max = field.GetValue(maxFrame).ToDouble();
					if (diff > max)
					{
						field.SetValue(maxFrame, Convert.ChangeType(diff, field.FieldType));
					}

					field.SetValue(diffFrame, Convert.ChangeType(diff, field.FieldType));
				}

				if (field.GetValue(f1).IsSubType())
				{
					ProgrammaticDiff(field.GetValue(f1), field.GetValue(f2), field.GetValue(diffFrame), field.GetValue(maxFrame));
				}
			}

			foreach (PropertyInfo field in f1.GetType().GetProperties())
			{
				if (Attribute.IsDefined(field, typeof(JsonIgnoreAttribute))) continue;

				if (field.GetValue(f1).IsNumber())
				{
					double diff = Math.Abs(field.GetValue(f1).ToDouble() - field.GetValue(f2).ToDouble());
					double max = field.GetValue(maxFrame).ToDouble();
					if (diff > max)
					{
						field.SetValue(maxFrame, Convert.ChangeType(diff, field.PropertyType));
					}

					field.SetValue(diffFrame, Convert.ChangeType(diff, field.PropertyType));
				}

				if (field.GetValue(f1).IsSubType())
				{
					ProgrammaticDiff(field.GetValue(f1), field.GetValue(f2), field.GetValue(diffFrame), field.GetValue(maxFrame));
				}

				if (field.GetValue(f1) is List<Team>)
				{
					ListDiff(field.GetValue(f1) as List<Team>, field.GetValue(f2) as List<Team>, field.GetValue(diffFrame) as List<Team>, field.GetValue(maxFrame) as List<Team>);
				}
				
				if (field.GetValue(f1) is List<Player>)
				{
					ListDiff(field.GetValue(f1) as List<Player>, field.GetValue(f2) as List<Player>, field.GetValue(diffFrame) as List<Player>, field.GetValue(maxFrame) as List<Player>);
				}
				
				if (field.GetValue(f1) is List<float>)
				{
					ListDiff(field.GetValue(f1) as List<float>, field.GetValue(f2) as List<float>, field.GetValue(diffFrame) as List<float>, field.GetValue(maxFrame) as List<float>);
				}
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