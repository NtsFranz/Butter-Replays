THIS FORMAT SPEC IS NOT COMPLETE AND HAS SOME INCORRECT INFORMATION
# .milk Replay Compression Format

### File structure
| Bytes | Data |
| ----- | ---- |
| (27-45) + (18-36) * n | File Header (n=total number of players in the file |
| ??*N | N Frames |

### File Header

| Bytes | Data |
| ----- | ---- |
| 1 | MILK version |
| 2-20 | `client_name` ASCII string |
| 1 | null byte |
| 16 | `sessionid` Session ids are made of 5 hexadecimal words, containing 8-4-4-4-12 digits each, so 32 hexadecimal digits makes 16 bytes |
| 4 | `sessionip` IPv4 addresses can be stored as 4 bytes |
| n | Total number of players in this file. Since players can join or leave, this can be more than 15.
| (2-20)*n | ASCII array of player names separated by null bytes, where n=number of players in the match at any time. |
| 8*n | Player `userid`s |
| 4*n | Player `number`s |
| 4*n | Player `level`s |
| 1 | `total_round_count` |
| .5 | `blue_round_score` |
| .5 | `orange_round_score` |
| 1 | Map Byte (described below) |

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
In general, data for each frame in this format is only stored as a diff of the previous frame, however, a full reset happens every N frames. This allows decompression of partial replays and streaming.
The start of a keyframe is denoted by the `0xFEFE` header. 


## Frames
Each frame represents the data retrieved from a single call to the game's API. Any numerical value in a frame is only stored as the difference from the previous frame, except for keyframes.

### Frame Data
| Bytes | Data |
| ----- | ---- |
| 2 | `0xFEFC` static header |
| 2 | `game_clock` half-precision float |
| 1
| 1 | `game_status` See Game Status coding table |
| 1 | `blue_points` |
| 1 | `orange_points` |
| 5 | Pause and restarts (See Pause and Restarts table). Contains continuous data |
| 8 | Inputs. Contains continuous data |
| 7 | Last Score |
| 26| Last Throw |
| ??| Team Stats |
| ??| Player Stats |

#### Game Status coding
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

#### Pause and restarts
| Bit  | Value |
| ----- | ---------|
| 0     | `blue_team_restart_request` |
| 1     | `orange_team_restart_request` |
| 2-3   | `paused_requested_team` See Team indices table |
| 4-5   | `unpaused_team` See Team indices table |
| 6-7   | `paused_state` See Paused state table |
| 2-byte | `paused_timer` |
| 2-byte | `unpaused_timer` |


#### Paused state
This is two bits

| Index | Value |
| ----- | ---------|
| 0     | unpaused |
| 1     | paused |
| 2     | unpausing  |
| 3     | pausing???  |

#### Team indices
This is two bits 

| Index | Team |
| ----- | ---------|
| 0     | Blue team |
| 1     | Orange Team |
| 2     | Spectator  |
| 3     | None  |

#### Goal Type indices
This is three bits

| Index | Team |
| ----- | ---------|
| 0     | INSIDE SHOT |
| 1     | LONG SHOT |
| 2     | BOUNCE SHOT  |
| 3     | ...???  |

#### Last Score
| Bit  | Value |
| ----- | ---------|
| 0-1   | `team` See Team indices table |
| 2     | `point_amount` 0 = 2-pointer, 1 = 3-pointer |
| 3-7   | `goal_type` See Goal Type table |
| 1-byte | `person_scored` index of person who scored |
| 1-byte | `assist_scored` 0 = 2-pointer, 1 = 3-pointer |
| 2-byte | `disc_speed` half |
| 2-byte | `distance_thrown` half |

#### Last Throw
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

#### Inputs
2-byte half-precision floats for each of:
- left_shoulder_pressed
- right_shoulder_pressed
- left_shoulder_pressed2
- right_shoulder_pressed2


### Team Stats
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
#### 1 byte each:
 - Points
 - Interceptions
 - Blocks
 - Steals
 - Catches
 - Passes
 - Saves
 - Goals
 - Assists
 - Shots Taken
#### 2 bytes each:
 - Stuns
 - Possession Time (half-precision float)

## Frames
Frames represent a single capture of the API.

### Frame Format

| `Header` | `Disc Position` |`Game State`| `Position Data` | `Possession` | `Blocking` | `Stunned` |
|:--------:|:---------------:|:----------:|:---------------:|:------------:|:----------:|:---------:|
| `0xFDFE` |     6-bytes     |   1 byte   |     n-bytes     |    1-byte    |   1-byte   |  1-byte   |

#### Game State
These values all have a length of 1-byte and use bit flags to determine state.
Below is the structure of these bytes
| Bit number | Field name |
| ---------- | -----------|
| 7       | `Post Sudden Death` |
| 6       | `Sudden Death`  |
| 5       | `Post Match`    |
| 4       | `Round Over`    |
| 3       | `Score`         |
| 2       | `Playing`       |
| 1       | `Round Start`   |
| 0       | `Pre-Match`     |

#### Possession, Blocking, Stunned
These values all have a length of 1-byte and use bit flags to determine state.
Below is the structure of these bytes
| Bit number | Field name |
| ---------- | -----------|
| 7-4        | `Orange Players State` |
| 3-0        | `Blue Players State`   |

Example of where Blue team's Player 3 has disc
|  `Flag_Value`  |   `0`   |  `0`  |  `1`  |  `0`  |  `0`  |  `0`  |  `0`  |  `0`  |
| -------------- | ----- | --- | --- | --- | --- | --- | --- | --- |
| `Player Index` |   `0`   |  `1`  |  `2`  |  `3`  |  `4`  |  `5`  |  `6`  |  `7`  |

The result of this would be 0x10

A bit is flagged as 1 if the field is true (ie. Player does have possession, player is stunned,etc)

## License
[MIT](https://choosealicense.com/licenses/mit/)