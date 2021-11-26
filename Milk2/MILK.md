﻿# .milk Replay Compression Format

### File structure
| Bytes | Data |
| ----- | ---- |
| (27-45) + (18-36) * n | File Header (n=total number of players in the file |
| ??*N | N Frames |

### File Header

| Bytes | Data |
| ----- | ---- |
| 1  | MILK version |
| 2  | Keyframe interval, ushort |
| 2-20 | `client_name` ASCII string |
| 1  | null byte |
| 16 | `sessionid` Session ids are made of 5 hexadecimal words, containing 8-4-4-4-12 digits each, so 32 hexadecimal digits makes 16 bytes |
| 4  | `sessionip` IPv4 addresses can be stored as 4 bytes |
| 1  | Total number of players in this file. Since players can join or leave, this can be more than 15.
| (2-20)*n | ASCII array of player names separated by null bytes, where n=number of players in the match at any time. |
| 8*n | Player `userid`s |
| 1*n | Player `number`s |
| 1*n | Player `level`s |
| 1   | `total_round_count` |
| .5  | `blue_round_score`   << 0 |
| .5  | `orange_round_score` << 4 |
| 1   | [Map Byte](#map-byte) (described below) |

#### Map Byte
Below is the bit flags structure for the Map Byte

| Bit number | Field name |
| ---------- | -----------|
| 0       | Public or Private, Public = 0, Private = 1 |
| 1-3     | Map name, see coding table below  |
| 4-7     | Currently Unused Bits |

#### Map name coding
| Index | Map Name |
| ----- | ---------|
| 0     | uncoded |
| 1     | mpl_lobby_b2  |
| 2     | mpl_arena_a |
| 3     | mpl_combat_fission |
| 4     | mpl_combat_combustion |
| 5     | mpl_combat_dyson |
| 6     | mpl_combat_gauss |
| 7     | mpl_tutorial_arena |

## Keyframes
In general, data for each frame in this format is only stored as a diff of the previous frame, however, a full reset happens every N frames. This allows decompression of partial replays and streaming, as well as prevents precision errors from building up over time.
The start of a keyframe is denoted by the `0xFEFE` header instead of the normal `0xFEFC` header. 


## Frames
Each frame represents the data retrieved from a single call to the game's API. Any numerical value in a frame is only stored as the difference from the previous frame, except for keyframes.

### Frame Data
| Bytes | Data |
| ----- | ---- |
| 2 | `0xFEFC` static header |
| 2 | `game_clock` half-precision float |
| 1 | [Inclusion bitmask](#inclusion-bitmask)
| 1 | `game_status` See [Game Status](#game-status-coding) table |
| 1 | `blue_points` |
| 1 | `orange_points` |
| 5 | [Pause and Restarts](#pause-and-restarts). |
| 8 | [Inputs](#inputs). |
| 7 | [Last Score](#last-score) No continuous data |
| 26| [Last Throw](#last-throw) No continuous data |
| 13| [VR Player](#vr-player)  |
| 19| [Disc](#disc)  |
| 1 | [Team data bitmask](#team-data-bitmask) |
| ??*3 | [Team data](#team-data) |

### Inclusion bitmask
| Bit | Value |
| --- | ------|
| 0   | `game_status` |
| 1   | blue/orange points |
| 2   | Pause and restarts |
| 3   | Inputs |
| 4   | Last Score |
| 5   | Last Throw |
| 6   | VR Player |
| 7   | Disc |

### Game Status coding
| Index | Map Name |
| ----- | ---------|
| 0     | uncoded |
| 1     | --empty string-- |
| 2     | pre_match  |
| 3     | round_start  |
| 4     | playing |
| 5     | score |
| 6     | round_over  |
| 7     | post_match  |
| 8     | pre_sudden_death |
| 9     | sudden_death |
| 10    | post_sudden_death |

### Pause and Restarts
| Bit  | Value |
| ----- | ---------|
| 0     | `blue_team_restart_request` |
| 1     | `orange_team_restart_request` |
| 2-3   | `paused_requested_team` See [Team indices](#team-indices) table |
| 4-5   | `unpaused_team` See [Team indices](#team-indices) table |
| 6-7   | `paused_state` See [Paused state](#paused-state) table |
| 2-byte | `paused_timer` |
| 2-byte | `unpaused_timer` |


### Paused state
This is two bits

| Index | Value |
| ----- | ---------|
| 0     | unpaused |
| 1     | paused |
| 2     | unpausing  |
| 3     | pausing???  |

### Team indices
This is two bits 

| Index | Team |
| ----- | ---------|
| 0     | Blue team |
| 1     | Orange Team |
| 2     | Spectator  |
| 3     | None  |

### Goal Type
This is three bits

| Index | Team |
| ----- | ---------|
| 0 | unknown |
| 1 | BOUNCE_SHOT |
| 2 | INSIDE_SHOT |
| 3 | LONG_BOUNCE_SHOT |
| 4 | LONG_SHOT |
| 5 | SELF_GOAL |
| 6 | SLAM_DUNK |
| 7 | BUMPER_SHOT |
| 8 | HEADBUTT |

### Last Score
| Bit  | Value |
| ----- | ---------|
| 0-1   | `team` See [Team indices](#team-indices) table |
| 2     | `point_amount` 0 = 2-pointer, 1 = 3-pointer |
| 3-7   | `goal_type` See [Goal Type](#goal-type) table |
| 1-byte | `person_scored` index of person who scored |
| 1-byte | `assist_scored` 0 = 2-pointer, 1 = 3-pointer |
| 2-byte | `disc_speed` half |
| 2-byte | `distance_thrown` half |

### Last Throw
2-byte half-precision floats for each of:
 - arm_speed
 - total_speed
 - off_axis_spin_deg
 - wrist_throw_penalty
 - rot_per_sec
 - pot_speed_from_rot
 - speed_from_arm
 - speed_from_movement
 - speed_from_wrist
 - wrist_align_to_throw_deg
 - throw_align_to_movement_deg
 - off_axis_penalty
 - throw_move_penalty

### Inputs
2-byte half-precision floats for each of:
- left_shoulder_pressed
- right_shoulder_pressed
- left_shoulder_pressed2
- right_shoulder_pressed2


### Disc
It would possible to eliminate the velocity component and only make use of frame diffs and timing to calculate velocity, but velocity 

| Bytes | Value |
| ----- | ---------|
| 2 | x position |
| 2 | y position |
| 2 | z position |
| 7 | [Orientation](#orientation)
| 2 | x velocity |
| 2 | y velocity |
| 2 | z velocity |

### VR Player
VR player is a simple [pose](#pose) value - 13 bytes.

### Pose
This is used for any position and rotation value

| Bytes | Value |
| ----- | ---------|
| 2 | x position |
| 2 | y position |
| 2 | z position |
| 7 | [Orientation](#orientation)

### Orientation
Orientations are usually presented in the API as 3 sets of XYZ direction vectors, but this can be encoded in 7 bytes with minimal precision loss by using [smallest three](https://gafferongames.com/post/snapshot_compression/) compression.
The orientation is converted to a Quaternion, then the component with the smallest absolute value is replaced by its index (in a byte). The other three values are recorded using 2 bytes each.


### Team data bitmask
| Bit   | Value |
| ----- | ---------|
| 0     | Blue team possession |
| 1     | Orange team possession |
| 2     | Blue team stats included |
| 3     | Orange team stats included |
| 4     | Spectator team stats included |
| 5     | unused |
| 6     | unused |
| 7     | unused |


### Team data
| Bytes | Value |
| ----- | ---------|
| 14 | Team [Stats](#stats) |
| 1  | Number of players on team (K) |
| (67 or 81) * K | [Player data](#player-data) |

### Stats
Example JSON:
```json
{
    "stats": {
        "points": 18,
        "possession_time": 561.90625,
        "interceptions": 0,
        "blocks": 0,
        "steals": 5,
        "catches": 0,
        "passes": 0,
        "saves": 11,
        "goals": 0,
        "stuns": 169,
        "assists": 8,
        "shots_taken": 29
    }
}
```
#### 1 byte each (alphabetical):
 - Assists
 - Blocks
 - Catches
 - Goals
 - Interceptions
 - Passes
 - Points
 - Saves
 - Shots Taken
 - Steals
#### 2 bytes each:
 - Possession Time (half-precision float)
 - Stuns


### Player data
67 bytes, or 81 bytes when the player stats are included

| Bytes | Value |
| ----- | ---------|
| 1  | Player index (for this file) |
| 1  | `playerid` |
| 1  | [Player state bitmask](#player-state-bitmask) |
| 2  | Ping |
| 2  | Packet loss ratio (half) |
| 1  | [Holding](#holding-indices) Left. If holding a player, the userid is converted to the player's file id |
| 1  | [Holding](#holding-indices) Right If holding a player, the userid is converted to the player's file id |
| 13 | Head [Pose](#pose)
| 13 | Body [Pose](#pose)
| 13 | Left Hand [Pose](#pose)
| 13 | Right Hand [Pose](#pose)
| 6  | Velocity
| 14*| Player [Stats](#stats) (if included) 


### Player state bitmask
Possession, Blocking, Stunned, Invulnerable

| Bit number | Field name |
| ---------- | -----------|
| 0     | `possession` |
| 1     | `blocking` |
| 2     | `stunned` |
| 3     | `invulnerable` |
| 4     | Contains changed stats |
| 5-7   | unused |


### Holding indices

| Case       | Value      |
| ---------- | -----------|
| `none`     | 255        |
| `geo`      | 254        |
| `disc`     | 253        |
| `playerid` | The file id of the player |