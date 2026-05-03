# Space Engineers Airlock System - Script Documentation

This *Space Engineers* programmable block script implements an **intelligent airlock system** that can manage automatic air cycling, leak detection, and automatic containment of compromised areas.

---

## Table of Contents

* [Initialization and Configuration](#initialization-and-configuration)
* [Cycling System](#cycling-system)
* [Emergency and Containment Protocols](#emergency-and-containment-protocols)
* [Terminal Commands](#terminal-commands)
* [LCD Display Logic](#lcd-display-logic)

---

## Initialization and Configuration

The system initializes by scanning the block groups defined in the script constants. It uses tags to distinguish multiple airlocks on the same grid.

### **Required Block Groups**
| Group Name | Description |
| --- | --- |
| `AIRLOCK_VENTS` | Air vents used for airlock cycling. |
| `AIRLOCK_DOORS_IN` | Doors leading to the base interior. |
| `AIRLOCK_DOORS_OUT` | Doors leading outside or to a hangar. |
| `AIRLOCK_LCD` | Local status display panels. |
| `AIRLOCK_LEDS` | Local airlock lights for state, cycling, secure mode, and emergencies. |
| `AIRLOCK_SOUND` | Airlock sound blocks: normal cycle sound and fallback if `AIRLOCK_ALARM` is missing. |
| `AIRLOCK_SAS_PROBE` | Airlock probe vents used to read O2 without driving the cycling logic. |
| `AIRLOCK_ALARM` | Global / secure alarm sound blocks, used first when the group exists. |
| `AIRLOCK_EMERGENCY_LEDS` | Global emergency and secure mode lights. |
| `AUTOCLOSE_DOORS` | Doors monitored by the global auto-close system. |
| `VENTS_OVERVIEW_LCD` | Global overview LCD for airlocks and monitored rooms. |

* **Tagging:** Use the `[SAS:AirlockName]` format in block names to assign blocks to a specific airlock.
* **Probes:** The `[PROBE]` tag on a vent allows accurate O2 readings without affecting pressurization.
* **Sounds:** Sound blocks do not need to be configured manually; the script forces the sound and loop duration based on the current state.

---

## Cycling System

The script automates transitions between pressurized and depressurized environments.

### **Operating Logic**
1.  **Check:** Ensures that no door is open before starting.
2.  **Interlock:** Locks the opposite doors during the cycle to prevent accidental decompression.
3.  **Equalization:** Runs the vents until the configured thresholds are reached (`0.95f` for pressurized, `0.05f` for depressurized).

---

## Emergency and Containment Protocols

The safety core of the system relies on constant area monitoring.

* **Leak Detection:** If the O2 level drops abnormally (default threshold: `20%`), the area enters containment mode.
* **Containment:** Locks the area's doors, shuts down non-essential ventilation, and activates visual alarms (blinking red LEDs).
* **Secure Mode:** A global command that locks all airlocks on the grid, useful for combat or intrusion alerts. The `Drone Large` sound is forced with a `2s` loop on `AIRLOCK_ALARM`, or on `AIRLOCK_SOUND` if no alarm group is available.

---

## Terminal Commands

The script accepts commands through the programmable block argument:

| Command | Action |
| --- | --- |
| `pressu` | Starts the default airlock pressurization cycle. |
| `depressu` | Starts the depressurization cycle. |
| `secure` | Toggles the global secure state (ON/OFF). |
| `contain <name>` | Manually enables or disables containment for a room. |
| `urgence` | Immediately triggers a manual emergency on the airlock. |

---

## LCD Display Logic

The LCD interface provides complete visual feedback:
* **O2 Status:** Displays internal oxygen as a percentage.
* **Door State:** Shows whether the INT or EXT doors are locked or open.
* **Alerts:** Shows the exact emergency cause (O2 leak, damaged door, manual).

---

## 📄 License & Author

Provided by **soraanoir/sundae https://github.com/soraanoir**.  
Free to use and modify, provided that credit is given to the author.
