using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Butter
{
	/// <summary>
	/// A recreation of the JSON object given by EchoVR
	/// https://github.com/Ajedi32/echovr_api_docs
	/// </summary>
	public class g_Instance
	{
		/// <summary>
		/// This isn't in the api, just useful for recorded data
		/// </summary>
		public DateTime recorded_time { get; set; }
		/// <summary>
		/// Disc object at the given instance.
		/// </summary>
		public g_Disc disc { get; set; }
		public g_LastThrow last_throw { get; set; }
		public string sessionid { get; set; }
		public bool orange_team_restart_request { get; set; }
		public string sessionip { get; set; }
		/// <summary>
		/// The current state of the match
		/// { pre_match, round_start, playing, score, round_over, pre_sudden_death, sudden_death, post_sudden_death, post_match }
		/// </summary>
		public string game_status { get; set; }
		/// <summary>
		/// Game time as displayed in game.
		/// </summary>
		public string game_clock_display { get; set; }
		/// <summary>
		/// Time of remaining in match (in seconds)
		/// </summary>
		public float game_clock { get; set; }
		[JsonIgnore]
		public bool inLobby => map_name == "mpl_lobby_b2";
		public string match_type { get; set; }
		public string map_name { get; set; }
		public bool private_match { get; set; }
		public int orange_points { get; set; }
		public int total_round_count { get; set; }
		public int blue_round_score { get; set; }
		public int orange_round_score { get; set; }
		public g_Playspace player { get; set; }
		public g_Pause pause { get; set; }
		/// <summary>
		/// List of integers to determine who currently has possession.
		/// [ team, player ]
		/// </summary>
		public List<int> possession { get; set; }
		public bool tournament_match { get; set; }
		public bool left_shoulder_pressed {get;set;}
		public bool right_shoulder_pressed {get;set;}
		public bool left_shoulder_pressed2 {get;set;} 
		public bool right_shoulder_pressed2 {get;set;}
		public bool blue_team_restart_request { get; set; }
		/// <summary>
		/// Name of the oculus username recording.
		/// </summary>
		public string client_name { get; set; }
		public int blue_points { get; set; }
		/// <summary>
		/// Object containing data from the last goal made.
		/// </summary>
		public g_Score last_score { get; set; }
		public List<g_Team> teams { get; set; }

		
		[JsonIgnore]
		public List<g_Team> playerTeams =>
			new List<g_Team>
			{
				teams[0], teams[1]
			};

		/// <summary>
		/// Gets all the g_Player objects from both teams
		/// </summary>
		public List<g_Player> GetAllPlayers(bool includeSpectators = false)
		{
			List<g_Player> list = new List<g_Player>();
			list.AddRange(teams[(int)g_Team.TeamColor.blue].players);
			list.AddRange(teams[(int)g_Team.TeamColor.orange].players);
			if (includeSpectators)
			{
				list.AddRange(teams[2].players);
			}
			return list;
		}

		/// <summary>
		/// Get a player from all players their name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public g_Player GetPlayer(string name)
		{
			foreach (g_Team t in teams)
			{
				foreach (g_Player p in t.players)
				{
					if (p.name == name) return p;
				}
			}
			return null;
		}

		/// <summary>
		/// Get a player from all players their userid.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		public g_Player GetPlayer(ulong userid)
		{
			foreach (var t in teams)
			{
				foreach (var p in t.players)
				{
					if (p.userid == userid) return p;
				}
			}

			return null;
		}
		
		public g_Team GetTeam(string player_name)
		{
			foreach (g_Team t in teams)
			{
				foreach (g_Player p in t.players)
				{
					if (p.name == player_name) return t;
				}
			}

			return null;
		}

		public g_Team GetTeam(ulong userid)
		{
			foreach (g_Team t in teams)
			{
				foreach (g_Player p in t.players)
				{
					if (p.userid == userid) return t;
				}
			}

			return null;
		}

		public g_Team.TeamColor GetTeamColor(ulong userid)
		{
			foreach (g_Team t in teams)
			{
				foreach (g_Player p in t.players)
				{
					if (p.userid == userid) return t.color;
				}
			}

			return g_Team.TeamColor.spectator;
		}
	}

	public class g_InstanceSimple
	{
		public string sessionid { get; set; }
		public bool private_match { get; set; }

		/// <summary>
		/// Name of the oculus username spectating.
		/// </summary>
		public string client_name { get; set; }
	}

	/// <summary>
	/// Object describing the disc at the given instant. 
	/// </summary>
	public class g_Disc
	{
		/// <summary>
		/// A 3 element list of floats representing the disc's position relative to the center of the map.
		/// < X, Y, Z >
		/// </summary>
		public List<float> position { get; set; }
		public List<float> forward { get; set; }
		public List<float> left { get; set; }
		public List<float> up { get; set; }
		/// <summary>
		/// A 3 element list of floats representing the disc's velocity.
		/// < X, Y, Z >
		/// </summary>
		public List<float> velocity { get; set; }
		public int bounce_count { get; set; }
	}

	/// <summary>
	/// Detailed info about the last throw
	/// </summary>
	public class g_LastThrow
	{
		public float arm_speed;
		public float rot_per_sec;
		public float pot_speed_from_rot;
		public float total_speed;
		public float speed_from_arm;
		public float speed_from_wrist;
		public float speed_from_movement;
		public float off_axis_spin_deg;
		public float wrist_align_to_throw_deg;
		public float throw_align_to_movement_deg;
		public float off_axis_penalty;
		public float wrist_throw_penalty;
		public float throw_move_penalty;
	}

	/// <summary>
	/// Object Containing basic player information and player stats 
	/// </summary>
	public class g_Player
	{
		[JsonIgnore]
		public g_Team team { get; set; }
		/// <summary>
		/// Right hand position and rotation
		/// </summary>
		public g_Transform rhand { get; set; }
		/// <summary>
		/// Index of the player in the match, so [0-6] for 3v3 & [0-7] for 4v4
		/// </summary>
		public int playerid { get; set; }
		/// <summary>
		/// Display Name
		/// </summary>
		public string name { get; set; }
		/// <summary>
		/// Application-scoped Oculus userid
		/// </summary>
		public ulong userid { get; set; }
		/// <summary>
		/// Object describing a player's aggregated statistics throughout the match.
		/// </summary>
		public g_Stats stats { get; set; }
		public int number { get; set; }
		public int level { get; set; }
		/// <summary>
		/// Boolean of player's stunned status.
		/// </summary>
		public bool stunned { get; set; }
		public int ping { get; set; }
		public float packetlossratio { get; set; }
		public string holding_left { get; set; }
		public string holding_right { get; set; }
		/// <summary>
		/// Boolean of the player's invulnerability after being stunned.
		/// </summary>
		public bool invulnerable { get; set; }
		public g_Transform head;
		/// <summary>
		/// Boolean determining whether or not this player has or had possession of the disc.
		/// possession will remain true until someone else grabs the disc or for 7 seconds (maybe?)
		/// </summary>
		public bool possession { get; set; }
		public g_Transform body;
		/// <summary>
		/// Left hand position and rotation
		/// </summary>
		public g_Transform lhand { get; set; }
		public bool blocking { get; set; }
		/// <summary>
		/// A 3 element list of floats representing the player's velocity.
		/// < X, Y, Z >
		/// </summary>
		public List<float> velocity { get; set; }
	}

	/// <summary>
	/// Object for position and rotation
	/// </summary>
	public class g_Transform
	{
		[JsonIgnore]
		public Vector3 Position {
			get {
				if (pos != null) return pos.ToVector3();
				else if (position != null) return position.ToVector3();
				else throw new NullReferenceException("Neither pos nor position are set");
			}
		}
		/// <summary>
		/// Don't get this value. Use Position property instead
		/// </summary>
		public List<float> pos { get; set; }
		/// <summary>
		/// Don't get this value. Use Position property instead
		/// </summary>
		public List<float> position { get; set; }
		public List<float> forward;
		public List<float> left;
		public List<float> up;
	}

	/// <summary>
	/// Object containing the player's stats in the match.
	/// </summary>
	public class g_Stats
	{
		public float possession_time { get; set; }
		public int points { get; set; }
		public int saves { get; set; }
		public int goals { get; set; }
		public int stuns { get; set; }
		public int passes { get; set; }
		public int catches { get; set; }
		public int steals { get; set; }
		public int blocks { get; set; }
		public int interceptions { get; set; }
		public int assists { get; set; }
		public int shots_taken { get; set; }

		public static g_Stats operator +(g_Stats a, g_Stats b)
		{
			g_Stats stats = new g_Stats
			{
				possession_time = a.possession_time + b.possession_time,
				points = a.points + b.points,
				passes = a.passes + b.passes,
				catches = a.catches + b.catches,
				steals = a.steals + b.steals,
				stuns = a.stuns + b.stuns,
				blocks = a.blocks + b.blocks,
				interceptions = a.interceptions + b.interceptions,
				assists = a.assists + b.assists,
				saves = a.saves + b.saves,
				goals = a.goals + b.goals,
				shots_taken = a.shots_taken + b.shots_taken
			};
			return stats;
		}

		public static g_Stats operator -(g_Stats a, g_Stats b)
		{
			g_Stats stats = new g_Stats
			{
				possession_time = a.possession_time - b.possession_time,
				points = a.points - b.points,
				passes = a.passes - b.passes,
				catches = a.catches - b.catches,
				steals = a.steals - b.steals,
				stuns = a.stuns - b.stuns,
				blocks = a.blocks - b.blocks,
				interceptions = a.interceptions - b.interceptions,
				assists = a.assists - b.assists,
				saves = a.saves - b.saves,
				goals = a.goals - b.goals,
				shots_taken = a.shots_taken - b.shots_taken
			};
			return stats;
		}
	}


	/// <summary>
	/// Object Containing basic team information and team stats
	/// </summary>
	public class g_Team
	{
		/// <summary>
		/// Enum declared for our own use.
		/// </summary>
		public enum TeamColor : byte { blue, orange, spectator }


		public List<g_Player> players { get; set; }
		/// <summary>
		/// Team name
		/// </summary>
		public string team { get; set; }
		public bool possession { get; set; }
		public g_Stats stats { get; set; }

		/// <summary>
		/// Not in the API, but add as soon as this frame is deserialized
		/// </summary>
		[JsonIgnore]
		public TeamColor color { get; set; }

		[JsonIgnore]
		public List<string> player_names {
			get
			{
				return players.Select(p => p.name).ToList();
			}
		}
	}

	/// <summary>
	/// Object Containing basic relavant information on who scored last.
	/// </summary>
	public class g_Score
	{
		public float disc_speed { get; set; }
		public string team { get; set; }
		public string goal_type { get; set; }
		public int point_amount { get; set; }
		public float distance_thrown { get; set; }
		/// <summary>
		/// Name of person who scored last.
		/// </summary>
		public string person_scored { get; set; }
		/// <summary>
		/// Name of person who assisted in the resulting goal.
		/// </summary>
		public string assist_scored { get; set; }

		public override bool Equals(object o)
		{
			g_Score s = (g_Score)o;
			return
				//Math.Abs(s.disc_speed - disc_speed) < .01f &&
				s.team == team &&
				s.goal_type == goal_type &&
				s.point_amount == point_amount &&
				Math.Abs(s.distance_thrown - distance_thrown) < .01f &&
				s.person_scored == person_scored &&
				s.assist_scored == assist_scored;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + disc_speed.GetHashCode();
			hash = hash * 29 + goal_type.GetHashCode();
			hash = hash * 31 + point_amount.GetHashCode();
			hash = hash * 37 + distance_thrown.GetHashCode();
			hash = hash * 41 + person_scored.GetHashCode();
			hash = hash * 43 + assist_scored.GetHashCode();
			return hash;
		}
	}

	public class g_Playspace
	{
		public float[] vr_left { get; set; }
		public float[] vr_position { get; set; }
		public float[] vr_forward { get; set; }
		public float[] vr_up { get; set; }

	}

	public class g_Pause
	{
		public string paused_state;
		public string unpaused_team;
		public string paused_requested_team;
		public float unpaused_timer;
		public float paused_timer;
	}
	
	
	/// <summary>
	/// Custom Vector3 class used to keep track of 3D coordinates.
	/// Works more like the Vector3 included with Unity now.
	/// </summary>
	public static class Vector3Extensions
	{
		public static Vector3 ToVector3(this List<float> input)
		{
			if (input.Count != 3)
			{
				throw new Exception("Can't convert List to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}

		public static Vector3 ToVector3(this float[] input)
		{
			if (input.Length != 3)
			{
				throw new Exception("Can't convert array to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}
		
		public static Vector3 ToVector3Backwards(this float[] input)
		{
			if (input.Length != 3)
			{
				throw new Exception("Can't convert array to Vector3");
			}

			return new Vector3(input[2], input[1], input[0]);
		}

		public static float DistanceTo(this Vector3 v1, Vector3 v2)
		{
			return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
		}

		public static Vector3 Normalized(this Vector3 v1)
		{
			return v1 / v1.Length();
		}
	}
}
