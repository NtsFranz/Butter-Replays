﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Milk2
{
    /// <summary>
    /// 🥛🥛🥛🥛🥛
    /// </summary>
    public class MilkV2
    {
        public const byte FORMAT_VERSION = 3;
        public const ushort KEYFRAME_INTERVAL = 30;

        private MilkFileHeader fileHeader;
        private List<MilkFrame> frames;

        public MilkV2() { }
        
        public MilkV2(g_Instance frame)
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

        public class MilkFileHeader
        {
            private g_Instance firstFrame;
            
            private List<string> players;
            private List<int> numbers;
            private List<int> levels;
            private List<long> userids;

            public MilkFileHeader(g_Instance firstFrame)
            {
                this.firstFrame = firstFrame;

                players = new List<string>();
                numbers = new List<int>();
                levels = new List<int>();
                userids = new List<long>();

                ConsiderNewFrame(firstFrame);
            }

            public void ConsiderNewFrame(g_Instance frame)
            {
                foreach (g_Team team in frame.teams)
                {
                    if (team.players==null) continue;
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

            public byte GetPlayerIndex(string playerName)
            {
                int index = players.IndexOf(playerName);
                return (byte)(index + 1);
            }
            
            public byte GetPlayerIndex(long userId)
            {
                int index = userids.IndexOf(userId);
                return (byte)(index + 1);
            }

            public byte[] GetBytes()
            {
                List<byte> bytes = new List<byte>();

                bytes.Add(FORMAT_VERSION); // version number
                bytes.AddRange(BitConverter.GetBytes(KEYFRAME_INTERVAL));
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
                    // only add the null byte between player names, not after the last
                    if (i < players.Count - 1)
                    {
                        bytes.Add(0);
                    }
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


        public class MilkFrame
        {
            private g_Instance frame;
            public MilkFrame lastMilkFrame;
            private MilkFileHeader milkHeader;

            /// <summary>
            /// Creates a new Milk frame from a decompressed frame class
            /// </summary>
            /// <param name="frame">The original data</param>
            /// <param name="lastMilkFrame">Previous milk frame to do diffs with. Pass in null to avoid diffs</param>
            /// <param name="milkHeader">The milk header, which contains player dictionaries</param>
            public MilkFrame(g_Instance frame, MilkFrame lastMilkFrame, MilkFileHeader milkHeader)
            {
                this.frame = frame;
                this.lastMilkFrame = lastMilkFrame;
                this.milkHeader = milkHeader;
            }

            public byte[] GetBytes()
            {
                List<byte> bytes = new List<byte> { 0xFE, 0xFC };
                bytes.AddRange(BitConverter.GetBytes((Half)frame.game_clock));
                byte inclusionBitmask = 0;
                inclusionBitmask += (byte)((frame.game_status != lastMilkFrame?.frame.game_status ? 1 : 0) << 0); // game_status
                inclusionBitmask += (byte)((frame.blue_points != lastMilkFrame?.frame.blue_points || frame.orange_points != lastMilkFrame.frame.orange_points ? 1 : 0) << 1); // blue or orange points
                inclusionBitmask += (byte)((PauseAndRestartsBytes().SameAs(lastMilkFrame?.PauseAndRestartsBytes()) ? 0 : 1) << 2); // pause and restarts
                inclusionBitmask += (byte)((InputBytes().SameAs(lastMilkFrame?.InputBytes()) ? 0 : 1) << 3); // inputs
                inclusionBitmask += (byte)((LastScoreBytes().SameAs(lastMilkFrame?.LastScoreBytes()) ? 0 : 1) << 4); // Last Score
                inclusionBitmask += (byte)((LastThrowBytes().SameAs(lastMilkFrame?.LastThrowBytes()) ? 0 : 1) << 5); // last throw
                inclusionBitmask += (byte)((VRPlayerBytes().SameAs(lastMilkFrame?.VRPlayerBytes()) ? 0 : 1) << 6); // vr player
                inclusionBitmask += (byte)((DiscBytes().SameAs(lastMilkFrame?.DiscBytes()) ? 0 : 1) << 7); // disc
                bytes.Add(inclusionBitmask);
                
                if ((inclusionBitmask & (1 << 0)) == 1)
                {
                    bytes.Add(GameStatusToByte(frame.game_status));
                }

                if ((inclusionBitmask & (1 << 1)) == 1)
                {
                    bytes.Add((byte)frame.blue_points);
                    bytes.Add((byte)frame.orange_points);
                }

                // Pause and restarts
                if ((inclusionBitmask & (1 << 2)) == 1)
                {
                    bytes.AddRange(PauseAndRestartsBytes());
                }

                // Inputs
                if ((inclusionBitmask & (1 << 3)) == 1)
                {
                    bytes.AddRange(InputBytes());
                }

                // Last Score
                if ((inclusionBitmask & (1 << 4)) == 1)
                {
                    bytes.AddRange(LastScoreBytes());
                }

                // Last Throw
                if ((inclusionBitmask & (1 << 5)) == 1)
                {
                    bytes.AddRange(LastThrowBytes());
                }


                // VR Player
                if ((inclusionBitmask & (1 << 6)) == 1)
                {
                    bytes.AddRange(VRPlayerBytes());
                }

                // Disc
                if ((inclusionBitmask & (1 << 7)) == 1)
                {
                    bytes.AddRange(DiscBytes());
                }

                byte teamDataBitmask = 0;
                teamDataBitmask |= (byte)(frame.teams[0].possession ? 1 : 0);
                teamDataBitmask |= (byte)(frame.teams[1].possession ? 1 : 0);
                // TODO check team stats diff
                // Team stats included
                bool[] teamStatsIncluded = new bool[3];
                teamStatsIncluded[0] = frame.teams[0]?.stats != null && !TeamStatsBytes(frame.teams[0].stats).SameAs(TeamStatsBytes(lastMilkFrame?.frame.teams[0].stats));    // TODO do this properly
                teamStatsIncluded[1] = frame.teams[1]?.stats != null && !TeamStatsBytes(frame.teams[1].stats).SameAs(TeamStatsBytes(lastMilkFrame?.frame.teams[1].stats));
                teamStatsIncluded[2] = frame.teams[2]?.stats != null && !TeamStatsBytes(frame.teams[2].stats).SameAs(TeamStatsBytes(lastMilkFrame?.frame.teams[2].stats));
                teamDataBitmask |= (byte)((byte)(teamStatsIncluded[0] ? 1 : 0) << 2);
                teamDataBitmask |= (byte)((byte)(teamStatsIncluded[1] ? 1 : 0) << 3);
                teamDataBitmask |= (byte)((byte)(teamStatsIncluded[2] ? 1 : 0) << 4);
                bytes.Add(teamDataBitmask);


                // add team data
                for (int i = 0; i < 3; i++)
                {
                    if (teamStatsIncluded[i])
                    {
                        bytes.AddRange(TeamStatsBytes(frame.teams[i].stats));
                    }
                    
                    bytes.Add((byte)(frame.teams[i]?.players?.Count ?? 0));
                    if ((frame.teams[i]?.players?.Count ?? 0) > 0) {
                        foreach (g_Player player in frame.teams[i].players)
                        {
                            bytes.Add(milkHeader.GetPlayerIndex(player.name));
                            bytes.Add((byte)player.playerid);
                            List<bool> playerStateBitmask = new List<bool>()
                            {
                                player.possession,
                                player.blocking,
                                player.stunned,
                                player.invulnerable,
                                !PlayerStatsBytes(player.stats).SameAs(PlayerStatsBytes(lastMilkFrame?.frame.GetPlayer(player.name).stats))
                            };
                            bytes.Add(playerStateBitmask.GetBitmasks()[0]);
                            bytes.AddRange(BitConverter.GetBytes((ushort)player.ping));
                            bytes.AddRange(BitConverter.GetBytes((Half)player.packetlossratio));
                            bytes.Add(HoldingToByte(player.holding_left));
                            bytes.Add(HoldingToByte(player.holding_right));

                            bytes.AddRange(PoseToBytes(player.head));
                            bytes.AddRange(PoseToBytes(player.body));
                            bytes.AddRange(PoseToBytes(player.lhand));
                            bytes.AddRange(PoseToBytes(player.rhand));
                            bytes.AddRange(player.velocity.GetHalfBytes());


                            if (playerStateBitmask[4])
                            {
                                bytes.AddRange(PlayerStatsBytes(player.stats));
                            }

                        }
                    }
                }

                return bytes.ToArray();
            }

            private static byte[] TeamStatsBytes(g_TeamStats stats)
            {
                if (stats == null) return null;
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((byte)stats.assists));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.blocks));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.catches));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.goals));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.interceptions));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.passes));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.points));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.saves));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.steals));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.shots_taken));

                bytes.AddRange(BitConverter.GetBytes((Half)stats.possession_time));
                bytes.AddRange(BitConverter.GetBytes((ushort)Math.Clamp(stats.stuns, 0, ushort.MaxValue)));
                return bytes.ToArray();
            }
            
            private static byte[] PlayerStatsBytes(g_PlayerStats stats)
            {
                if (stats == null) return null;
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((byte)stats.assists));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.blocks));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.catches));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.goals));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.interceptions));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.passes));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.points));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.saves));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.steals));
                bytes.AddRange(BitConverter.GetBytes((byte)stats.shots_taken));

                bytes.AddRange(BitConverter.GetBytes((Half)stats.possession_time));
                bytes.AddRange(BitConverter.GetBytes((ushort)Math.Clamp(stats.stuns, 0, ushort.MaxValue)));
                return bytes.ToArray();
            }

            private byte[] DiscBytes()
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(PoseToBytes(
                    frame.disc.position.ToVector3(),
                    frame.disc.forward.ToVector3(),
                    frame.disc.up.ToVector3())
                );
                bytes.AddRange(frame.disc.velocity.GetHalfBytes());
                return bytes.ToArray();
            }

            private byte[] VRPlayerBytes()
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(PoseToBytes(
                    frame.player.vr_position.ToVector3(),
                    frame.player.vr_forward.ToVector3(),
                    frame.player.vr_up.ToVector3())
                );
                return bytes.ToArray();
            }

            private byte[] LastThrowBytes()
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
                return bytes.ToArray();
            }

            private byte[] LastScoreBytes()
            {
                List<byte> bytes = new List<byte>();
                byte b = TeamToTeamIndex(frame.last_score.team);
                b |= (byte)((frame.last_score.point_amount == 2 ? 0 : 1) << 2);
                if (!Enum.TryParse(frame.last_score.goal_type, out GoalType type))
                {
                    type = GoalType.unknown;
                }

                b |= (byte)((byte)type << 3);
                bytes.Add(b);

                bytes.Add(milkHeader.GetPlayerIndex(frame.last_score.person_scored));
                bytes.Add(milkHeader.GetPlayerIndex(frame.last_score.assist_scored));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.last_score.disc_speed));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.last_score.distance_thrown));
                return bytes.ToArray();
            }

            private byte[] InputBytes()
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((Half)frame.left_shoulder_pressed));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.right_shoulder_pressed));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.left_shoulder_pressed2));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.right_shoulder_pressed2));
                return bytes.ToArray();
            }

            private byte[] PauseAndRestartsBytes()
            {
                List<byte> bytes = new List<byte>();
                byte pauses = 0;
                pauses |= (byte)((frame.blue_team_restart_request ? 1 : 0) << 0);
                pauses |= (byte)((frame.orange_team_restart_request ? 1 : 0) << 1);
                pauses |= (byte)(TeamToTeamIndex(frame.pause.paused_requested_team) << 2);
                pauses |= (byte)(TeamToTeamIndex(frame.pause.unpaused_team) << 4);
                pauses |= (byte)(PausedStateToByte(frame.pause.paused_state) << 6);
                bytes.Add(pauses);

                bytes.AddRange(BitConverter.GetBytes((Half)frame.pause.paused_timer));
                bytes.AddRange(BitConverter.GetBytes((Half)frame.pause.unpaused_timer));
                return bytes.ToArray();
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

            public byte HoldingToByte(string holding)
            {
                return holding switch
                {
                    "none" => 255,
                    "geo" => 254,
                    "disc" => 253,
                    _ => milkHeader.GetPlayerIndex(long.Parse(holding))
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

            public static byte[] PoseToBytes(g_Transform transform)
            {
                return PoseToBytes(
                    transform.Position, 
                    transform.forward.ToVector3(), 
                    transform.up.ToVector3()
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
                float[] components = {
                    q.X, q.Y, q.Z, q.W
                };
                
                // find component with largest absolute value
                float max = 0;
                int maxIndex = 0;   
                float[] absComponents = components.Select(MathF.Abs).ToArray();
                for (int i = 0; i < 4; i++)
                {
                    if (absComponents[i] > max)
                    {
                        max = absComponents[i];
                        maxIndex = i;
                    }
                }

                // store the index as a byte
                List<byte> bytes = new List<byte>
                {
                    (byte)maxIndex
                };

                // store the other three components as 2-byte Halfs
                for (int i = 0; i < 4; i++)
                {
                    if (i != maxIndex)
                    {
                        bytes.AddRange(BitConverter.GetBytes((Half)components[i]));
                    }
                }

                return bytes.ToArray();
            }
        }
        

        public void AddFrame(g_Instance frame)
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


            MilkFrame milkFrame = new MilkFrame(frame, frames.Count > 0 ? frames.Last() : null, fileHeader);
            frames.Add(milkFrame);
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(fileHeader.GetBytes());
            foreach (MilkFrame frame in frames)
            {
                bytes.AddRange(frame.GetBytes());
            }

            return bytes.ToArray();
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

        public static byte[] GetBytes(this IEnumerable<long> values)
        {
            List<byte> bytes = new List<byte>();
            foreach (long val in values)
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
    }
}