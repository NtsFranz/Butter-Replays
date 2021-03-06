using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoVRAPI;
using Half = SystemHalf.Half;

namespace ButterReplays
{
	/// <summary>
	/// 🥛🥛🥛🥛🥛
	/// </summary>
	public class Milk
	{
		public MilkFileHeader fileHeader;
		public List<MilkFrame> frames;

		public Milk(Frame frame)
		{
			AddFrame(frame);
		}

		public enum MilkGameState : byte
		{
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

		/// <summary>
		/// Should be serialized to json when written
		/// </summary>
		public class MilkFileHeader
		{
			public Frame firstFrame;
			public List<string> players;
			public List<int> numbers;
			public List<int> levels;
			public List<long> userids;
			public byte headerByte;

			public MilkFileHeader(Frame firstFrame)
			{
				this.firstFrame = firstFrame;

				headerByte = 0;

				if (firstFrame.private_match)
				{
					headerByte |= 1 << 0;
				}

				if (firstFrame.match_type != "Echo Arena")
				{
					headerByte |= 1 << 1;
				}

				if (firstFrame.tournament_match)
				{
					headerByte |= 1 << 2;
				}

				players = new List<string>();
				numbers = new List<int>();
				levels = new List<int>();
				userids = new List<long>();

				foreach (Team team in firstFrame.teams)
				{
					if (team.players != null)
					{
						foreach (Player player in team.players)
						{
							players.Add(player.name);
							numbers.Add(player.number);
							levels.Add(player.level);
							userids.Add(player.userid);
						}
					}
				}
			}

			public void ConsiderNewFrame(Frame frame)
			{
				foreach (Team team in frame.teams)
				{
					if (team.players != null)
					{
						foreach (Player player in team.players)
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
			}

			public byte[] GetBytes()
			{

				List<byte> bytes = new List<byte>();

				bytes.Add(2);   // version number
				bytes.AddRange(Encoding.ASCII.GetBytes(firstFrame.client_name));
				bytes.Add(0);
				bytes.AddRange(Encoding.ASCII.GetBytes(firstFrame.sessionid));
				bytes.Add(0);
				bytes.AddRange(Encoding.ASCII.GetBytes(firstFrame.sessionip));
				bytes.Add(0);
				bytes.AddRange(Encoding.ASCII.GetBytes("[" + string.Join(",", players) + "]"));
				bytes.AddRange(numbers.GetBytes());
				bytes.AddRange(levels.GetBytes());
				bytes.AddRange(userids.GetBytes());

				return bytes.ToArray();
			}
		}

		public class MilkFrame
		{
			public byte[] header;
			public List<byte> data;
		}

		private static byte[] BuildFrameHeader(Frame frame)
		{
			List<byte> bytes = new List<byte> { 254, 253 };
			bytes.AddRange(BitConverter.GetBytes(frame.blue_points));
			bytes.AddRange(BitConverter.GetBytes(frame.orange_points));
			bytes.AddRange((new List<bool> { frame.blue_team_restart_request, frame.orange_team_restart_request }).GetBitmasks());

			// add last score
			bytes.AddRange(BitConverter.GetBytes(frame.last_score.disc_speed));
			bytes.Add((byte)Enum.Parse(typeof(Team.TeamColor), frame.last_score.team));
			bytes.AddRange(Encoding.ASCII.GetBytes(frame.last_score.goal_type));
			bytes.Add(0);
			bytes.AddRange(BitConverter.GetBytes(frame.last_score.point_amount));
			bytes.AddRange(BitConverter.GetBytes(frame.last_score.distance_thrown));
			bytes.AddRange(Encoding.ASCII.GetBytes(frame.last_score.person_scored));
			bytes.Add(0);
			bytes.AddRange(Encoding.ASCII.GetBytes(frame.last_score.assist_scored));
			bytes.Add(0);

			foreach (var team in frame.teams)
			{
				if (team.stats == null) team.stats = new Stats();
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.points));
				bytes.AddRange(BitConverter.GetBytes(team.stats.possession_time));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.interceptions));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.blocks));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.steals));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.catches));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.passes));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.saves));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.goals));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.stuns));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.assists));
				bytes.AddRange(BitConverter.GetBytes((byte)team.stats.shots_taken));

				if (team.players != null)
				{
					foreach (var player in team.players)
					{
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.points));
						bytes.AddRange(BitConverter.GetBytes(player.stats.possession_time));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.interceptions));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.blocks));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.steals));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.catches));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.passes));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.saves));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.goals));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.stuns));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.assists));
						bytes.AddRange(BitConverter.GetBytes((byte)player.stats.shots_taken));
					}
				}
			}

			return bytes.ToArray();
		}

		private static byte[] BuildChunk(Frame frame)
		{
			List<byte> bytes = new List<byte> { 253, 254 };
			List<bool> bools = new List<bool>();

			bytes.AddRange(BitConverter.GetBytes(frame.game_clock));

			// disc position, velocity, and orientation
			bytes.AddRange(frame.disc.position.GetHalfBytes());
			bytes.AddRange(frame.disc.left.GetHalfBytes());
			bytes.AddRange(frame.disc.up.GetHalfBytes());
			bytes.AddRange(frame.disc.forward.GetHalfBytes());
			bytes.AddRange(frame.disc.velocity.GetHalfBytes());

			// local vr player position
			bytes.AddRange(frame.player.vr_position.GetHalfBytes());
			bytes.AddRange(frame.player.vr_left.GetHalfBytes());
			bytes.AddRange(frame.player.vr_up.GetHalfBytes());
			bytes.AddRange(frame.player.vr_forward.GetHalfBytes());

			// game state
			if (string.IsNullOrEmpty(frame.game_status))
			{
				bytes.Add(0);
			}
			else
			{
				bytes.Add((byte)(MilkGameState)Enum.Parse(typeof(MilkGameState), frame.game_status));
			}

			// loop through all the player and add team- and player-specific info
			foreach (var team in frame.teams)
			{
				if (team.players != null)
				{
					foreach (var player in team.players)
					{
						List<float>[] vectors =
						{
							player.velocity,

							player.head.position,
							player.head.left,
							player.head.up,
							player.head.forward,

							player.body.position,
							player.body.left,
							player.body.up,
							player.body.forward,

							player.lhand.pos,
							player.lhand.left,
							player.lhand.up,
							player.lhand.forward,

							player.rhand.pos,
							player.rhand.left,
							player.rhand.up,
							player.rhand.forward
						};

						foreach (List<float> vector in vectors)
						{
							bytes.AddRange(vector.GetHalfBytes());
						}

						bytes.AddRange(BitConverter.GetBytes(player.ping));

						bools.Add(player.stunned);
						bools.Add(player.invulnerable);
						bools.Add(player.possession);
						bools.Add(player.blocking);
					}
				}
			}

			bytes.AddRange(bools.GetBitmasks());

			return bytes.ToArray();
		}

		public void AddFrame(Frame frame)
		{
			// if there is no data yet, add this frame to the file header
			if (fileHeader == null)
			{
				fileHeader = new MilkFileHeader(frame);
				frames = new List<MilkFrame>();
			}
			else
			{
				fileHeader.ConsiderNewFrame(frame);
			}

			byte[] newFrameHeader = BuildFrameHeader(frame);

			// if there are no frames yet, add this frame as a new milkframe
			// if there are other frames, check to see if the chunk size is too large and split to a new milkframe
			if (frames.Count == 0 || frames[frames.Count - 1].data.Count > 10000)
			{
				var milkFrame = new MilkFrame
				{
					header = newFrameHeader,
					data = BuildChunk(frame).ToList()
				};
				frames.Add(milkFrame);
			}
			// else just add this echoframe as another chunk
			else
			{
				frames[frames.Count - 1].data.AddRange(BuildChunk(frame));
			}
		}

		public byte[] GetBytes()
		{
			List<byte> bytes = new List<byte>();
			bytes.AddRange(fileHeader.GetBytes());
			foreach (var frame in frames)
			{
				bytes.AddRange(frame.header);
				bytes.AddRange(frame.data);
			}

			return bytes.ToArray();
		}
	}
}
