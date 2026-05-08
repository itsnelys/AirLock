# Space Engineers Airlock System - Script Documentation

This *Space Engineers* programmable block script implements an **intelligent multi-airlock controller**. It manages airlock cycling, door interlocks, oxygen leak detection, room containment, secure lockdown, local LCDs, global overview displays, lights, and sound alarms.

---

## Table of Contents

* [Initialization and Configuration](#initialization-and-configuration)
* [Airlock Cycling Logic](#airlock-cycling-logic)
* [Emergency and Secure Protocols](#emergency-and-secure-protocols)
* [Room Containment System](#room-containment-system)
* [LCD Display Logic](#lcd-display-logic)
* [Terminal Commands](#terminal-commands)
* [Troubleshooting](#troubleshooting)

---

## Initialization and Configuration

The script scans named block groups and block tags on the same construct as the programmable block. Each airlock can be separated by a `[SAS:...]` tag, allowing multiple airlocks to run from the same script while keeping their own doors, vents, sounds, lights, and state.

### **Script Constants**
| Constant | Default Value | Description |
| --- | --- | --- |
| `VENTS_GROUP` | `AIRLOCK_VENTS` | Air vents used by the active airlock. |
| `DOORS_IN_GROUP` | `AIRLOCK_DOORS_IN` | Doors leading to the pressurized interior side. |
| `DOORS_OUT_GROUP` | `AIRLOCK_DOORS_OUT` | Doors leading to the exterior or hangar side. |
| `LCD_GROUP` | `AIRLOCK_LCD` | Local LCDs for each airlock. |
| `LEDS_GROUP` | `AIRLOCK_LEDS` | Local indicator lights for each airlock. |
| `SOUND_NORMAL_GROUP` | `AIRLOCK_SOUND` | Local cycle sound blocks and fallback alarm sound blocks. |
| `PROBE_VENTS_GROUP` | `AIRLOCK_SAS_PROBE` | Probe vents used for reliable oxygen readings. |
| `EMERGENCY_LEDS_GROUP` | `AIRLOCK_EMERGENCY_LEDS` | Global emergency / secure mode lights. |
| `SOUND_ALARM_GROUP` | `AIRLOCK_ALARM` | Global alarm / secure mode sound blocks. |
| `AUTOCLOSE_DOORS_GROUP` | `AUTOCLOSE_DOORS` | Extra doors watched by the auto-close system. |
| `OVERVIEW_LCD_GROUP` | `VENTS_OVERVIEW_LCD` | Global overview LCDs for all airlocks and rooms. |
| `CONTAIN_GROUP_PREFIX` | `CONTAIN_` | Prefix for room containment groups. |
| `ROOM_LCD_GROUP` | `ROOM_STATUS_LCD` | LCD group for room status displays. |
| `SAS_TAG_PREFIX` | `[SAS:` | Tag used to assign blocks to a specific airlock. |
| `ROOM_TAG_PREFIX` | `[ROOM:` | Tag used to assign vents, lights, and LCDs to a room. |
| `PROBE_TAG` | `[PROBE]` | Tag used to identify probe vents. |

### **Timing and Threshold Constants**
| Constant | Default Value | Description |
| --- | --- | --- |
| `O2_PRESSURIZED` | `0.95f` | Oxygen level considered pressurized. |
| `O2_DEPRESSURIZED` | `0.05f` | Oxygen level considered depressurized. |
| `LEAK_O2_THRESHOLD` | `0.20f` | Airlock leak threshold. |
| `ROOM_LEAK_THRESHOLD` | `0.20f` | Room leak threshold. |
| `ROOM_RELEASE_O2` | `0.25f` | O2 level required before an auto-contained room can be released. |
| `DOOR_MIN_HEALTH` | `0.60f` | Minimum door integrity before triggering an emergency. |
| `CYCLE_SECONDS` | `10.0` | Duration of a pressurization or depressurization cycle. |
| `AUTOCLOSE_SECONDS` | `8.0` | Time before monitored open doors are closed automatically. |
| `DOOR_HOLD_SECONDS` | `30.0` | Time the destination-side door remains open after a cycle. |
| `EMERG_P1_SECONDS` | `30.0` | Emergency phase 1 duration. |
| `EMERG_P2_SECONDS` | `5.0` | Emergency phase 2 duration. |
| `ROOM_TEST_DURATION` | `10.0` | Room pressurization test duration. |
| `ROOM_RETEST_PERIOD` | `60.0` | Delay before an auto-contained room can be retested. |
| `DOOR_TEMP_UNLOCK_SECONDS` | `8.0` | Temporary containment door unlock duration. |
| `MANUAL_CONTAIN_PROTO_SECONDS` | `10.0` | Delay before manual containment changes are applied. |

### **Sound Constants**
| Constant | Default Value | Description |
| --- | --- | --- |
| `SOUND_PRESSURE_CYCLE` | `Alert 3` | Normal pressurization / depressurization cycle sound. |
| `SOUND_GLOBAL_ALARM` | `Alert 2` | Global alarm sound outside secure mode. |
| `SOUND_LOCAL_ALARM` | `Alert 1` | Local airlock emergency sound. |
| `SOUND_SECURE_ALARM` | `Drone Large` | Secure mode sound. |
| `DEFAULT_ROOM_ALERT_SOUND` | `Alert 1` | Room containment alert sound. |
| `NORMAL_LOOP_PERIOD` | `1.0f` | Default normal cycle sound loop duration. |
| `ALARM_LOOP_PERIOD` | `1.0f` | Default alarm loop duration. |
| `SECURE_LOOP_PERIOD` | `2.0f` | Secure mode sound loop duration. |

### **Required Block Groups**
| Group Name | Blocks Inside | Description |
| --- | --- | --- |
| `AIRLOCK_VENTS` | Air vents | Vents used to pressurize or depressurize the airlock. |
| `AIRLOCK_DOORS_IN` | Doors | Interior-side airlock doors. |
| `AIRLOCK_DOORS_OUT` | Doors | Exterior-side or hangar-side airlock doors. |
| `AIRLOCK_LCD` | LCD panels or display-capable blocks | Local airlock status screens. |
| `AIRLOCK_LEDS` | Interior lights | Local status lights. |
| `AIRLOCK_SOUND` | Sound blocks | Normal cycle sound; also used as fallback if `AIRLOCK_ALARM` is missing. |
| `AIRLOCK_SAS_PROBE` | Air vents | Optional dedicated O2 probes. These are never used for cycling. |
| `AIRLOCK_ALARM` | Sound blocks | Global and secure alarm sound blocks. |
| `AIRLOCK_EMERGENCY_LEDS` | Interior lights | Global emergency and secure mode lights. |
| `AUTOCLOSE_DOORS` | Doors | Optional doors closed automatically after `AUTOCLOSE_SECONDS`. |
| `VENTS_OVERVIEW_LCD` | LCD panels or display-capable blocks | Optional global overview screen. |
| `ROOM_STATUS_LCD` | LCD panels or display-capable blocks | Optional room status displays, matched by `[ROOM:...]`. |
| `CONTAIN_<ROOM>` | Doors, vents, lights, sound blocks | Optional containment group for a room. |

### **Tagging Rules**
| Tag / Naming Rule | Used On | Description |
| --- | --- | --- |
| `[SAS:Name]` | Airlock doors, vents, LCDs, lights, sound blocks | Assigns the block to a specific airlock. Example: `[SAS:SAS 01]`. |
| `[ROOM:Name]` | Room vents, lights, room LCDs | Assigns the block to a monitored room. Example: `[ROOM:Medbay]`. |
| `[PROBE]` | Air vents | Marks a vent as a read-only oxygen probe. |
| `CONTAIN_<ROOM>` | Block group name | Defines which doors, vents, lights, and sound blocks belong to a containment zone. |

* **Multiple airlocks:** Put all airlock blocks in the same group names, then separate each airlock with `[SAS:...]` tags.
* **Default airlock:** If the programmable block itself has a `[SAS:...]` tag, that airlock becomes the default target for commands without an explicit SAS name.
* **Same construct only:** With `LIMIT_TO_CONSTRUCT = true`, the script ignores blocks on other connected constructs.
* **No manual sound setup:** Sound blocks do not need to be preconfigured; the script selects the sound and loop period automatically.

---

## Airlock Cycling Logic

The script automates movement between pressurized and depressurized environments while preventing both sides from opening at the same time.

### **Operating Logic**
1.  **Command:** A `pressu` or `depressu` command starts the requested cycle.
2.  **Pre-lock:** Both door groups are enabled, ordered closed, and checked.
3.  **Interlock:** Once all doors are closed, both sides are disabled to prevent manual opening.
4.  **Vent control:** Vents are set to pressurize or depressurize for `CYCLE_SECONDS`.
5.  **Access:** The destination-side door opens for `DOOR_HOLD_SECONDS`.
6.  **Recovery:** The door is closed, normal interlocks are restored, and the airlock returns to idle.

### **Door Direction**
| Command | Vent Mode | Door Opened After Cycle |
| --- | --- | --- |
| `pressu` | Pressurize | `AIRLOCK_DOORS_IN` |
| `depressu` | Depressurize | `AIRLOCK_DOORS_OUT` |

### **Interlock Behavior**
* Interior doors stay disabled while the airlock is depressurized.
* Exterior doors stay disabled while the airlock is pressurized.
* During active cycling, both door groups are closed and disabled.
* If secure mode or a remote emergency is active, normal cycling is blocked.

---

## Emergency and Secure Protocols

The script can enter local emergency, global emergency, or secure mode depending on the situation.

### **Emergency Triggers**
| Trigger | Result |
| --- | --- |
| Manual `urgence` / `emergency` command | Starts a local emergency on the target airlock. |
| O2 leak below `LEAK_O2_THRESHOLD` | Starts a local O2 leak emergency. |
| Door integrity below `DOOR_MIN_HEALTH` | Starts a local door damage emergency. |
| Another airlock in emergency | Locks non-source airlocks through remote emergency handling. |

### **Emergency Phases**
1.  **Phase 1 (`EMERG_P1_SECONDS`):** Doors are closed, non-probe vents are shut down, local alarm and red lights are enabled.
2.  **Phase 2 (`EMERG_P2_SECONDS`):** The system keeps doors secured and prepares recovery.
3.  **Restore:** If `RESTORE_AFTER_EMERG` is enabled, vents and interlocks are restored when safe.

### **Secure Mode**
Secure mode is a global lockdown intended for combat, intrusion, or full-grid safety situations.

* Locks all discovered airlocks.
* Closes and hard-locks airlock doors.
* Turns off non-probe vents globally.
* Uses global emergency LEDs if `SECURE_USES_GLOBAL_LEDS = true`.
* Uses global alarm sound if `SECURE_USES_GLOBAL_ALARM = true`.
* Forces `Drone Large` with a `2s` loop on `AIRLOCK_ALARM`.
* Falls back to `AIRLOCK_SOUND` if the `AIRLOCK_ALARM` group is missing or empty.

---

## Room Containment System

Rooms can be monitored independently from the airlocks. This allows the script to isolate a leaking room without shutting down the whole base.

### **Room Detection**
| Setup | Description |
| --- | --- |
| `[ROOM:Name]` on vents | Adds those vents to a monitored room. |
| `[ROOM:Name]` on lights | Adds those lights to room alarm feedback. |
| `[ROOM:Name]` on LCDs in `ROOM_STATUS_LCD` | Creates room-specific status displays. |
| `CONTAIN_<ROOM>` group | Defines doors, vents, lights, and sounds controlled during containment. |

### **Containment Logic**
1.  **Monitor:** The script reads room O2 from a `[PROBE]` vent first, or from normal vents if no probe exists.
2.  **Test:** If O2 drops below `ROOM_TEST_TRIGGER`, the room can run a pressurization test for `ROOM_TEST_DURATION`.
3.  **Contain:** If O2 remains below `ROOM_LEAK_THRESHOLD`, the room is isolated.
4.  **Lockdown:** Containment doors close, non-probe vents stop, lights blink red, and room sound blocks play `Alert 1`.
5.  **Release:** When O2 is above `ROOM_RELEASE_O2`, the script waits `RELEASE_DELAY_ON_RECOVER` before releasing auto-containment.

### **Manual Containment**
Manual containment changes use a protocol delay to prevent accidental toggles.

| Action | Behavior |
| --- | --- |
| `contain <room>` when inactive | Schedules containment activation after `MANUAL_CONTAIN_PROTO_SECONDS`. |
| `contain <room>` when active | Schedules containment release after `MANUAL_CONTAIN_PROTO_SECONDS`. |
| `suspend <room>` | Temporarily unlocks containment doors for `DOOR_TEMP_UNLOCK_SECONDS`. |

---

## LCD Display Logic

The script writes status information to three display categories: local airlock LCDs, global overview LCDs, and room LCDs.

### **Local Airlock LCD (`AIRLOCK_LCD`)**
Displayed information includes:
* **Secure state:** Active, armed, or disabled.
* **Emergency state:** Local or global emergency source and cause.
* **Mode:** Idle, pressurizing, depressurizing, opening, closing, or emergency phase.
* **Progress:** Countdown and progress bar during cycles, door hold, and emergency phases.
* **O2 status:** Internal oxygen percentage.
* **Door state:** Interior and exterior door status.

### **Global Overview LCD (`VENTS_OVERVIEW_LCD`)**
Displayed information includes:
* **Global secure status.**
* **Global emergency status and source airlock.**
* **All discovered airlocks with O2 and current action.**
* **Monitored rooms with O2, containment state, tests, and ventilation state.**

### **Room Status LCD (`ROOM_STATUS_LCD`)**
Displayed information includes:
* **Room name.**
* **Containment status.**
* **O2 percentage.**
* **Manual protocol countdown.**
* **Pressurization test countdown.**
* **Temporary unlock countdown.**
* **Auto-release countdown.**
* **Next auto-test delay when applicable.**

The script automatically sets LCDs to text display mode and writes compact dashboard-style text.

---

## Terminal Commands

The script accepts commands through the programmable block argument field.

### **Airlock Commands**
| Command | Action |
| --- | --- |
| `pressu` | Starts pressurization on the default airlock. |
| `pressurize` | Same as `pressu`. |
| `depressu` | Starts depressurization on the default airlock. |
| `depressurize` | Same as `depressu`. |
| `urgence` | Starts a manual emergency on the target airlock. |
| `emergency` | Same as `urgence`. |
| `scan` | Prints debug information about the current airlock block scan. |

### **Global Commands**
| Command | Action |
| --- | --- |
| `secure` | Toggles global secure mode. |
| `secure on` | Enables global secure mode. |
| `secure off` | Disables global secure mode. |
| `securite` | French alias for `secure`. |
| `confinement` | Alias for global secure mode. |

### **Containment Commands**
| Command | Action |
| --- | --- |
| `contain <room>` | Toggles manual containment for a room after the protocol delay. |
| `contain_<room>` | Alternate containment command format. |
| `suspend <room>` | Temporarily unlocks containment doors for that room. |

### **Targeting a Specific Airlock**
Commands can include a SAS name so they run against a specific airlock instead of the default one.

Examples:
* `pressu SAS 01`
* `depressu SAS 02`
* `urgence SAS Hangar`
* `scan SAS 03`

---

## Troubleshooting

| Problem | Fix |
| --- | --- |
| The script finds no airlock blocks | Check that the block is in the right group and has the correct `[SAS:...]` tag. |
| A command affects the wrong airlock | Add `[SAS:Name]` to the programmable block or include the SAS name in the command. |
| Doors do not open after a cycle | Check that both door groups exist, the doors are functional, and secure/emergency mode is not active. |
| Vents do not cycle | Check `AIRLOCK_VENTS`, make sure the vents are not tagged as `[PROBE]`, and verify they are on the same construct. |
| O2 readings look wrong | Add a dedicated vent to `AIRLOCK_SAS_PROBE` and tag it with `[PROBE]`. |
| Secure mode has no sound | Add sound blocks to `AIRLOCK_ALARM`, or ensure `AIRLOCK_SOUND` contains fallback sound blocks. |
| Secure mode sound loops too quickly or slowly | Check `SECURE_LOOP_PERIOD`; the default is `2.0f` for `Drone Large`. |
| Normal cycle sound is missing | Add sound blocks to `AIRLOCK_SOUND`. The script will select `Alert 3` automatically. |
| Global overview is blank | Add an LCD or display-capable block to `VENTS_OVERVIEW_LCD`. |
| Room LCD is blank | Add the display to `ROOM_STATUS_LCD` and include a matching `[ROOM:Name]` tag. |
| Room containment does not trigger | Check `[ROOM:Name]` vent tags and create a matching `CONTAIN_<ROOM>` group. |
| Containment doors reopen immediately | Check for `suspend <room>` state or wait for `DOOR_TEMP_UNLOCK_SECONDS` to expire. |
| Blocks on another grid are ignored | This is expected when `LIMIT_TO_CONSTRUCT = true`. |

---

## License & Author

Provided by **soraanoir/sundae https://github.com/soraanoir**.  
Free to use and modify, provided that credit is given to the author.
