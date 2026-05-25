# Space Engineers Airlock / SAS Controller

## FR

Script de Programmable Block pour *Space Engineers* qui gere un ou plusieurs SAS intelligents.

Il controle les cycles pressurisation/depressurisation, les interverrouillages de portes, les urgences, le mode securise, les fuites O2, le confinement des pieces, les LCD, les lumieres et les alarmes.

### Installation

1. Place un `Programmable Block`.
2. Copie `Script_Airlock.cs` dans le Programmable Block.
3. Cree les groupes de blocs indiques ci-dessous.
4. Ajoute des tags `[SAS:Nom]` si plusieurs SAS sont geres par le meme script.
5. Ajoute des tags `[ROOM:Nom]` si tu veux surveiller des pieces.
6. Compile et lance le script.

### Groupes principaux

| Groupe | Blocs | Description |
| --- | --- | --- |
| `AIRLOCK_VENTS` | Vents | Vents qui pressurisent ou depressurisent le SAS. |
| `AIRLOCK_DOORS_IN` | Portes | Portes cote interieur pressurise. |
| `AIRLOCK_DOORS_OUT` | Portes | Portes cote exterieur, hangar ou vide. |
| `AIRLOCK_LCD` | LCD / ecrans | Affichage local du SAS. |
| `AIRLOCK_LEDS` | Lumieres | Indicateurs locaux. |
| `AIRLOCK_SOUND` | Sound blocks | Son de cycle normal, fallback d'alarme locale. |
| `AIRLOCK_SAS_PROBE` | Vents | Sondes O2 optionnelles, jamais utilisees pour cycler. |
| `AIRLOCK_ALARM` | Sound blocks | Alarmes globales et mode securise. |
| `AIRLOCK_EMERGENCY_LEDS` | Lumieres | Lumieres globales d'urgence/securite. |
| `AUTOCLOSE_DOORS` | Portes | Portes optionnelles fermees automatiquement. |
| `VENTS_OVERVIEW_LCD` | LCD / ecrans | Vue globale des SAS et pieces. |
| `ROOM_STATUS_LCD` | LCD / ecrans | Statut des pieces surveillees. |
| `CONTAIN_<ROOM>` | Portes, vents, lumieres, sons | Zone de confinement d'une piece. |

### Tags

| Tag / Regle | Utilisation | Description |
| --- | --- | --- |
| `[SAS:Nom]` | Portes, vents, LCD, lumieres, sons | Assigne un bloc a un SAS precis. |
| `[ROOM:Nom]` | Vents, lumieres, LCD | Assigne un bloc a une piece surveillee. |
| `[PROBE]` | Vents | Marque une sonde O2 en lecture seule. |
| `CONTAIN_<ROOM>` | Nom de groupe | Definit les blocs d'une zone de confinement. |

Exemples : `[SAS:SAS 01]`, `[SAS:Hangar]`, `[ROOM:Medbay]`, `CONTAIN_Medbay`.

Si le Programmable Block porte un tag `[SAS:...]`, ce SAS devient la cible par defaut des commandes sans nom.

### Reglages importants

| Reglage | Valeur par defaut | Description |
| --- | --- | --- |
| `O2_PRESSURIZED` | `0.95f` | Niveau O2 considere pressurise. |
| `O2_DEPRESSURIZED` | `0.05f` | Niveau O2 considere depressurise. |
| `LEAK_O2_THRESHOLD` | `0.20f` | Seuil de fuite O2 pour un SAS. |
| `ROOM_LEAK_THRESHOLD` | `0.20f` | Seuil de fuite O2 pour une piece. |
| `ROOM_RELEASE_O2` | `0.25f` | O2 requis avant liberation d'un confinement auto. |
| `DOOR_MIN_HEALTH` | `0.60f` | Integrite minimum d'une porte avant urgence. |
| `CYCLE_SECONDS` | `10.0` | Duree d'un cycle de pression. |
| `AUTOCLOSE_SECONDS` | `8.0` | Delai de fermeture automatique des portes surveillees. |
| `DOOR_HOLD_SECONDS` | `30.0` | Temps d'ouverture de la porte de destination. |
| `LIMIT_TO_CONSTRUCT` | `true` | Ignore les blocs d'autres constructs. |

### Cycle SAS

| Commande | Mode des vents | Porte ouverte apres cycle |
| --- | --- | --- |
| `pressu` / `pressurize` | Pressurise | `AIRLOCK_DOORS_IN` |
| `depressu` / `depressurize` | Depressurise | `AIRLOCK_DOORS_OUT` |

Pendant un cycle, les deux cotes sont fermes et verrouilles. Le script evite l'ouverture simultanee des portes interieures et exterieures.

### Urgence et securite

Le script declenche ou gere :

- urgence manuelle avec `urgence` ou `emergency` ;
- fuite O2 sous `LEAK_O2_THRESHOLD` ;
- porte endommagee sous `DOOR_MIN_HEALTH` ;
- verrouillage des autres SAS si un SAS entre en urgence ;
- mode securise global avec `secure`, `secure on`, `secure off`, `securite` ou `confinement`.

Le mode securise ferme et verrouille les SAS, coupe les vents non-sondes, active les lumieres d'urgence et joue l'alarme globale.

### Confinement des pieces

Les pieces peuvent etre surveillees avec `[ROOM:Nom]` sur les vents, lumieres et LCD. Un groupe `CONTAIN_<ROOM>` definit les portes, vents, lumieres et sons a controler pendant un confinement.

| Commande | Action |
| --- | --- |
| `contain <room>` | Active ou libere le confinement apres le delai de protocole. |
| `contain_<room>` | Format alternatif. |
| `suspend <room>` | Deverrouille temporairement les portes de confinement. |

Le confinement automatique se declenche si l'O2 reste sous le seuil apres test. La liberation automatique attend que l'O2 remonte au-dessus de `ROOM_RELEASE_O2`.

### LCD

Le script peut afficher :

- etat local du SAS ;
- progression des cycles ;
- niveau O2 ;
- etat des portes ;
- mode securise ou urgence ;
- vue globale de tous les SAS ;
- statut des pieces et confinements.

### Commandes

| Commande | Action |
| --- | --- |
| `pressu` | Pressurise le SAS cible. |
| `pressurize` | Alias de `pressu`. |
| `depressu` | Depressurise le SAS cible. |
| `depressurize` | Alias de `depressu`. |
| `urgence` | Declenche une urgence locale. |
| `emergency` | Alias de `urgence`. |
| `scan` | Affiche les informations de detection. |
| `secure` | Bascule le mode securise global. |
| `secure on` | Active le mode securise global. |
| `secure off` | Desactive le mode securise global. |
| `securite` | Alias francais de `secure`. |
| `confinement` | Alias de securite globale. |

Commandes ciblees possibles : `pressu SAS 01`, `depressu SAS 02`, `urgence Hangar`, `scan SAS 03`.

### Depannage

| Probleme | Solution |
| --- | --- |
| Aucun bloc de SAS detecte | Verifie les groupes et les tags `[SAS:...]`. |
| La commande agit sur le mauvais SAS | Ajoute `[SAS:Nom]` au Programmable Block ou indique le nom dans la commande. |
| Les portes ne s'ouvrent pas | Verifie les groupes de portes, leur etat, et les modes urgence/securite. |
| Les vents ne cyclent pas | Verifie `AIRLOCK_VENTS` et assure-toi que les vents ne sont pas `[PROBE]`. |
| Les lectures O2 semblent fausses | Ajoute une sonde dans `AIRLOCK_SAS_PROBE` avec `[PROBE]`. |
| Le mode securise n'a pas de son | Ajoute des sound blocks dans `AIRLOCK_ALARM` ou `AIRLOCK_SOUND`. |
| Le LCD global est vide | Ajoute un ecran dans `VENTS_OVERVIEW_LCD`. |
| Le confinement ne part pas | Verifie les tags `[ROOM:Nom]` et le groupe `CONTAIN_<ROOM>`. |

### Note de patch 0.1

- Premiere version stable documentee.
- Gestion multi-SAS avec tags `[SAS:...]`.
- Cycles pressurisation/depressurisation avec interverrouillage des portes.
- Urgences locales, urgence globale et mode securise.
- Surveillance O2 et confinement automatique des pieces.
- Support LCD local, LCD global, lumieres et alarmes.

---

## EN

*Space Engineers* Programmable Block script that manages one or more intelligent airlocks.

It controls pressurization/depressurization cycles, door interlocks, emergencies, secure mode, O2 leak detection, room containment, LCDs, lights, and alarms.

### Installation

1. Place a `Programmable Block`.
2. Copy `Script_Airlock.cs` into the Programmable Block.
3. Create the block groups listed below.
4. Add `[SAS:Name]` tags if several airlocks are handled by the same script.
5. Add `[ROOM:Name]` tags if you want room monitoring.
6. Compile and run the script.

### Main Groups

| Group | Blocks | Description |
| --- | --- | --- |
| `AIRLOCK_VENTS` | Air vents | Vents used to pressurize or depressurize the airlock. |
| `AIRLOCK_DOORS_IN` | Doors | Doors leading to the pressurized interior side. |
| `AIRLOCK_DOORS_OUT` | Doors | Doors leading outside, to a hangar, or to vacuum. |
| `AIRLOCK_LCD` | LCDs / screens | Local airlock display. |
| `AIRLOCK_LEDS` | Lights | Local indicators. |
| `AIRLOCK_SOUND` | Sound blocks | Normal cycle sound, local alarm fallback. |
| `AIRLOCK_SAS_PROBE` | Air vents | Optional O2 probes, never used for cycling. |
| `AIRLOCK_ALARM` | Sound blocks | Global alarm and secure mode sounds. |
| `AIRLOCK_EMERGENCY_LEDS` | Lights | Global emergency/secure lights. |
| `AUTOCLOSE_DOORS` | Doors | Optional automatically closed doors. |
| `VENTS_OVERVIEW_LCD` | LCDs / screens | Global overview for airlocks and rooms. |
| `ROOM_STATUS_LCD` | LCDs / screens | Room status displays. |
| `CONTAIN_<ROOM>` | Doors, vents, lights, sounds | Room containment zone. |

### Tags

| Tag / Rule | Used On | Description |
| --- | --- | --- |
| `[SAS:Name]` | Doors, vents, LCDs, lights, sounds | Assigns a block to a specific airlock. |
| `[ROOM:Name]` | Vents, lights, LCDs | Assigns a block to a monitored room. |
| `[PROBE]` | Air vents | Marks a read-only O2 probe. |
| `CONTAIN_<ROOM>` | Group name | Defines the blocks of a containment zone. |

Examples: `[SAS:SAS 01]`, `[SAS:Hangar]`, `[ROOM:Medbay]`, `CONTAIN_Medbay`.

If the Programmable Block has a `[SAS:...]` tag, that airlock becomes the default target for commands without a name.

### Important Settings

| Setting | Default Value | Description |
| --- | --- | --- |
| `O2_PRESSURIZED` | `0.95f` | O2 level considered pressurized. |
| `O2_DEPRESSURIZED` | `0.05f` | O2 level considered depressurized. |
| `LEAK_O2_THRESHOLD` | `0.20f` | Airlock O2 leak threshold. |
| `ROOM_LEAK_THRESHOLD` | `0.20f` | Room O2 leak threshold. |
| `ROOM_RELEASE_O2` | `0.25f` | O2 required before auto-containment release. |
| `DOOR_MIN_HEALTH` | `0.60f` | Minimum door integrity before emergency. |
| `CYCLE_SECONDS` | `10.0` | Duration of a pressure cycle. |
| `AUTOCLOSE_SECONDS` | `8.0` | Delay before monitored doors auto-close. |
| `DOOR_HOLD_SECONDS` | `30.0` | Destination door hold-open time. |
| `LIMIT_TO_CONSTRUCT` | `true` | Ignores blocks on other constructs. |

### Airlock Cycle

| Command | Vent Mode | Door Opened After Cycle |
| --- | --- | --- |
| `pressu` / `pressurize` | Pressurize | `AIRLOCK_DOORS_IN` |
| `depressu` / `depressurize` | Depressurize | `AIRLOCK_DOORS_OUT` |

During a cycle, both sides are closed and locked. The script prevents the interior and exterior doors from opening at the same time.

### Emergency and Secure Mode

The script handles:

- manual emergency with `urgence` or `emergency`;
- O2 leak below `LEAK_O2_THRESHOLD`;
- damaged door below `DOOR_MIN_HEALTH`;
- remote locking of other airlocks when one airlock is in emergency;
- global secure mode with `secure`, `secure on`, `secure off`, `securite`, or `confinement`.

Secure mode closes and locks airlocks, turns off non-probe vents, enables emergency lights, and plays the global alarm.

### Room Containment

Rooms can be monitored with `[ROOM:Name]` on vents, lights, and LCDs. A `CONTAIN_<ROOM>` group defines the doors, vents, lights, and sounds controlled during containment.

| Command | Action |
| --- | --- |
| `contain <room>` | Activates or releases containment after the protocol delay. |
| `contain_<room>` | Alternate format. |
| `suspend <room>` | Temporarily unlocks containment doors. |

Automatic containment starts if O2 remains below the threshold after testing. Automatic release waits until O2 rises above `ROOM_RELEASE_O2`.

### LCD

The script can display:

- local airlock state;
- cycle progress;
- O2 level;
- door state;
- secure or emergency state;
- global overview of all airlocks;
- room and containment status.

### Commands

| Command | Action |
| --- | --- |
| `pressu` | Pressurizes the target airlock. |
| `pressurize` | Alias for `pressu`. |
| `depressu` | Depressurizes the target airlock. |
| `depressurize` | Alias for `depressu`. |
| `urgence` | Starts a local emergency. |
| `emergency` | Alias for `urgence`. |
| `scan` | Prints detection information. |
| `secure` | Toggles global secure mode. |
| `secure on` | Enables global secure mode. |
| `secure off` | Disables global secure mode. |
| `securite` | French alias for `secure`. |
| `confinement` | Global secure alias. |

Targeted command examples: `pressu SAS 01`, `depressu SAS 02`, `urgence Hangar`, `scan SAS 03`.

### Troubleshooting

| Problem | Fix |
| --- | --- |
| No airlock blocks are detected | Check groups and `[SAS:...]` tags. |
| Command affects the wrong airlock | Add `[SAS:Name]` to the Programmable Block or include the name in the command. |
| Doors do not open | Check door groups, door condition, and emergency/secure modes. |
| Vents do not cycle | Check `AIRLOCK_VENTS` and make sure cycle vents are not `[PROBE]`. |
| O2 readings look wrong | Add a probe in `AIRLOCK_SAS_PROBE` with `[PROBE]`. |
| Secure mode has no sound | Add sound blocks to `AIRLOCK_ALARM` or `AIRLOCK_SOUND`. |
| Global LCD is blank | Add a screen to `VENTS_OVERVIEW_LCD`. |
| Containment does not trigger | Check `[ROOM:Name]` tags and the `CONTAIN_<ROOM>` group. |

### Patch Note 0.1

- First documented stable release.
- Multi-airlock support with `[SAS:...]` tags.
- Pressurization/depressurization cycles with door interlocks.
- Local emergencies, global emergency handling, and secure mode.
- O2 monitoring and automatic room containment.
- Support for local LCDs, global LCDs, lights, and alarms.

---

## License & Author

Provided by **soraanoir/sundae https://github.com/soraanoir**.  
Free to use and modify, provided that credit is given to the author.
