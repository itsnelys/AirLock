# AirLock

[Français](#français) | [English](#english)

---

## Français

### Présentation
AirLock est un script Space Engineers pour piloter des sas pressurises. Il gere des groupes de portes, vents, lumieres, alarmes et LCD afin d'automatiser les cycles de pressurisation et depressurisation. Le projet sert a securiser les passages entre zones pressurisees et environnement exterieur.

### Fonctionnalités
- Gestion multi-SAS avec tags [SAS:...].
- Cycles automatiques de pressurisation et depressurisation.
- Interverrouillage des portes, detection de fuite O2 et modes urgence/securite.
- Affichage d'etat sur LCD locaux ou globaux.

### Installation
Ajoute Script_Airlock.cs dans un Programmable Block Space Engineers, puis cree les groupes et tags de blocs indiques par le script.

### Utilisation
Configure les groupes de sas, vents, portes et LCD dans la grille, puis lance le script depuis le Programmable Block. Les commandes documentees dans le script permettent de gerer les scans, urgences et modes de securite.

### Structure du dépôt

```text
Script_Airlock.cs
README.md
LICENSE
```

### Licence
Ce projet est distribue sous licence MIT. Copyright © 2026 itsnelys.

---

## English

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
