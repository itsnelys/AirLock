# AirLock

### Overview
AirLock is a Space Engineers script for controlling pressurized airlocks. It manages door, vent, light, alarm and LCD groups to automate pressurization and depressurization cycles. The project helps secure transitions between pressurized rooms and outside environments.

### Features
- Multi-airlock management with [SAS:...] tags.
- Automatic pressurization and depressurization cycles.
- Door interlocks, O2 leak detection and emergency/security modes.
- Status display on local or global LCD panels.

### Installation
Add Script_Airlock.cs to a Space Engineers Programmable Block, then create the block groups and tags expected by the script.

### Usage
Configure the airlock doors, vents and LCD groups on the grid, then run the script from the Programmable Block. Script commands can refresh scans, trigger emergencies and control security modes.

### Repository Structure

```text
Script_Airlock.cs
README.md
LICENSE
```

### License
This project is released under the MIT License. Copyright © 2026 itsnelys.
