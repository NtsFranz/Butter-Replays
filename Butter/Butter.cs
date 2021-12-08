using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Butter
{
	/// <summary>
	/// 🥛🥛🥛🥛🥛
	/// </summary>
	public class Butter
	{
		private ButterFileHeader fileHeader;
		private List<ButterFrame> frames;

		public static int includedCount = 0;
		public static int excludedCount = 0;

		public Butter()
		{
		}

		public Butter(g_Instance frame)
		{
			AddFrame(frame);
		}

		private enum GameStatus : byte
		{
			_ = 0,
			pre_match = 1,
			round_start = 2,
			playing = 3,
			score = 4,
			round_over = 5,
			post_match = 6,
			pre_sudden_death = 7,
			sudden_death = 8,
			post_sudden_death = 9
		}


		private enum MapName : byte
		{
			uncoded = 0,
			mpl_lobby_b2 = 1,
			mpl_arena_a = 2,
			mpl_combat_fission = 3,
			mpl_combat_combustion = 4,
			mpl_combat_dyson = 5,
			mpl_combat_gauss = 6,
			mpl_tutorial_arena = 7,
		}

		/// <summary>
		/// This can be stored as 2 bits
		/// </summary>
		private enum PausedState : byte
		{
			unpaused = 0,
			paused = 1,
			unpausing = 2,
			pausing = 3, // TODO is this a thing?
		}

		/// <summary>
		/// This can be stored as 2 bits
		/// </summary>
		private enum TeamIndex : byte
		{
			Blue = 0,
			Orange = 1,
			Spectator = 2,
			None = 3
		}

		private enum GoalType : byte
		{
			unknown,
			BOUNCE_SHOT,
			INSIDE_SHOT,
			LONG_BOUNCE_SHOT,
			LONG_SHOT,
			SELF_GOAL,
			SLAM_DUNK,
			BUMPER_SHOT,
			HEADBUTT,
			// TODO contains more
		}

		public static string MatchType(string mapName, bool privateMatch) {
			return mapName switch {
				// "mpl_lobby_b2" => privateMatch ? "Private Match" : "Public Match",
				"mpl_arena_a" => privateMatch ? "Echo_Arena_Private" : "Unknown",
				// "mpl_combat_fission" => privateMatch ? "Private Match" : "Public Match",
				// "mpl_combat_combustion" => privateMatch ? "Private Match" : "Public Match",
				// "mpl_combat_dyson" => privateMatch ? "Private Match" : "Public Match",
				// "mpl_combat_gauss" => privateMatch ? "Private Match" : "Public Match",
				// "mpl_tutorial_arena" => privateMatch ? "Private Match" : "Public Match",
				_ => "Unknown"
			};
		}

		public class ButterFileHeader
		{
			public byte formatVersion = 3;
			public ushort keyframeInterval = 30;

			public g_Instance firstFrame;

			public List<string> players;
			public List<int> numbers;
			public List<int> levels;
			public List<ulong> userids;


			public ButterFileHeader()
			{
				players = new List<string>();
				numbers = new List<int>();
				levels = new List<int>();
				userids = new List<ulong>();
			}

			public ButterFileHeader(g_Instance firstFrame)
			{
				this.firstFrame = firstFrame;

				players = new List<string>();
				numbers = new List<int>();
				levels = new List<int>();
				userids = new List<ulong>();

				ConsiderNewFrame(firstFrame);
			}

			public void ConsiderNewFrame(g_Instance frame)
			{
				foreach (g_Team team in frame.teams)
				{
					if (team.players == null) continue;
					foreach (g_Player player in team.players)
					{
						if (!userids.Contains(player.userid))
						{
							players.Add(player.name);
							numbers.Add(player.number);
							levels.Add(player.level);
							userids.Add(player.userid);
						}
					}
				}
			}

			/// <summary>
			/// IPv4 Addresses can be stored as 4 bytes
			/// </summary>
			public static byte[] IpAddressToBytes(string ipAddress)
			{
				string[] parts = ipAddress.Split('.');
				byte[] bytes = new byte[4];

				if (parts.Length != 4)
				{
					throw new ArgumentException("IP Address doesn't have 4 parts.");
				}

				for (int i = 0; i < 4; i++)
				{
					bytes[i] = byte.Parse(parts[i]);
				}

				return bytes;
			}

			/// <summary>
			/// Converts a session id from a string into 16 bytes
			/// </summary>
			/// <returns></returns>
			public static byte[] SessionIdToBytes(string sessionId)
			{
				string str = sessionId.Replace("-", "");
				return StringToByteArrayFastest(str);
			}

			/// <summary>
			/// https://stackoverflow.com/a/9995303
			/// </summary>
			public static byte[] StringToByteArrayFastest(string hex)
			{
				if (hex.Length % 2 == 1)
					throw new Exception("The binary key cannot have an odd number of digits");

				byte[] arr = new byte[hex.Length >> 1];

				for (int i = 0; i < hex.Length >> 1; ++i)
				{
					arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
				}

				return arr;
			}

			public static int GetHexVal(char hex)
			{
				int val = hex;
				return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
			}

			public static string ByteArrayToString(byte[] bytes)
			{
				return BitConverter.ToString(bytes).Replace("-", string.Empty);
			}

			public byte GetPlayerIndex(string playerName)
			{
				int index = players.IndexOf(playerName);
				return (byte)(index + 1);
			}


			public string GetPlayerName(byte playerIndex)
			{
				return playerIndex == 0 ? "INVALID PLAYER" : players[playerIndex - 1];
			}
			public int GetPlayerLevel(byte playerIndex)
			{
				return playerIndex == 0 ? -1 : levels[playerIndex - 1];
			}

			public ulong GetUserId(byte playerIndex)
			{
				return playerIndex == 0 ? 0 : userids[playerIndex - 1];
			}

			public byte GetPlayerIndex(ulong userId)
			{
				int index = userids.IndexOf(userId);
				return (byte)(index + 1);
			}


			public byte HoldingToByte(string holding)
			{
				return holding switch
				{
					"none" => 255,
					"geo" => 254,
					"disc" => 253,
					_ => GetPlayerIndex(ulong.Parse(holding))
				};
			}

			public string ByteToHolding(byte holding)
			{
				return holding switch
				{
					255 => "none",
					254 => "geo",
					253 => "disc",
					_ => GetUserId(holding).ToString()
				};
			}

			public byte[] GetBytes()
			{
				List<byte> bytes = new List<byte>();

				bytes.Add(formatVersion); // version number
				bytes.AddRange(BitConverter.GetBytes(keyframeInterval));
				bytes.AddRange(Encoding.ASCII.GetBytes(firstFrame.client_name));
				bytes.Add(0);
				bytes.AddRange(SessionIdToBytes(firstFrame.sessionid));
				bytes.AddRange(IpAddressToBytes(firstFrame.sessionip));
				if (players.Count > 255) throw new Exception("Number of players doesn't fit in a byte.");
				bytes.Add((byte)players.Count);
				for (int i = 0; i < players.Count; i++)
				{
					string t = players[i];
					bytes.AddRange(Encoding.ASCII.GetBytes(t));
					bytes.Add(0);
				}

				bytes.AddRange(userids.GetBytes());
				bytes.AddRange(numbers.GetByteBytes());
				bytes.AddRange(levels.GetByteBytes());
				bytes.Add((byte)firstFrame.total_round_count);
				if (firstFrame.blue_round_score > 127 ||
				    firstFrame.orange_round_score > 127)
				{
					throw new Exception("Round scores don't fit.");
				}

				byte roundScores = (byte)firstFrame.blue_round_score;
				roundScores += (byte)(firstFrame.orange_round_score << 4);
				bytes.Add(roundScores);

				byte mapByte = firstFrame.private_match ? (byte)1 : (byte)0;
				mapByte += (byte)((byte)Enum.Parse<MapName>(firstFrame.map_name) << 1);
				bytes.Add(mapByte);

				return bytes.ToArray();
			}
		}


		public class ButterFrame
		{
			private g_Instance frame;
			public ButterFrame lastButterFrame;
			private ButterFileHeader butterHeader;


			private byte _gameStatusByte;
			private byte[] _pointsBytes;
			private byte[] _pauseAndRestartsBytes;
			private byte[] _inputBytes;
			private byte[] _lastScoreBytes;
			private byte[] _lastThrowBytes;
			private byte[] _vrPlayerBytes;
			private byte[] _discBytes;

			/// <summary>
			/// Creates a new Butter frame from a decompressed frame class
			/// </summary>
			/// <param name="frame">The original data</param>
			/// <param name="lastButterFrame">Previous butter frame to do diffs with. Pass in null to avoid diffs</param>
			/// <param name="butterHeader">The butter header, which contains player dictionaries</param>
			public ButterFrame(g_Instance frame, ButterFrame lastButterFrame, ButterFileHeader butterHeader)
			{
				this.frame = frame;
				this.lastButterFrame = lastButterFrame;
				this.butterHeader = butterHeader;
			}

			public byte[] GetBytes()
			{
				List<byte> bytes = new List<byte>();
				if (lastButterFrame == null)
				{
					bytes.AddRange(BitConverter.GetBytes((ushort)0xFEFC));
				}
				else
				{
					bytes.AddRange(BitConverter.GetBytes((ushort)0xFEFE));
				}

				bytes.AddRange(
					BitConverter.GetBytes((Half)(frame.game_clock - (lastButterFrame?.frame.game_clock ?? 0))));
				byte inclusionBitmask = 0;
				inclusionBitmask +=
					(byte)((frame.game_status != lastButterFrame?.frame.game_status ? 1 : 0) << 0); // game_status
				inclusionBitmask +=
					(byte)((frame.blue_points != lastButterFrame?.frame.blue_points ||
					        frame.orange_points != lastButterFrame.frame.orange_points
						? 1
						: 0) << 1); // blue or orange points
				inclusionBitmask +=
					(byte)((PauseAndRestartsBytes.SameAs(lastButterFrame?.PauseAndRestartsBytes) ? 0 : 1) <<
					       2); // pause and restarts
				inclusionBitmask += (byte)((InputBytes.SameAs(lastButterFrame?.InputBytes) ? 0 : 1) << 3); // inputs
				inclusionBitmask +=
					(byte)((LastScoreBytes.SameAs(lastButterFrame?.LastScoreBytes) ? 0 : 1) << 4); // Last Score
				inclusionBitmask +=
					(byte)((LastThrowBytes.SameAs(lastButterFrame?.LastThrowBytes) ? 0 : 1) << 5); // last throw
				inclusionBitmask +=
					(byte)((VrPlayerBytes.SameAs(lastButterFrame?.VrPlayerBytes) ? 0 : 1) << 6); // vr player
				inclusionBitmask += (byte)((DiscBytes.SameAs(lastButterFrame?.DiscBytes) ? 0 : 1) << 7); // disc
				bytes.Add(inclusionBitmask);


				if ((inclusionBitmask & (1 << 0)) > 0)
				{
					bytes.Add(GameStatusToByte(frame.game_status));
				}

				if ((inclusionBitmask & (1 << 1)) > 0)
				{
					bytes.Add((byte)frame.blue_points);
					bytes.Add((byte)frame.orange_points);
				}

				// Pause and restarts
				if ((inclusionBitmask & (1 << 2)) > 0)
				{
					bytes.AddRange(PauseAndRestartsBytes);
				}

				// Inputs
				if ((inclusionBitmask & (1 << 3)) > 0)
				{
					bytes.AddRange(InputBytes);
				}

				// Last Score
				if ((inclusionBitmask & (1 << 4)) > 0)
				{
					bytes.AddRange(LastScoreBytes);
				}

				// Last Throw
				if ((inclusionBitmask & (1 << 5)) > 0)
				{
					bytes.AddRange(LastThrowBytes);
				}

				// VR Player
				if ((inclusionBitmask & (1 << 6)) > 0)
				{
					bytes.AddRange(VrPlayerBytes);
				}

				// Disc
				if ((inclusionBitmask & (1 << 7)) > 0)
				{
					bytes.AddRange(DiscBytes);
				}

				byte teamDataBitmask = 0;
				teamDataBitmask |= (byte)(frame.teams[0].possession ? 1 : 0);
				teamDataBitmask |= (byte)((frame.teams[1].possession ? 1 : 0) << 1);
				// TODO check team stats diff
				// Team stats included
				bool[] teamStatsIncluded = new bool[3];
				teamStatsIncluded[0] = frame.teams[0]?.stats != null && !StatsBytes(frame.teams[0].stats)
					.SameAs(StatsBytes(lastButterFrame?.frame.teams[0].stats));
				teamStatsIncluded[1] = frame.teams[1]?.stats != null && !StatsBytes(frame.teams[1].stats)
					.SameAs(StatsBytes(lastButterFrame?.frame.teams[1].stats));
				teamStatsIncluded[2] = frame.teams[2]?.stats != null && !StatsBytes(frame.teams[2].stats)
					.SameAs(StatsBytes(lastButterFrame?.frame.teams[2].stats));
				teamDataBitmask |= (byte)((byte)(teamStatsIncluded[0] ? 1 : 0) << 2);
				teamDataBitmask |= (byte)((byte)(teamStatsIncluded[1] ? 1 : 0) << 3);
				teamDataBitmask |= (byte)((byte)(teamStatsIncluded[2] ? 1 : 0) << 4);
				bytes.Add(teamDataBitmask);


				// add team data
				for (int i = 0; i < 3; i++)
				{
					if (teamStatsIncluded[i])
					{
						bytes.AddRange(StatsBytes(frame.teams[i].stats, lastButterFrame?.frame.teams[i].stats));
					}

					bytes.Add((byte)(frame.teams[i]?.players?.Count ?? 0));
					if ((frame.teams[i]?.players?.Count ?? 0) > 0)
					{
						foreach (g_Player player in frame.teams[i].players)
						{
							bytes.Add(butterHeader.GetPlayerIndex(player.name));
							bytes.Add((byte)player.playerid);

							g_Player lastFramePlayer = lastButterFrame?.frame.GetPlayer(player.name);

							Vector3 vel = player.velocity.ToVector3() - (lastFramePlayer?.velocity.ToVector3() ??
							              Vector3.Zero);

							List<bool> playerStateBitmask = new List<bool>()
							{
								player.possession,
								player.blocking,
								player.stunned,
								player.invulnerable,
								!StatsBytes(player.stats).SameAs(StatsBytes(lastFramePlayer?.stats)),
								lastFramePlayer == null || player.ping == lastFramePlayer.ping &&
								Math.Abs(player.packetlossratio - lastFramePlayer.packetlossratio) < float.Epsilon,
								lastFramePlayer == null || player.holding_left == lastFramePlayer.holding_left &&
								player.holding_right == lastFramePlayer.holding_right,
								vel.LengthSquared() > .0001f
							};
							bytes.Add(playerStateBitmask.GetBitmasks()[0]);


							if (playerStateBitmask[4])
							{
								bytes.AddRange(StatsBytes(player.stats, lastFramePlayer?.stats));
							}

							if (playerStateBitmask[5])
							{
								bytes.AddRange(
									BitConverter.GetBytes((ushort)(player.ping - (lastFramePlayer?.ping ?? 0))));
								bytes.AddRange(BitConverter.GetBytes(
									(Half)(player.packetlossratio - (lastFramePlayer?.packetlossratio ?? 0))));
							}

							if (playerStateBitmask[6])
							{
								bytes.Add(butterHeader.HoldingToByte(player.holding_left));
								bytes.Add(butterHeader.HoldingToByte(player.holding_right));
							}

							if (playerStateBitmask[7])
							{
								bytes.AddRange((vel).GetHalfBytes());
							}

							List<bool> playerPoseBitmask = new List<bool>()
							{
								lastFramePlayer == null ||
								Vector3.DistanceSquared(player.head.Position, lastFramePlayer.head.Position) > .0001f,
								lastFramePlayer == null ||
								Vector3.DistanceSquared(player.body.Position - player.head.Position,
									lastFramePlayer.body.Position - lastFramePlayer.head.Position) > .0001f,
								lastFramePlayer == null ||
								Vector3.DistanceSquared(player.lhand.Position - player.head.Position,
									lastFramePlayer.lhand.Position - lastFramePlayer.head.Position) > .0001f,
								lastFramePlayer == null ||
								Vector3.DistanceSquared(player.rhand.Position - player.head.Position,
									lastFramePlayer.rhand.Position - lastFramePlayer.head.Position) > .0001f,
							};
							bytes.Add(playerPoseBitmask.GetBitmasks()[0]);

							if (playerPoseBitmask[1])
							{
								includedCount++;
							}
							else
							{
								excludedCount++;
							}

							byte[] headBytes = PoseToBytes(player.head, lastFramePlayer?.head);
							byte[] bodyBytes = PoseToBytes(player.body, lastFramePlayer?.body);
							byte[] lHandBytes = PoseToBytes(player.lhand, lastFramePlayer?.lhand);
							byte[] rHandBytes = PoseToBytes(player.rhand, lastFramePlayer?.rhand);
							bytes.AddRange(playerPoseBitmask[0] ? headBytes : headBytes.Skip(6));
							bytes.AddRange(playerPoseBitmask[1] ? bodyBytes : bodyBytes.Skip(6));
							bytes.AddRange(playerPoseBitmask[2] ? lHandBytes : lHandBytes.Skip(6));
							bytes.AddRange(playerPoseBitmask[3] ? rHandBytes : rHandBytes.Skip(6));
						}
					}
				}

				return bytes.ToArray();
			}

			private static byte[] StatsBytes(g_Stats stats, g_Stats lastStats = null)
			{
				if (stats == null) return null;
				List<byte> bytes = new List<byte>();

				if (stats.stuns > ushort.MaxValue) throw new Exception("Too many stuns to fit.");

				bytes.Add((byte)(stats.assists - (lastStats?.assists ?? 0)));
				bytes.Add((byte)(stats.blocks - (lastStats?.blocks ?? 0)));
				bytes.Add((byte)(stats.catches - (lastStats?.catches ?? 0)));
				bytes.Add((byte)(stats.goals - (lastStats?.goals ?? 0)));
				bytes.Add((byte)(stats.interceptions - (lastStats?.interceptions ?? 0)));
				bytes.Add((byte)(stats.passes - (lastStats?.passes ?? 0)));
				bytes.Add((byte)(stats.points - (lastStats?.points ?? 0)));
				bytes.Add((byte)(stats.saves - (lastStats?.saves ?? 0)));
				bytes.Add((byte)(stats.steals - (lastStats?.steals ?? 0)));
				bytes.Add((byte)(stats.shots_taken - (lastStats?.shots_taken ?? 0)));

				bytes.AddRange(BitConverter.GetBytes((Half)(stats.possession_time - (lastStats?.possession_time ?? 0))));
				bytes.AddRange(BitConverter.GetBytes((ushort)Math.Clamp(stats.stuns - (lastStats?.stuns ?? 0), 0,
					ushort.MaxValue)));
				return bytes.ToArray();
			}

			private byte[] DiscBytes
			{
				get
				{
					if (_discBytes == null)
					{
						List<byte> bytes = new List<byte>();

						bytes.AddRange(PoseToBytes(
							frame.disc.position.ToVector3(),
							frame.disc.forward.ToVector3(),
							frame.disc.up.ToVector3(),
							lastButterFrame?.frame.disc.position.ToVector3()
						));

						bytes.AddRange((frame.disc.velocity.ToVector3() - (lastButterFrame?.frame.disc.velocity.ToVector3() ?? Vector3.Zero)).GetHalfBytes());

						_discBytes = bytes.ToArray();
					}

					return _discBytes;
				}
			}

			private byte[] VrPlayerBytes
			{
				get
				{
					if (_vrPlayerBytes == null)
					{
						List<byte> bytes = new List<byte>();

						bytes.AddRange(PoseToBytes(
							frame.player.vr_position.ToVector3(),
							frame.player.vr_forward.ToVector3(),
							frame.player.vr_up.ToVector3(),
							lastButterFrame?.frame.player.vr_position.ToVector3()
						));

						_vrPlayerBytes = bytes.ToArray();
					}

					return _vrPlayerBytes;
				}
			}

			private byte[] LastThrowBytes
			{
				get
				{
					if (_lastThrowBytes == null)
					{
						List<byte> bytes = new List<byte>();
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.arm_speed));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.total_speed));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.off_axis_spin_deg));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.wrist_throw_penalty));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.rot_per_sec));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.pot_speed_from_rot));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.speed_from_arm));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.speed_from_movement));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.speed_from_wrist));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.wrist_align_to_throw_deg));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.throw_align_to_movement_deg));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.off_axis_penalty));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_throw.throw_move_penalty));
						_lastThrowBytes = bytes.ToArray();
					}

					return _lastThrowBytes;
				}
			}

			private byte[] LastScoreBytes
			{
				get
				{
					if (_lastScoreBytes == null)
					{
						List<byte> bytes = new List<byte>();
						byte b = TeamToTeamIndex(frame.last_score.team);
						b |= (byte)((frame.last_score.point_amount == 2 ? 0 : 1) << 2);
						if (!Enum.TryParse(frame.last_score.goal_type.Replace(" ", "_"), out GoalType type))
						{
							type = GoalType.unknown;
						}

						b |= (byte)((byte)type << 3);
						bytes.Add(b);

						bytes.Add(butterHeader.GetPlayerIndex(frame.last_score.person_scored));
						bytes.Add(butterHeader.GetPlayerIndex(frame.last_score.assist_scored));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_score.disc_speed));
						bytes.AddRange(BitConverter.GetBytes((Half)frame.last_score.distance_thrown));
						_lastScoreBytes = bytes.ToArray();
					}

					return _lastScoreBytes;
				}
			}

			private byte[] InputBytes
			{
				get
				{
					if (_inputBytes == null)
					{
						List<byte> bytes = new List<byte>();
						bytes.AddRange(BitConverter.GetBytes(
							(Half)(frame.left_shoulder_pressed - (lastButterFrame?.frame.left_shoulder_pressed ?? 0))));
						bytes.AddRange(BitConverter.GetBytes((Half)(frame.right_shoulder_pressed -
							(lastButterFrame?.frame.right_shoulder_pressed ?? 0))));
						bytes.AddRange(BitConverter.GetBytes((Half)(frame.left_shoulder_pressed2 -
							(lastButterFrame?.frame.left_shoulder_pressed2 ?? 0))));
						bytes.AddRange(BitConverter.GetBytes((Half)(frame.right_shoulder_pressed2 -
							(lastButterFrame?.frame.right_shoulder_pressed2 ?? 0))));
						_inputBytes = bytes.ToArray();
					}

					return _inputBytes;
				}
			}

			private byte[] PauseAndRestartsBytes
			{
				get
				{
					if (_pauseAndRestartsBytes == null)
					{
						List<byte> bytes = new List<byte>();
						byte pauses = 0;
						pauses |= (byte)((frame.blue_team_restart_request ? 1 : 0) << 0);
						pauses |= (byte)((frame.orange_team_restart_request ? 1 : 0) << 1);
						pauses |= (byte)(TeamToTeamIndex(frame.pause.paused_requested_team) << 2);
						pauses |= (byte)(TeamToTeamIndex(frame.pause.unpaused_team) << 4);
						pauses |= (byte)(PausedStateToByte(frame.pause.paused_state) << 6);
						bytes.Add(pauses);

						bytes.AddRange(BitConverter.GetBytes(
							(Half)(frame.pause.paused_timer - lastButterFrame?.frame.pause.paused_timer ?? 0)));
						bytes.AddRange(BitConverter.GetBytes(
							(Half)(frame.pause.unpaused_timer - lastButterFrame?.frame.pause.unpaused_timer ?? 0)));
						_pauseAndRestartsBytes = bytes.ToArray();
					}

					return _pauseAndRestartsBytes;
				}
			}


			/// <summary>
			/// This is only 2 bits
			/// </summary>
			public static byte TeamToTeamIndex(string team)
			{
				return team switch
				{
					"blue" => 0,
					"orange" => 1,
					"spectator" => 2,
					"none" => 3,
					_ => throw new Exception("Team index failed to parse")
				};
			}

			/// <summary>
			/// This is only 2 bits
			/// </summary>
			public static string TeamIndexToTeam(byte b)
			{
				return b switch
				{
					0 => "blue",
					1 => "orange",
					2 => "spectator",
					3 => "none",
					_ => throw new Exception("Team index failed to parse")
				};
			}

			/// <summary>
			/// This is only 2 bits
			/// </summary>
			public static byte PausedStateToByte(string team)
			{
				return team switch
				{
					"unpaused" => 0,
					"paused" => 1,
					"unpausing" => 2,
					"pausing" => 3, // TODO exists?
					_ => 3
				};
			}


			/// <summary>
			/// This is only 2 bits
			/// </summary>
			public static string ByteToPausedState(byte b)
			{
				return b switch
				{
					0 => "unpaused",
					1 => "paused",
					2 => "unpausing",
					3 => "pausing", // TODO exists?
					_ => "NOT FOUND"
				};
			}

			public static byte GameStatusToByte(string gameStatus)
			{
				return gameStatus switch
				{
					"" => 1,
					"pre_match" => 2,
					"round_start" => 3,
					"playing" => 4,
					"score" => 5,
					"round_over" => 6,
					"post_match" => 7,
					"pre_sudden_death" => 8,
					"sudden_death" => 9,
					"post_sudden_death" => 10,
					_ => 0
				};
			}

			public static string ByteToGameStatus(byte gameStatusByte)
			{
				return gameStatusByte switch
				{
					1 => "",
					2 => "pre_match",
					3 => "round_start",
					4 => "playing",
					5 => "score",
					6 => "round_over",
					7 => "post_match",
					8 => "pre_sudden_death",
					9 => "sudden_death",
					10 => "post_sudden_death",
					_ => "NOT FOUND"
				};
			}

			public static byte[] PoseToBytes(g_Transform transform, g_Transform lastTransform)
			{
				// Quaternion rot = QuaternionLookRotation(transform.forward.ToVector3(), transform.up.ToVector3());
				// Quaternion lastRot = QuaternionLookRotation(lastTransform.forward.ToVector3(), lastTransform.up.ToVector3());
				// Quaternion final = new Quaternion(rot.X - lastRot.X, )
				return PoseToBytes(
					transform.Position - (lastTransform?.Position ?? Vector3.Zero),
					transform.forward.ToVector3(),
					transform.up.ToVector3()
				);
			}

			public static byte[] PoseToBytes(g_Transform transform)
			{
				return PoseToBytes(
					transform.Position,
					transform.forward.ToVector3(),
					transform.up.ToVector3()
				);
			}

			public static byte[] PoseToBytes(Vector3 pos, Vector3 forward, Vector3 up, Vector3? lastPos)
			{
				return PoseToBytes(
					pos - (lastPos ?? Vector3.Zero),
					forward,
					up
				);
			}

			public static byte[] PoseToBytes(Vector3 pos, Vector3 forward, Vector3 up)
			{
				return PoseToBytes(pos, QuaternionLookRotation(forward, up));
			}

			public static byte[] PoseToBytes(Vector3 pos, Quaternion rot)
			{
				List<byte> bytes = new List<byte>();
				bytes.AddRange(BitConverter.GetBytes((Half)pos.X));
				bytes.AddRange(BitConverter.GetBytes((Half)pos.Y));
				bytes.AddRange(BitConverter.GetBytes((Half)pos.Z));

				bytes.AddRange(SmallestThree(rot));

				return bytes.ToArray();
			}

			public static Quaternion QuaternionLookRotation(Vector3 forward, Vector3 up)
			{
				forward /= forward.Length();

				Vector3 vector = Vector3.Normalize(forward);
				Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
				Vector3 vector3 = Vector3.Cross(vector, vector2);
				var m00 = vector2.X;
				var m01 = vector2.Y;
				var m02 = vector2.Z;
				var m10 = vector3.X;
				var m11 = vector3.Y;
				var m12 = vector3.Z;
				var m20 = vector.X;
				var m21 = vector.Y;
				var m22 = vector.Z;


				float num8 = (m00 + m11) + m22;
				var quaternion = new Quaternion();
				if (num8 > 0f)
				{
					var num = (float)Math.Sqrt(num8 + 1f);
					quaternion.W = num * 0.5f;
					num = 0.5f / num;
					quaternion.X = (m12 - m21) * num;
					quaternion.Y = (m20 - m02) * num;
					quaternion.Z = (m01 - m10) * num;
					return quaternion;
				}

				if ((m00 >= m11) && (m00 >= m22))
				{
					var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
					var num4 = 0.5f / num7;
					quaternion.X = 0.5f * num7;
					quaternion.Y = (m01 + m10) * num4;
					quaternion.Z = (m02 + m20) * num4;
					quaternion.W = (m12 - m21) * num4;
					return quaternion;
				}

				if (m11 > m22)
				{
					var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
					var num3 = 0.5f / num6;
					quaternion.X = (m10 + m01) * num3;
					quaternion.Y = 0.5f * num6;
					quaternion.Z = (m21 + m12) * num3;
					quaternion.W = (m20 - m02) * num3;
					return quaternion;
				}

				var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
				var num2 = 0.5f / num5;
				quaternion.X = (m20 + m02) * num2;
				quaternion.Y = (m21 + m12) * num2;
				quaternion.Z = 0.5f * num5;
				quaternion.W = (m01 - m10) * num2;
				return quaternion;
			}

			public static byte[] SmallestThree(Quaternion q)
			{
				// make an array of the components
				float[] components =
				{
					q.X, q.Y, q.Z, q.W
				};

				// find component with largest absolute value
				float max = 0;
				int maxIndex = 0;
				int sign = 1;
				float[] absComponents = components.Select(MathF.Abs).ToArray();
				for (int i = 0; i < 4; i++)
				{
					if (absComponents[i] > max)
					{
						max = absComponents[i];
						maxIndex = i;
						sign = components[i] < 0 ? -1 : 1;
					}
				}

				// This is 32 bits, which is used to store the rotation
				int data = 0;

				// store the index as 2 bits
				data |= (byte)maxIndex;

				float decimals = 1000f;

				// store the other three components as 10-bit numbers
				for (int i = 0; i < 4; i++)
				{
					if (i != maxIndex)
					{
						// TODO test if these rotations are correct
						ushort val = (ushort)(components[i] * sign * decimals + decimals / 2);
						data |= val << ((i * 10) + 2);
					}
				}

				return BitConverter.GetBytes(data);
			}
		}


		public void AddFrame(g_Instance frame)
		{
			// if there is no data yet, add this frame to the file header
			if (fileHeader == null)
			{
				fileHeader = new ButterFileHeader(frame);
				frames = new List<ButterFrame>();
			}
			else
			{
				fileHeader.ConsiderNewFrame(frame);
			}

			ButterFrame butterFrame = new ButterFrame(
				frame,
				// if on a keyframe interval and previous frames exist
				(frames.Count % fileHeader.keyframeInterval != 0) && frames.Count > 0 ? frames.Last() : null,
				fileHeader
			);

			frames.Add(butterFrame);
		}

		public byte[] GetBytes()
		{
			List<byte> bytes = new List<byte>();
			bytes.AddRange(fileHeader.GetBytes());
			double byteTotal = 0;
			foreach (ButterFrame frame in frames)
			{
				byte[] newBytes = frame.GetBytes();
				bytes.AddRange(newBytes);
				byteTotal += newBytes.Length;
			}

			Console.WriteLine();

			Console.WriteLine($"Average frame size:\t{byteTotal / frames.Count:N1} bytes");

			Console.WriteLine($"Included:\t{includedCount:N0}");
			Console.WriteLine($"Excluded:\t{excludedCount:N0}");

			return bytes.ToArray();
		}

		public static List<g_Instance> FromBytes(BinaryReader input)
		{
			List<g_Instance> l = new List<g_Instance>();

			Butter b = new Butter
			{
				fileHeader = new ButterFileHeader
				{
					// read header
					formatVersion = input.ReadByte(),
					keyframeInterval = input.ReadUInt16()
				}
			};

			g_Instance firstFrame = new g_Instance
			{
				client_name = input.ReadASCIIString(),
				sessionid = input.ReadSessionId(),
				sessionip = input.ReadIpAddress()
			};
			byte playerCount = input.ReadByte();


			for (int i = 0; i < playerCount; i++)
			{
				b.fileHeader.players.Add(input.ReadASCIIString());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.fileHeader.userids.Add(input.ReadUInt64());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.fileHeader.numbers.Add(input.ReadByte());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.fileHeader.levels.Add(input.ReadByte());
			}

			firstFrame.total_round_count = input.ReadByte();
			byte roundScores = input.ReadByte();
			firstFrame.blue_round_score = roundScores & 0xF;
			firstFrame.orange_round_score = (roundScores >> 4) & 0xF;

			byte mapByte = input.ReadByte();
			firstFrame.private_match = (mapByte & 1) == 1;
			firstFrame.map_name = ((MapName)(mapByte >> 1)).ToString();
			firstFrame.match_type = MatchType(firstFrame.map_name, firstFrame.private_match);
			// TODO arena or combat from map name

			b.fileHeader.firstFrame = firstFrame;

			g_Instance lastKeframe = null;

			// reads one frame at a time
			while (!input.EOF())
			{
				ushort headerByte = input.ReadUInt16();
				if (headerByte != 0xFEFC && headerByte != 0xFEFE)
				{
					throw new Exception("Not reading at beginning of frame, maybe wrong frame size");
				}

				bool isKeyframe = headerByte == 0xFEFC;

				if (!isKeyframe && lastKeframe == null)
				{
					throw new Exception("This isn't a keyframe, but no previous keyframe found.");
				}


				// g_Instance f = isKeyframe ? new g_Instance() : lastKeframe.Copy();
				g_Instance f = new g_Instance();

				f.client_name = b.fileHeader.firstFrame.client_name;
				f.sessionid = b.fileHeader.firstFrame.sessionid;
				f.sessionip = b.fileHeader.firstFrame.sessionip;
				f.total_round_count = b.fileHeader.firstFrame.total_round_count;
				f.blue_round_score = b.fileHeader.firstFrame.blue_round_score;
				f.orange_round_score = b.fileHeader.firstFrame.orange_round_score;
				f.private_match = b.fileHeader.firstFrame.private_match;
				f.map_name = b.fileHeader.firstFrame.map_name;
				f.match_type = b.fileHeader.firstFrame.match_type;

				f.game_clock = (float)input.ReadHalf();
				f.game_clock_display = f.game_clock.ToGameClockDisplay();
				if (!isKeyframe) f.game_clock += lastKeframe.game_clock;
				byte inclusionBitmask = input.ReadByte();

				if ((inclusionBitmask & (1 << 0)) > 0)
				{
					f.game_status = ButterFrame.ByteToGameStatus(input.ReadByte());
				}

				if ((inclusionBitmask & (1 << 1)) > 0)
				{
					f.blue_points = input.ReadByte();
					f.orange_points = input.ReadByte();
				}

				// Pause and restarts
				if ((inclusionBitmask & (1 << 2)) > 0)
				{
					byte pauses = input.ReadByte();
					f.blue_team_restart_request = (pauses & 0b1) > 0;
					f.orange_team_restart_request = (pauses & 0b10) > 0;
					f.pause = new g_Pause
					{
						paused_requested_team = ButterFrame.TeamIndexToTeam((byte)((pauses & 0b1100) >> 2)),
						unpaused_team = ButterFrame.TeamIndexToTeam((byte)((pauses & 0b110000) >> 4)),
						paused_state = ButterFrame.ByteToPausedState((byte)((pauses & 0b11000000) >> 6)),
						paused_timer = (float)input.ReadHalf(),
						unpaused_timer = (float)input.ReadHalf(),
					};
				}

				// Inputs
				if ((inclusionBitmask & (1 << 3)) > 0)
				{
					f.left_shoulder_pressed = (float)input.ReadHalf();
					f.right_shoulder_pressed = (float)input.ReadHalf();
					f.left_shoulder_pressed2 = (float)input.ReadHalf();
					f.right_shoulder_pressed2 = (float)input.ReadHalf();
				}

				// Last Score
				if ((inclusionBitmask & (1 << 4)) > 0)
				{
					byte lastScoreByte = input.ReadByte();
					f.last_score = new g_Score
					{
						team = ButterFrame.TeamIndexToTeam((byte)(lastScoreByte & 0b11)),
						point_amount = (lastScoreByte & 0b100) > 0 ? 3 : 2,
						goal_type = ((GoalType)((lastScoreByte & 0b11111000) >> 3)).ToString().Replace("_", " "),
						person_scored = b.fileHeader.GetPlayerName(input.ReadByte()),
						assist_scored = b.fileHeader.GetPlayerName(input.ReadByte()),
						disc_speed = (float)input.ReadHalf(),
						distance_thrown = (float)input.ReadHalf(),
					};
				}

				// Last Throw
				if ((inclusionBitmask & (1 << 5)) > 0)
				{
					f.last_throw = new g_LastThrow
					{
						arm_speed = (float)input.ReadHalf(),
						total_speed = (float)input.ReadHalf(),
						off_axis_spin_deg = (float)input.ReadHalf(),
						wrist_throw_penalty = (float)input.ReadHalf(),
						rot_per_sec = (float)input.ReadHalf(),
						pot_speed_from_rot = (float)input.ReadHalf(),
						speed_from_arm = (float)input.ReadHalf(),
						speed_from_movement = (float)input.ReadHalf(),
						speed_from_wrist = (float)input.ReadHalf(),
						wrist_align_to_throw_deg = (float)input.ReadHalf(),
						throw_align_to_movement_deg = (float)input.ReadHalf(),
						off_axis_penalty = (float)input.ReadHalf(),
						throw_move_penalty = (float)input.ReadHalf()
					};
				}

				// VR Player
				if ((inclusionBitmask & (1 << 6)) > 0)
				{
					(Vector3 p, Quaternion q) = input.ReadPose();
					f.player = new g_Playspace
					{
						vr_position = p.ToFloatArray(),
						vr_forward = q.Forward().ToFloatArray(),
						vr_left = q.Left().ToFloatArray(),
						vr_up = q.Up().ToFloatArray(),
					};
					// TODO get diff from previous frames
				}

				// Disc
				if ((inclusionBitmask & (1 << 7)) > 0)
				{
					(Vector3 p, Quaternion q) = input.ReadPose();

					f.disc = new g_Disc
					{
						position = p.ToFloatArray().ToList(),
						forward = q.Forward().ToFloatArray().ToList(),
						left = q.Left().ToFloatArray().ToList(),
						up = q.Up().ToFloatArray().ToList(),
						// TODO get vectors from quaternion
						velocity = new List<float>()
						{
							// TODO check this order
							(float)input.ReadHalf(),
							(float)input.ReadHalf(),
							(float)input.ReadHalf(),
						}
					};
				}

				byte teamDataBitmask = input.ReadByte();
				f.teams = new List<g_Team>()
				{
					new g_Team(),
					new g_Team(),
					new g_Team(),
				};
				f.teams[0].possession = (teamDataBitmask & 0b1) > 0;
				f.teams[1].possession = (teamDataBitmask & 0b10) > 0;

				// Team stats included
				bool[] teamStatsIncluded = new bool[3];
				teamStatsIncluded[0] = (teamDataBitmask & 0b100) > 0;
				teamStatsIncluded[1] = (teamDataBitmask & 0b1000) > 0;
				teamStatsIncluded[2] = (teamDataBitmask & 0b10000) > 0;

				// add team data
				for (int i = 0; i < 3; i++)
				{
					if (teamStatsIncluded[i])
					{
						f.teams[i].stats = input.ReadStats();
						// TODO diff
					}

					int teamPlayerCount = input.ReadByte();

					f.teams[i].players = new List<g_Player>();
					for (int j = 0; j < teamPlayerCount; j++)
					{
						// TODO match to previous keyframe and diff

						byte fileIndex = input.ReadByte();

						g_Player p = new g_Player
						{
							name = b.fileHeader.GetPlayerName(fileIndex),
							playerid = input.ReadByte(),
							level = b.fileHeader.GetPlayerLevel(fileIndex),
							userid = b.fileHeader.GetUserId(fileIndex),
						};

						byte playerStateBitmask = input.ReadByte();
						p.possession = (playerStateBitmask & 0b1) > 0;
						p.blocking = (playerStateBitmask & 0b10) > 0;
						p.stunned = (playerStateBitmask & 0b100) > 0;
						p.invulnerable = (playerStateBitmask & 0b1000) > 0;

						if ((playerStateBitmask & (1 << 4)) > 0)
						{
							p.stats = input.ReadStats();
						}

						if ((playerStateBitmask & (1 << 5)) > 0)
						{
							p.ping = input.ReadUInt16();
							p.packetlossratio = (float)input.ReadHalf();
						}

						if ((playerStateBitmask & (1 << 6)) > 0)
						{
							p.holding_left = b.fileHeader.ByteToHolding(input.ReadByte());
							p.holding_right = b.fileHeader.ByteToHolding(input.ReadByte());
						}

						if ((playerStateBitmask & (1 << 7)) > 0)
						{
							p.velocity = input.ReadVector3Half().ToFloatArray().ToList();
							// p.velocity = new List<float>()
							// {
							// 	// TODO check this order
							// 	(float)input.ReadHalf(),
							// 	(float)input.ReadHalf(),
							// 	(float)input.ReadHalf(),
							// };
						}
						// p.velocity ??= new List<float>(){0,0,0};

						byte playerPoseBitmask = input.ReadByte();

						p.head = new g_Transform();
						p.body = new g_Transform();
						p.lhand = new g_Transform();
						p.rhand = new g_Transform();

						if ((playerPoseBitmask & 1 << 0) > 0)
						{
							p.head.position = input.ReadVector3Half().ToFloatArray().ToList();
						}

						Quaternion q = input.ReadSmallestThree();
						p.head.forward = q.Forward().ToFloatArray().ToList();
						p.head.left = q.Left().ToFloatArray().ToList();
						p.head.up = q.Up().ToFloatArray().ToList();

						if ((playerPoseBitmask & (1 << 1)) > 0)
						{
							p.body.position = input.ReadVector3Half().ToFloatArray().ToList();
						}

						q = input.ReadSmallestThree();
						p.body.forward = q.Forward().ToFloatArray().ToList();
						p.body.left = q.Left().ToFloatArray().ToList();
						p.body.up = q.Up().ToFloatArray().ToList();

						if ((playerPoseBitmask & (1 << 2)) > 0)
						{
							p.lhand.pos = input.ReadVector3Half().ToFloatArray().ToList();
						}

						q = input.ReadSmallestThree();
						p.lhand.forward = q.Forward().ToFloatArray().ToList();
						p.lhand.left = q.Left().ToFloatArray().ToList();
						p.lhand.up = q.Up().ToFloatArray().ToList();

						if ((playerPoseBitmask & (1 << 3)) > 0)
						{
							p.rhand.pos = input.ReadVector3Half().ToFloatArray().ToList();
						}

						q = input.ReadSmallestThree();
						p.rhand.forward = q.Forward().ToFloatArray().ToList();
						p.rhand.left = q.Left().ToFloatArray().ToList();
						p.rhand.up = q.Up().ToFloatArray().ToList();

						// TODO assign to correct player if array already exists
						f.teams[i].players.Add(p);
					}
				}

				if (isKeyframe) lastKeframe = f;
				l.Add(f);
			}

			return l;
		}
	}

	public static class BitConverterExtensions
	{
		/// <summary>
		/// Converts a list of floats to Halfs and then to bytes
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static byte[] GetHalfBytes(this IEnumerable<float> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (float val in values)
			{
				bytes.AddRange(BitConverter.GetBytes((Half)val));
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Converts a Vector3 to Halfs and then to bytes
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] GetHalfBytes(this Vector3 value)
		{
			List<byte> bytes = new List<byte>();
			bytes.AddRange(BitConverter.GetBytes((Half)value.X));
			bytes.AddRange(BitConverter.GetBytes((Half)value.Y));
			bytes.AddRange(BitConverter.GetBytes((Half)value.Z));
			return bytes.ToArray();
		}

		public static byte[] GetByteBytes(this IEnumerable<int> values)
		{
			return values.Select(val => (byte)val).ToArray();
		}

		public static byte[] GetBytes(this IEnumerable<int> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (int val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}

		public static byte[] GetBytes(this IEnumerable<ulong> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (ulong val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Compresses the list of bools into bytes using a bitmask
		/// </summary>
		public static byte[] GetBitmasks(this List<bool> values)
		{
			List<byte> bytes = new List<byte>();
			for (int b = 0; b < Math.Ceiling(values.Count / 8f); b++)
			{
				byte currentByte = 0;
				for (int bit = 0; bit < 8; bit++)
				{
					if (values.Count > b * 8 + bit)
					{
						currentByte |= (byte)((values[b * 8 + bit] ? 1 : 0) << bit);
					}
				}

				bytes.Add(currentByte);
			}

			return bytes.ToArray();
		}

		public static bool SameAs(this byte[] b1, byte[] b2)
		{
			if (b1 == null) return false;
			if (b2 == null) return false;
			if (b1.Length != b2.Length) return false;
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[i]) return false;
			}

			return true;
		}

		public static bool IsZero(this byte[] b)
		{
			if (b == null) throw new ArgumentException("Input array null");
			return b.All(t => t == 0);
		}

		public static string ReadASCIIString(this BinaryReader reader, int maxLength = 1024)
		{
			List<byte> str = new List<byte>();
			for (int i = 0; i < maxLength; i++)
			{
				byte lastByte = reader.ReadByte();
				if (lastByte == 0)
				{
					return Encoding.ASCII.GetString(str.ToArray());
				}

				str.Add(lastByte);
			}

			return Encoding.ASCII.GetString(str.ToArray());
		}

		public static string ReadSessionId(this BinaryReader reader)
		{
			string str = Butter.ButterFileHeader.ByteArrayToString(reader.ReadBytes(16));
			str = str.Insert(8, "-");
			str = str.Insert(13, "-");
			str = str.Insert(18, "-");
			str = str.Insert(23, "-");
			return str;
		}

		public static string ReadIpAddress(this BinaryReader reader)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 4; i++)
			{
				sb.Append(reader.ReadByte());
				if (i < 3) sb.Append('.');
			}

			return sb.ToString();
		}

		public static bool EOF(this BinaryReader binaryReader)
		{
			Stream bs = binaryReader.BaseStream;
			return (bs.Position == bs.Length);
		}

		public static (Vector3, Quaternion) ReadPose(this BinaryReader reader)
		{
			Vector3 p = new Vector3();
			Quaternion q = new Quaternion();
			p.X = (float)reader.ReadHalf();
			p.Y = (float)reader.ReadHalf();
			p.Z = (float)reader.ReadHalf();

			// TODO read quaternion
			reader.ReadInt32();

			return (p, q);
		}

		public static g_Stats ReadStats(this BinaryReader reader)
		{
			g_Stats stats = new g_Stats
			{
				assists = reader.ReadByte(),
				blocks = reader.ReadByte(),
				catches = reader.ReadByte(),
				goals = reader.ReadByte(),
				interceptions = reader.ReadByte(),
				passes = reader.ReadByte(),
				points = reader.ReadByte(),
				saves = reader.ReadByte(),
				steals = reader.ReadByte(),
				shots_taken = reader.ReadByte(),
				possession_time = (float)reader.ReadHalf(),
				stuns = reader.ReadUInt16(),
			};
			return stats;
		}


		public static Vector3 ReadVector3Half(this BinaryReader reader)
		{
			Vector3 p = new Vector3
			{
				X = (float)reader.ReadHalf(),
				Y = (float)reader.ReadHalf(),
				Z = (float)reader.ReadHalf()
			};

			return p;
		}


		public static Quaternion ReadSmallestThree(this BinaryReader reader)
		{
			int st = reader.ReadInt32();

			float decimals = 1000f;
			int maxIndex = st & 0b11;
			float f1 = (((ushort)((st & (0b1111111111 << 2)) >> 2)) - decimals/2) / decimals;
			float f2 = (((ushort)((st & (0b1111111111 << 12)) >> 12)) - decimals/2) / decimals;
			float f3 = (((ushort)((st & (0b1111111111 << 22)) >> 22)) - decimals/2) / decimals;
			float f4 = MathF.Sqrt(1 - f1 * f1 - f2 * f2 - f3 * f3);
			switch (maxIndex)
			{
				case 0:
					return new Quaternion(f4, f1, f2, f3);
				case 1:
					return new Quaternion(f1, f4, f2, f3);
				case 2:
					return new Quaternion(f1, f2, f4, f3);
				case 3:
					return new Quaternion(f1, f2, f3, f4);
				default:
					throw new Exception("Invalid index");
			};
		}

		// converts time in seconds to a string in the format "mm:ss.ms"
		public static string ToGameClockDisplay(this float time) {
			int minutes = (int)time / 60;
			int seconds = (int)time % 60;
			int milliseconds = (int)((time - (int)time) * 100);
			return $"{minutes:D2}:{seconds:D2}.{milliseconds:D2}";
		}

		// converts this quaternion to its forward vector
		public static Vector3 Forward(this Quaternion q) {
			return new Vector3(2 * (q.X * q.Z + q.W * q.Y), 2 * (q.Y * q.Z - q.W * q.X), 1 - 2 * (q.X * q.X + q.Y * q.Y));
		}

		// converts this quaternion to its left vector
		public static Vector3 Left(this Quaternion q) {
			return new Vector3(2 * (q.X * q.Y - q.W * q.Z), 1 - 2 * (q.X * q.X + q.Z * q.Z), 2 * (q.Y * q.Z + q.W * q.X));
		}

		// converts this quaternion to its up vector
		public static Vector3 Up(this Quaternion q) {
			return new Vector3(2 * (q.X * q.Z - q.W * q.Y), 1 - 2 * (q.X * q.X + q.Y * q.Y), 2 * (q.Y * q.Z + q.W * q.X));
		}
	}
}