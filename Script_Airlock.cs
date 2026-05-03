const string VENTS_GROUP        = "AIRLOCK_VENTS";
const string DOORS_IN_GROUP     = "AIRLOCK_DOORS_IN";
const string DOORS_OUT_GROUP    = "AIRLOCK_DOORS_OUT";
const string LCD_GROUP          = "AIRLOCK_LCD";
const string LEDS_GROUP         = "AIRLOCK_LEDS";
const string SOUND_NORMAL_GROUP = "AIRLOCK_SOUND";
const string PROBE_VENTS_GROUP  = "AIRLOCK_SAS_PROBE";

const string EMERGENCY_LEDS_GROUP  = "AIRLOCK_EMERGENCY_LEDS";
const string SOUND_ALARM_GROUP     = "AIRLOCK_ALARM";
const string AUTOCLOSE_DOORS_GROUP = "AUTOCLOSE_DOORS";
const string OVERVIEW_LCD_GROUP    = "VENTS_OVERVIEW_LCD";

const bool SECURE_USES_GLOBAL_LEDS  = true;
const bool SECURE_USES_GLOBAL_ALARM = true;
const bool LIMIT_TO_CONSTRUCT       = true;

bool globalSecure = false;

bool SameConstruct(IMyTerminalBlock b) {
  return !LIMIT_TO_CONSTRUCT || (b != null && b.IsSameConstructAs(Me));
}
bool globalEmergencyActive = false;
Dictionary<long, bool> globalVentsBackup = new Dictionary<long, bool>();
Dictionary<long, bool> secureVentsBackup = new Dictionary<long, bool>();
Dictionary<long, float> emergencyBaseIntensity = new Dictionary<long, float>();
bool secureApplied = false;

string NormalizeSasName(string s) {
  if (string.IsNullOrWhiteSpace(s)) return null;
  s = s.Trim().Replace(':',' ').Replace('_',' ').Replace('-', ' ');
  var parts = s.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
  for (int i=0; i<parts.Length; i++) {
    if (string.Equals(parts[i], "SAS", StringComparison.OrdinalIgnoreCase)) {
      if (i+1 < parts.Length) {
        int n; var num = parts[i+1].Trim();
        if (int.TryParse(num, out n)) return "SAS " + n.ToString("00");
        return "SAS " + num;
      }
      return "SAS 01";
    }
  }
  if (s.StartsWith("SAS", StringComparison.OrdinalIgnoreCase)) {
    var rest = s.Substring(3).Trim();
    int n; if (int.TryParse(rest, out n)) return "SAS " + n.ToString("00");
    return "SAS " + rest;
  }
  return s;
}

const string SAS_TAG_PREFIX = "[SAS:";
string SAS_NAME = "SAS 01";

string ExtractSasTag(string name) {
  if (string.IsNullOrEmpty(name)) return null;
  int i = name.IndexOf(SAS_TAG_PREFIX, StringComparison.OrdinalIgnoreCase);
  if (i < 0) return null;
  int j = name.IndexOf(']', i + SAS_TAG_PREFIX.Length);
  if (j < 0) return null;
  var raw = name.Substring(i + SAS_TAG_PREFIX.Length, j - (i + SAS_TAG_PREFIX.Length)).Trim();
  return string.IsNullOrEmpty(raw) ? null : NormalizeSasName(raw);
}

string ExtractSasFromArg(string rawArg) {
  if (string.IsNullOrWhiteSpace(rawArg)) return null;
  var a = rawArg.Trim();
  int i = a.LastIndexOf("SAS", StringComparison.OrdinalIgnoreCase);
  if (i < 0) return null;
  var tail = a.Substring(i).Trim().Replace(':',' ').Replace('_',' ');
  var parts = tail.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
  if (parts.Length >= 2 && parts[0].Equals("SAS", StringComparison.OrdinalIgnoreCase)) {
    return NormalizeSasName(parts[0] + " " + parts[1]);
  }
  return null;
}

bool MatchesSas(IMyTerminalBlock b) {
  var tag = ExtractSasTag(b?.CustomName ?? "");
  if (tag == null) return false;
  var target = NormalizeSasName(SAS_NAME);
  return string.Equals(tag, target, StringComparison.OrdinalIgnoreCase);
}

string _sasNameBackup = null;

void WithSas(string sas, Action body) {
  _sasNameBackup = SAS_NAME;
  if (!string.IsNullOrEmpty(sas)) SAS_NAME = NormalizeSasName(sas);
  CollectBlocks();
  try { if (body != null) body(); }
  finally { SAS_NAME = _sasNameBackup ?? SAS_NAME; _sasNameBackup = null; }
}

const string CONTAIN_GROUP_PREFIX = "CONTAIN_";
const string ROOM_LCD_GROUP       = "ROOM_STATUS_LCD";
const string ROOM_TAG_PREFIX      = "[ROOM:";
const string PROBE_TAG            = "[PROBE]";

const float ROOM_LEAK_THRESHOLD      = 0.20f;
const float ROOM_RELEASE_O2          = 0.25f;
const float ROOM_DROP_RETEST_TRIGGER = 0.20f;

const double ROOM_TEST_DURATION = 10.0;
const double ROOM_RETEST_PERIOD = 60.0;

const double DOOR_TEMP_UNLOCK_SECONDS = 8.0;
const double RELEASE_DELAY_ON_RECOVER = 10.0; 

const float ROOM_TEST_TRIGGER = 0.20f;

const double MANUAL_CONTAIN_PROTO_SECONDS = 10.0;

const float O2_PRESSURIZED      = 0.95f;
const float O2_DEPRESSURIZED    = 0.05f;
const float LEAK_O2_THRESHOLD   = 0.20f;
const float DOOR_MIN_HEALTH     = 0.60f;
const double CYCLE_SECONDS      = 10.0;
const double AUTOCLOSE_SECONDS  = 8.0;
const double DOOR_HOLD_SECONDS  = 30.0;

const double EMERG_P1_SECONDS    = 30.0;
const double EMERG_P2_SECONDS    = 5.0;
const bool RESTORE_AFTER_EMERG   = true;
bool emergMidChecked = false;

const string SOUND_PRESSURE_CYCLE = "Alert 3"; 
const string SOUND_GLOBAL_ALARM   = "Alert 2"; 
const string SOUND_LOCAL_ALARM    = "Alert 1"; 
const string SOUND_SECURE_ALARM   = "Drone Large";
const string DEFAULT_ROOM_ALERT_SOUND = "Alert 1";
const float NORMAL_LOOP_PERIOD    = 1.0f;
const double NORMAL_KICK_PERIOD   = 1.0;
const float ALARM_LOOP_PERIOD     = 1.0f;
const double ALARM_KICK_PERIOD    = 1.0;
const float SECURE_LOOP_PERIOD    = 2.0f;
const double SECURE_KICK_PERIOD   = 2.0;

class SasState {
  public Mode mode = Mode.Idle;
  public double modeStart = 0;

  public EnvTarget envTarget = EnvTarget.Unknown;
  public EmergencyCause emergencyCause = EmergencyCause.None;

  public Dictionary<long,double> openSince = new Dictionary<long,double>();
  public long? activeDoorId = null;

  public bool phaseGate = false;
  public bool openedThisPhase = false;

  public bool normalSoundOn = false, alarmSoundOn = false;
  public double normalLastKick = -9999, alarmLastKick = -9999;
  public Dictionary<long, bool> backupAllVentsEnabled = new Dictionary<long, bool>();
}

Dictionary<string, SasState> sasStates = new Dictionary<string, SasState>(System.StringComparer.OrdinalIgnoreCase);

SasState GetState(string sas) {
  SasState st;
  if (!sasStates.TryGetValue(sas, out st)) { st = new SasState(); sasStates[sas] = st; }
  return st;
}

bool globalAlarmOn = false;
double globalAlarmLastKick = -9999;

IMyGridTerminalSystem GTS;
List<IMyAirVent> vents = new List<IMyAirVent>();
List<IMyAirVent> probeVents = new List<IMyAirVent>();
List<IMyDoor> doorsIN  = new List<IMyDoor>();
List<IMyDoor> doorsOUT = new List<IMyDoor>();
List<IMyDoor> doorsAuto= new List<IMyDoor>();

List<IMyTextSurface> lcds = new List<IMyTextSurface>();
List<IMyTextSurface> lcdOverview = new List<IMyTextSurface>();
List<IMyLightingBlock> leds = new List<IMyLightingBlock>();

List<IMyLightingBlock> ledsEmergency = new List<IMyLightingBlock>();
List<IMySoundBlock> sndNormal  = new List<IMySoundBlock>();
List<IMySoundBlock> sndAlarm   = new List<IMySoundBlock>();

HashSet<string> autoContainedRooms = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

Dictionary<string, double> containTempDoorUnlockUntil = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);

Dictionary<string, double> containPendingReleaseAt = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);

Dictionary<string, float> roomLastO2 = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, double> roomTestUntil   = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, double> roomLastTestAt = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, double> roomNextTestAt = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);

Dictionary<string, bool>  manualContainTargetOn  = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, double> manualContainApplyAt = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, bool> manualDesired = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);

Dictionary<string, List<IMySoundBlock>> containSounds = new Dictionary<string, List<IMySoundBlock>>(System.StringComparer.OrdinalIgnoreCase);

List<IMyAirVent> allVents = new List<IMyAirVent>();
List<IMyLightingBlock> roomLights = new List<IMyLightingBlock>();
Dictionary<string, List<IMyAirVent>> roomToVents = new Dictionary<string, List<IMyAirVent>>();
Dictionary<string, List<IMyLightingBlock>> roomToLights = new Dictionary<string, List<IMyLightingBlock>>();
Dictionary<string, IMyAirVent> roomPrimaryVent = new Dictionary<string, IMyAirVent>();
Dictionary<long, bool> backupAllVentsEnabled = new Dictionary<long, bool>();

Dictionary<string, List<IMyAirVent>>        containVents  = new Dictionary<string, List<IMyAirVent>>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, List<IMyDoor>>           containDoors  = new Dictionary<string, List<IMyDoor>>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, List<IMyLightingBlock>>  containLights = new Dictionary<string, List<IMyLightingBlock>>(System.StringComparer.OrdinalIgnoreCase);
HashSet<string> containActive = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
Dictionary<string, List<IMyTextSurface>> roomLCDs = new Dictionary<string, List<IMyTextSurface>>(System.StringComparer.OrdinalIgnoreCase);

enum Mode {
  Idle,
  PreLockPressurize, PreLockDepressurize,
  Pressurizing, Depressurizing,
  AfterCyclePressu, AfterCycleDepressu,
  HoldOpenIn, HoldOpenOut,
  ClosingIn, ClosingOut,
  EmergencyPhase1, EmergencyPhase2
}
Mode mode = Mode.Idle;
double modeStart = 0;

enum EnvTarget { Unknown, Pressurized, Depressurized }
EnvTarget envTarget = EnvTarget.Unknown;

enum EmergencyCause { None, Manual, O2Leak, DoorDamage }
EmergencyCause emergencyCause = EmergencyCause.None;

Dictionary<long,double> openSince = new Dictionary<long,double>();
IMyDoor activeDoor = null;

bool phaseGate = false;
bool openedThisPhase = false;

bool normalSoundOn = false, alarmSoundOn = false;
double normalLastKick = -9999, alarmLastKick = -9999;

double _now = 0;
double Now { get { return _now; } }
double Elapsed(double t0) { return Now - t0; }

void AdvanceClock(UpdateType src)
{
  double dt = Runtime.TimeSinceLastRun.TotalSeconds;
  if (!(dt > 0) || double.IsNaN(dt) || double.IsInfinity(dt))
  {
    if ((Runtime.UpdateFrequency & UpdateFrequency.Update1) != 0) dt = 1.0 / 60.0;
    else if ((Runtime.UpdateFrequency & UpdateFrequency.Update10) != 0) dt = 1.0 / 6.0;
    else if ((Runtime.UpdateFrequency & UpdateFrequency.Update100) != 0) dt = 100.0 / 60.0;
    else dt = 1.0 / 6.0;
  }
  _now += dt;
}

const string STORAGE_VERSION = "SAS-PERSIST-V1";

string F64(double v){ return v.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
string F32(float  v){ return v.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
bool PD(string s, out double v){ return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v); }
bool PF(string s, out float  v){ return float .TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v); }

string Escape(string s){
    if (s == null) return "";
    return s.Replace("\\","\\\\").Replace("|","\\p").Replace(";", "\\s").Replace(":", "\\c").Replace(",", "\\k");
}
string Unescape(string s){
    if (s == null) return "";
    var sb = new System.Text.StringBuilder(s.Length);
    bool esc = false;
    for (int i=0;i<s.Length;i++){
        char ch = s[i];
        if (!esc){
            if (ch=='\\'){ esc=true; continue; }
            sb.Append(ch);
        } else {
            esc = false;
            if      (ch=='p') sb.Append('|');
            else if (ch=='s') sb.Append(';');
            else if (ch=='c') sb.Append(':');
            else if (ch=='k') sb.Append(',');
            else if (ch=='\\') sb.Append('\\');
            else sb.Append(ch);
        }
    }
    return sb.ToString();
}

Dictionary<string,string> SplitKV(string line){
    var d = new Dictionary<string,string>(System.StringComparer.OrdinalIgnoreCase);
    var keySb = new System.Text.StringBuilder();
    var valSb = new System.Text.StringBuilder();
    bool inKey = true, esc = false;

    Action flush = () => {
        if (keySb.Length > 0) d[Unescape(keySb.ToString())] = Unescape(valSb.ToString());
        keySb.Length = 0; valSb.Length = 0; inKey = true;
    };

    for (int i=0;i<line.Length;i++){
        char ch = line[i];
        if (esc){
            char add = ch=='p'?'|': ch=='s'?';': ch=='c'?':': ch=='k'?',' : ch=='\\'?'\\': ch;
            if (inKey) keySb.Append(add); else valSb.Append(add);
            esc = false; continue;
        }
        if (ch=='\\'){ esc = true; continue; }
        if (inKey && ch==':'){ inKey=false; continue; }
        if (ch==';'){ flush(); continue; }
        if (inKey) keySb.Append(ch); else valSb.Append(ch);
    }
    flush();
    return d;
}

string JoinKV(List<KeyValuePair<string,string>> kvs){
    var sb = new System.Text.StringBuilder(kvs.Count*16);
    for (int i=0;i<kvs.Count;i++){
        if (i>0) sb.Append(';');
        sb.Append(Escape(kvs[i].Key)).Append(':').Append(Escape(kvs[i].Value));
    }
    return sb.ToString();
}

string SerializeAll(){
    var sb = new System.Text.StringBuilder(8192);
    sb.AppendLine(STORAGE_VERSION);

  
    sb.Append("G|secure=").Append(globalSecure ? "1":"0")
      .Append(";secureApplied=").Append(secureApplied ? "1":"0")
      .AppendLine();

  
    if (containActive.Count>0)           sb.Append("CA|").Append(string.Join(",", new List<string>(containActive).ToArray())).AppendLine();
    if (autoContainedRooms.Count>0)      sb.Append("AA|").Append(string.Join(",", new List<string>(autoContainedRooms).ToArray())).AppendLine();

    if (manualDesired.Count>0){
        var pairs = new List<string>();
        foreach (var it in manualDesired) pairs.Add(Escape(it.Key)+":"+(it.Value?"1":"0"));
        sb.Append("MD|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (containTempDoorUnlockUntil.Count>0){
        var pairs = new List<string>();
        foreach (var it in containTempDoorUnlockUntil) pairs.Add(Escape(it.Key)+":"+F64(it.Value));
        sb.Append("TU|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (containPendingReleaseAt.Count>0){
        var pairs = new List<string>();
        foreach (var it in containPendingReleaseAt) pairs.Add(Escape(it.Key)+":"+F64(it.Value));
        sb.Append("PR|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (roomNextTestAt.Count>0){
        var pairs = new List<string>();
        foreach (var it in roomNextTestAt) pairs.Add(Escape(it.Key)+":"+F64(it.Value));
        sb.Append("NT|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (manualContainApplyAt.Count>0){
        var pairs = new List<string>();
        foreach (var it in manualContainApplyAt) pairs.Add(Escape(it.Key)+":"+F64(it.Value));
        sb.Append("PA|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (roomLastO2.Count>0){
        var pairs = new List<string>();
        foreach (var it in roomLastO2) pairs.Add(Escape(it.Key)+":"+F32(it.Value));
        sb.Append("LO2|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }
    if (roomTestUntil.Count>0){
        var pairs = new List<string>();
        foreach (var it in roomTestUntil) pairs.Add(Escape(it.Key)+":"+F64(it.Value));
        sb.Append("RTU|").Append(string.Join(";", pairs.ToArray())).AppendLine();
    }

  
    foreach (var kvS in sasStates){
        string name = kvS.Key;
        var S = kvS.Value;

        var baseKVs = new List<KeyValuePair<string,string>>();
        baseKVs.Add(new KeyValuePair<string,string>("name", name));
        baseKVs.Add(new KeyValuePair<string,string>("mode", ((int)S.mode).ToString()));
        baseKVs.Add(new KeyValuePair<string,string>("mstart", F64(S.modeStart)));
        baseKVs.Add(new KeyValuePair<string,string>("tgt", ((int)S.envTarget).ToString()));
        baseKVs.Add(new KeyValuePair<string,string>("cause", ((int)S.emergencyCause).ToString()));
        baseKVs.Add(new KeyValuePair<string,string>("nOn", S.normalSoundOn?"1":"0"));
        baseKVs.Add(new KeyValuePair<string,string>("aOn", S.alarmSoundOn?"1":"0"));
        baseKVs.Add(new KeyValuePair<string,string>("nKick", F64(S.normalLastKick)));
        baseKVs.Add(new KeyValuePair<string,string>("aKick", F64(S.alarmLastKick)));
        baseKVs.Add(new KeyValuePair<string,string>("door", S.activeDoorId.HasValue? S.activeDoorId.Value.ToString() : "0"));

        sb.Append("SAS|").Append(JoinKV(baseKVs)).AppendLine();

        if (S.openSince != null && S.openSince.Count>0){
            var pairsOS = new List<string>();
            foreach (var os in S.openSince) pairsOS.Add(os.Key+":"+F64(os.Value));
            sb.Append("OS|").Append(Escape(name)).Append('|').Append(string.Join(";", pairsOS.ToArray())).AppendLine();
        }
        if (S.backupAllVentsEnabled != null && S.backupAllVentsEnabled.Count>0){
            var pairsBV = new List<string>();
            foreach (var b in S.backupAllVentsEnabled) pairsBV.Add(b.Key+":"+(b.Value?"1":"0"));
            sb.Append("BV|").Append(Escape(name)).Append('|').Append(string.Join(";", pairsBV.ToArray())).AppendLine();
        }
    }

  
    if (globalVentsBackup.Count>0){
        var pairsG = new List<string>();
        foreach (var b in globalVentsBackup) pairsG.Add(b.Key+":"+(b.Value?"1":"0"));
        sb.Append("GBV|").Append(string.Join(";", pairsG.ToArray())).AppendLine();
    }
    if (secureVentsBackup.Count>0){
        var pairsS = new List<string>();
        foreach (var b in secureVentsBackup) pairsS.Add(b.Key+":"+(b.Value?"1":"0"));
        sb.Append("SBV|").Append(string.Join(";", pairsS.ToArray())).AppendLine();
    }

    return sb.ToString();
}

void DeserializeAll(string data){
    if (string.IsNullOrEmpty(data)) return;
    var lines = data.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length==0 || lines[0]!=STORAGE_VERSION) return;

  
    containActive.Clear();
    autoContainedRooms.Clear();
    manualDesired.Clear();
    containTempDoorUnlockUntil.Clear();
    containPendingReleaseAt.Clear();
    roomNextTestAt.Clear();
    manualContainApplyAt.Clear();
    roomLastO2.Clear();
    roomTestUntil.Clear();
    sasStates.Clear();
    globalVentsBackup.Clear();
    secureVentsBackup.Clear();

    for (int i=1;i<lines.Length;i++){
        string ln = lines[i];

        if (ln.StartsWith("G|")){
            var kv = SplitKV(ln.Substring(2));
            string v;
            if (kv.TryGetValue("secure", out v)) globalSecure = (v=="1");
            if (kv.TryGetValue("secureApplied", out v)) secureApplied = (v=="1");
        }
        else if (ln.StartsWith("CA|")){
            var arr = ln.Substring(3).Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
            for (int k=0;k<arr.Length;k++) containActive.Add(arr[k]);
        }
        else if (ln.StartsWith("AA|")){
            var arr = ln.Substring(3).Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
            for (int k=0;k<arr.Length;k++) autoContainedRooms.Add(arr[k]);
        }
        else if (ln.StartsWith("MD|")){
            var d = SplitKV(ln.Substring(3));
            foreach (var kvp in d) manualDesired[kvp.Key] = (kvp.Value=="1");
        }
        else if (ln.StartsWith("TU|")){
            var d = SplitKV(ln.Substring(3));
            foreach (var kvp in d){ double t; if (PD(kvp.Value, out t)) containTempDoorUnlockUntil[kvp.Key]=t; }
        }
        else if (ln.StartsWith("PR|")){
            var d = SplitKV(ln.Substring(3));
            foreach (var kvp in d){ double t; if (PD(kvp.Value, out t)) containPendingReleaseAt[kvp.Key]=t; }
        }
        else if (ln.StartsWith("NT|")){
            var d = SplitKV(ln.Substring(3));
            foreach (var kvp in d){ double t; if (PD(kvp.Value, out t)) roomNextTestAt[kvp.Key]=t; }
        }
        else if (ln.StartsWith("PA|")){
            var d = SplitKV(ln.Substring(3));
            foreach (var kvp in d){ double t; if (PD(kvp.Value, out t)) manualContainApplyAt[kvp.Key]=t; }
        }
        else if (ln.StartsWith("LO2|")){
            var d = SplitKV(ln.Substring(4));
            foreach (var kvp in d){ float f; if (PF(kvp.Value, out f)) roomLastO2[kvp.Key]=f; }
        }
        else if (ln.StartsWith("RTU|")){
            var d = SplitKV(ln.Substring(4));
            foreach (var kvp in d){ double t; if (PD(kvp.Value, out t)) roomTestUntil[kvp.Key]=t; }
        }
        else if (ln.StartsWith("SAS|")){
            var d = SplitKV(ln.Substring(4));
            string name; if (!d.TryGetValue("name", out name)) continue;
            var S = GetState(name);

            int iv; double dv; long lv; string tmp;

            if (d.TryGetValue("mode", out tmp) && int.TryParse(tmp, out iv)) S.mode = (Mode)iv;
            if (d.TryGetValue("mstart", out tmp) && PD(tmp, out dv)) S.modeStart = dv;
            if (d.TryGetValue("tgt", out tmp) && int.TryParse(tmp, out iv)) S.envTarget = (EnvTarget)iv;
            if (d.TryGetValue("cause", out tmp) && int.TryParse(tmp, out iv)) S.emergencyCause = (EmergencyCause)iv;
            if (d.TryGetValue("nOn", out tmp)) S.normalSoundOn = (tmp=="1");
            if (d.TryGetValue("aOn", out tmp)) S.alarmSoundOn  = (tmp=="1");
            if (d.TryGetValue("nKick", out tmp) && PD(tmp, out dv)) S.normalLastKick = dv;
            if (d.TryGetValue("aKick", out tmp) && PD(tmp, out dv)) S.alarmLastKick  = dv;
            if (d.TryGetValue("door", out tmp) && long.TryParse(tmp, out lv) && lv!=0) S.activeDoorId = lv;
        }
        else if (ln.StartsWith("OS|")){
            int pipe = ln.IndexOf('|',3); if (pipe<0) continue;
            string name = Unescape(ln.Substring(3, pipe-3));
            string rest = ln.Substring(pipe+1);
            var S = GetState(name);
            S.openSince = new Dictionary<long,double>();
            var pairs = rest.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            for (int p=0;p<pairs.Length;p++){
                var kv = pairs[p].Split(':'); if (kv.Length!=2) continue;
                long id; double t;
                if (long.TryParse(kv[0], out id) && PD(kv[1], out t)) S.openSince[id]=t;
            }
        }
        else if (ln.StartsWith("BV|")){
            int pipe = ln.IndexOf('|',3); if (pipe<0) continue;
            string name = Unescape(ln.Substring(3, pipe-3));
            string rest = ln.Substring(pipe+1);
            var S = GetState(name);
            S.backupAllVentsEnabled = new Dictionary<long,bool>();
            var pairs = rest.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            for (int p=0;p<pairs.Length;p++){
                var kv = pairs[p].Split(':'); if (kv.Length!=2) continue;
                long id;
                if (long.TryParse(kv[0], out id)) S.backupAllVentsEnabled[id] = (kv[1]=="1");
            }
        }
        else if (ln.StartsWith("GBV|")){
            var pairs = ln.Substring(4).Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            for (int p=0;p<pairs.Length;p++){
                var kv = pairs[p].Split(':'); if (kv.Length!=2) continue;
                long id; if (long.TryParse(kv[0], out id)) globalVentsBackup[id] = (kv[1]=="1");
            }
        }
        else if (ln.StartsWith("SBV|")){
            var pairs = ln.Substring(4).Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            for (int p=0;p<pairs.Length;p++){
                var kv = pairs[p].Split(':'); if (kv.Length!=2) continue;
                long id; if (long.TryParse(kv[0], out id)) secureVentsBackup[id] = (kv[1]=="1");
            }
        }
    }
}

public Program() {
    GTS = GridTerminalSystem;

  
    if (!string.IsNullOrEmpty(Storage)) {
        try { DeserializeAll(Storage); } catch {}
    }

  
  
    var meTag = ExtractSasTag(Me.CustomName);
    if (!string.IsNullOrEmpty(meTag)) SAS_NAME = meTag;
    SAS_NAME = NormalizeSasName(SAS_NAME);

    CollectBlocks();
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save() {
    try { Storage = SerializeAll(); }
    catch { Storage = ""; }
}

public void Main(string argument, UpdateType updateSource)
{
  AdvanceClock(updateSource);
  if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
    CollectBlocks();

  string rawArg     = (argument ?? "").Trim();
  string arg        = rawArg.ToLowerInvariant();
  string sasFromArg = ExtractSasFromArg(rawArg);

  string __src; EmergencyCause __cause;
  bool anyEmergNow = ComputeGlobalEmergency(out __src, out __cause);


  if (arg == "secure on" || arg == "securite on" || arg == "confinement on") {
    globalSecure = true;
    ActivateSecure();
    Echo("[SECURE] Sécurité globale ACTIVÉE.");
    Save();
  }
  else if (arg == "secure off" || arg == "securite off" || arg == "confinement off") {
    globalSecure = false;
    DeactivateSecure();
    Echo("[SECURE] Sécurité globale DÉSACTIVÉE.");
    Save();
  }
  else if (arg == "secure" || arg == "securite" || arg == "confinement") {
    globalSecure = !globalSecure;
    if (globalSecure) {
      ActivateSecure();
      Echo("[SECURE] Sécurité globale ACTIVÉE.");
    } else {
      DeactivateSecure();
      Echo("[SECURE] Sécurité globale DÉSACTIVÉE.");
    }
    Save();
  }


  if (!string.IsNullOrEmpty(rawArg) &&
      (arg.StartsWith("pressu") || arg.StartsWith("depressu") ||
       arg.StartsWith("emergency") || arg.StartsWith("urgence") ||
       arg.StartsWith("contain") || arg.StartsWith("suspend") ||
       arg.StartsWith("scan")))
  {
    bool isHardCmd = (arg.StartsWith("pressu") || arg.StartsWith("depressu") ||
                      arg.StartsWith("emergency") || arg.StartsWith("urgence"));

    string _src; EmergencyCause _cause;
    bool anyEmergency = ComputeGlobalEmergency(out _src, out _cause);

    if (isHardCmd && (anyEmergency || globalSecure)) {
      Echo("[CMD] Bloqué : une URGENCE est en cours. Attendez la fin de l’alerte.");
    }
    else
    {
      string target = sasFromArg ?? SAS_NAME;
      WithSas(target, () =>
      {
        LoadSasState(SAS_NAME);

        if (arg.StartsWith("scan")) { DebugScanSas(); }
        else if (arg.StartsWith("pressurize") || arg.StartsWith("pressu")) { BeginPreLock(true); }
        else if (arg.StartsWith("depressurize") || arg.StartsWith("depressu")) { BeginPreLock(false); }
        else if (arg.StartsWith("emergency") || arg.StartsWith("urgence"))
        {
          LoadSasState(SAS_NAME);
          BeginEmergency(EmergencyCause.Manual);
          SaveSasState(SAS_NAME);
          Echo("[EMERG] Urgence MANUELLE locale sur " + SAS_NAME + ".");
        }

        else if (arg.StartsWith("contain"))
        {
          var key = ParseContainKey(rawArg);
          if (!string.IsNullOrEmpty(key))
          {
            bool isActive = containActive.Contains(NormalizeKey(key));
            ManualRequestContain(key, !isActive);
          }
        }
        else if (arg.StartsWith("suspend "))
        {
          var parts = rawArg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          if (parts.Length >= 2)
          {
            string room = string.Join(" ", parts, 1, parts.Length - 1);
            SuspendContain(room);
          }
          else Echo("Usage: suspend <room>");
        }

        MonitorSasCompromise();
        MonitorRoomLeaks();
        ProcessManualPendingContain();
        MaintainInterlocks();
        EnforceContainDoorsClosedAndLocked();
        RunStateMachine();
        AutoCloseDoors();
        UpdateSignals();
        DrawLCD();
        DrawOverviewLCD();
        DrawRoomLCDs();
        EchoStatus();

        SaveSasState(SAS_NAME);
      });
    }
  }


  var all = DiscoverAllSasNames();
  foreach (var sas in all)
  {
    WithSas(sas, () =>
    {
      LoadSasState(SAS_NAME);
      CollectBlocks();

      MonitorSasCompromise();
      MonitorRoomLeaks();
      ProcessManualPendingContain();
      MaintainInterlocks();
      EnforceContainDoorsClosedAndLocked();
      RunStateMachine();
      AutoCloseDoors();
      UpdateSignals();
      DrawLCD();
      DrawOverviewLCD();
      DrawRoomLCDs();
      EchoStatus();

      SaveSasState(SAS_NAME);
    });
  }

  UpdateGlobalEmergencySignals();
}

void CollectBlocks()
{
  vents.Clear(); probeVents.Clear(); doorsIN.Clear(); doorsOUT.Clear(); doorsAuto.Clear();
  lcds.Clear(); lcdOverview.Clear(); leds.Clear(); ledsEmergency.Clear();
  sndNormal.Clear(); sndAlarm.Clear();
  allVents.Clear(); roomLights.Clear(); roomToVents.Clear(); roomToLights.Clear();
  roomPrimaryVent.Clear(); containVents.Clear(); containDoors.Clear(); containLights.Clear();
  roomLCDs.Clear();

  var gV = GTS.GetBlockGroupWithName(VENTS_GROUP);
  if (gV != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gV.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var v = tmp[i] as IMyAirVent; if (v != null) vents.Add(v); }
  }

  var gProbe = GTS.GetBlockGroupWithName(PROBE_VENTS_GROUP);
  if (gProbe != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gProbe.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var v = tmp[i] as IMyAirVent; if (v != null) probeVents.Add(v); }
  }
  var gI = GTS.GetBlockGroupWithName(DOORS_IN_GROUP);
  if (gI != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gI.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var d = tmp[i] as IMyDoor; if (d != null) doorsIN.Add(d); }
  }
  var gO = GTS.GetBlockGroupWithName(DOORS_OUT_GROUP);
  if (gO != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gO.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var d = tmp[i] as IMyDoor; if (d != null) doorsOUT.Add(d); }
  }

  var gL = GTS.GetBlockGroupWithName(LCD_GROUP);
  if (gL != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gL.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++)
    {
      var sp = tmp[i] as IMyTextSurfaceProvider; if (sp != null) { lcds.Add(sp.GetSurface(0)); continue; }
      var tp = tmp[i] as IMyTextPanel; if (tp != null) lcds.Add(tp as IMyTextSurface);
    }
  }

  var gLed = GTS.GetBlockGroupWithName(LEDS_GROUP);
  if (gLed != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gLed.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var l = tmp[i] as IMyLightingBlock; if (l != null) leds.Add(l); }
  }

  var gSndN = GTS.GetBlockGroupWithName(SOUND_NORMAL_GROUP);
  if (gSndN != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gSndN.GetBlocks(tmp, b => SameConstruct(b) && MatchesSas(b));
    for (int i = 0; i < tmp.Count; i++) { var s = tmp[i] as IMySoundBlock; if (s != null) sndNormal.Add(s); }
  }

  var gAC = GTS.GetBlockGroupWithName(AUTOCLOSE_DOORS_GROUP);
  if (gAC != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gAC.GetBlocks(tmp, b => SameConstruct(b));
    for (int i = 0; i < tmp.Count; i++) { var d = tmp[i] as IMyDoor; if (d != null) doorsAuto.Add(d); }
  }

  var gOL = GTS.GetBlockGroupWithName(OVERVIEW_LCD_GROUP);
  if (gOL != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gOL.GetBlocks(tmp, b => SameConstruct(b));
    for (int i = 0; i < tmp.Count; i++)
    {
      var sp = tmp[i] as IMyTextSurfaceProvider; if (sp != null) { lcdOverview.Add(sp.GetSurface(0)); continue; }
      var tp = tmp[i] as IMyTextPanel; if (tp != null) lcdOverview.Add(tp as IMyTextSurface);
    }
  }

  var gLedE = GTS.GetBlockGroupWithName(EMERGENCY_LEDS_GROUP);
  if (gLedE != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gLedE.GetBlocks(tmp, b => SameConstruct(b));
    for (int i = 0; i < tmp.Count; i++) { var l = tmp[i] as IMyLightingBlock; if (l != null) ledsEmergency.Add(l); }
  }


  for (int i = 0; i < ledsEmergency.Count; i++) {
    var l = ledsEmergency[i];
    if (l == null) continue;
    if (!emergencyBaseIntensity.ContainsKey(l.EntityId)) {
      emergencyBaseIntensity[l.EntityId] = l.Intensity;
    }
  }

  var gSndA = GTS.GetBlockGroupWithName(SOUND_ALARM_GROUP);
  if (gSndA != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gSndA.GetBlocks(tmp, b => SameConstruct(b));
    for (int i = 0; i < tmp.Count; i++) { var s = tmp[i] as IMySoundBlock; if (s != null) sndAlarm.Add(s); }
  }

  var tmpV = new List<IMyAirVent>(); GTS.GetBlocksOfType(tmpV, v => SameConstruct(v));
  for (int i = 0; i < tmpV.Count; i++) allVents.Add(tmpV[i]);

  var tmpL = new List<IMyLightingBlock>(); GTS.GetBlocksOfType(tmpL, l => SameConstruct(l));
  for (int i = 0; i < tmpL.Count; i++)
  {
    var rm = ExtractRoomTag(tmpL[i].CustomName); if (rm != null) roomLights.Add(tmpL[i]);
  }

  for (int i = 0; i < allVents.Count; i++)
  {
    var v = allVents[i];
    var rm = ExtractRoomTag(v.CustomName); if (rm == null) continue;
    List<IMyAirVent> list; if (!roomToVents.TryGetValue(rm, out list)) { list = new List<IMyAirVent>(); roomToVents[rm] = list; }
    list.Add(v);

    IMyAirVent cur; if (!roomPrimaryVent.TryGetValue(rm, out cur)) roomPrimaryVent[rm] = v;
    else if (IsProbe(v) && !IsProbe(cur)) roomPrimaryVent[rm] = v;

    if (IsProbe(v)) { v.Enabled = true; v.Depressurize = false; }
  }

  for (int i = 0; i < roomLights.Count; i++)
  {
    var rm = ExtractRoomTag(roomLights[i].CustomName); if (rm == null) continue;
    List<IMyLightingBlock> list; if (!roomToLights.TryGetValue(rm, out list)) { list = new List<IMyLightingBlock>(); roomToLights[rm] = list; }
    list.Add(roomLights[i]);
  }

  if (vents.Count > 0)
  {
    bool anyDep = false; for (int i = 0; i < vents.Count; i++) if (vents[i].Depressurize) { anyDep = true; break; }
    envTarget = anyDep ? EnvTarget.Depressurized : EnvTarget.Pressurized;
  }
  else envTarget = EnvTarget.Unknown;

  BuildContainIndexes();

  var gRoomLCD = GTS.GetBlockGroupWithName(ROOM_LCD_GROUP);
  if (gRoomLCD != null)
  {
    var tmp = new List<IMyTerminalBlock>();
    gRoomLCD.GetBlocks(tmp, b => SameConstruct(b));
    for (int i = 0; i < tmp.Count; i++)
    {
      string rm = ExtractRoomTag(tmp[i].CustomName); if (string.IsNullOrEmpty(rm)) continue;
      IMyTextSurface surf = null;
      var sp = tmp[i] as IMyTextSurfaceProvider; if (sp != null) surf = sp.GetSurface(0);
      var tp = tmp[i] as IMyTextPanel; if (tp != null) surf = tp as IMyTextSurface;
      if (surf == null) continue;
      List<IMyTextSurface> ls; if (!roomLCDs.TryGetValue(rm, out ls)) { ls = new List<IMyTextSurface>(); roomLCDs[rm] = ls; }
      ls.Add(surf);
    }
  }
  allProbeVentIds.Clear();

  var gProbeAll = GTS.GetBlockGroupWithName(PROBE_VENTS_GROUP);
  if (gProbeAll != null) {
    var tmpProbe = new List<IMyTerminalBlock>();
    gProbeAll.GetBlocks(tmpProbe, b => SameConstruct(b));
    for (int i=0; i<tmpProbe.Count; i++){
      var v = tmpProbe[i] as IMyAirVent;
      if (v != null) allProbeVentIds.Add(v.EntityId);
    }
  }

  for (int i=0; i<allVents.Count; i++){
    var v = allVents[i]; if (v==null) continue;
    var nm = v.CustomName ?? "";
    if (nm.IndexOf("[PROBE]", StringComparison.OrdinalIgnoreCase) >= 0 ||
        nm.IndexOf("SAS_PROBE", StringComparison.OrdinalIgnoreCase) >= 0 ||
        nm.IndexOf("PROBE", StringComparison.OrdinalIgnoreCase) >= 0)
      allProbeVentIds.Add(v.EntityId);
  }
}

void BuildContainIndexes(){
  containVents.Clear();
  containDoors.Clear();
  containLights.Clear();
  containSounds.Clear();

  var groups = new List<IMyBlockGroup>();
  GTS.GetBlockGroups(groups);

  for (int i = 0; i < groups.Count; i++){
    var g = groups[i]; if (g == null) continue;

    var name = (g.Name ?? "").Trim();
    if (!name.StartsWith(CONTAIN_GROUP_PREFIX, System.StringComparison.OrdinalIgnoreCase)) continue;

    string key = NormalizeKey(name.Substring(CONTAIN_GROUP_PREFIX.Length));
    if (string.IsNullOrEmpty(key)) continue;

    var blocks = new List<IMyTerminalBlock>();
    g.GetBlocks(blocks, b => b.IsSameConstructAs(Me));

    for (int k = 0; k < blocks.Count; k++){
      var b = blocks[k];
      if (b is IMyAirVent){
        List<IMyAirVent> list;
        if (!containVents.TryGetValue(key, out list)) { list = new List<IMyAirVent>(); containVents[key] = list; }
        list.Add(b as IMyAirVent);
      }
      else if (b is IMyDoor){
        List<IMyDoor> list;
        if (!containDoors.TryGetValue(key, out list)) { list = new List<IMyDoor>(); containDoors[key] = list; }
        list.Add(b as IMyDoor);
      }
      else if (b is IMyLightingBlock){
        List<IMyLightingBlock> list;
        if (!containLights.TryGetValue(key, out list)) { list = new List<IMyLightingBlock>(); containLights[key] = list; }
        list.Add(b as IMyLightingBlock);
      }
      else if (b is IMySoundBlock){
        List<IMySoundBlock> list;
        if (!containSounds.TryGetValue(key, out list)) { list = new List<IMySoundBlock>(); containSounds[key] = list; }
        list.Add(b as IMySoundBlock);
      }
    }
  }
}

void ProcessManualPendingContain(){
  if (manualContainApplyAt.Count == 0) return;

  var keys = new List<string>(manualContainApplyAt.Keys);
  for (int i = 0; i < keys.Count; i++){
    string key = keys[i];
    double at;
    if (!manualContainApplyAt.TryGetValue(key, out at)) continue;

    if (Now >= at){
      bool wantOn = false; manualContainTargetOn.TryGetValue(key, out wantOn);

      ToggleContain(key, wantOn);

      manualDesired[key] = wantOn;

      manualContainApplyAt.Remove(key);
      manualContainTargetOn.Remove(key);
      Echo($"[PROTO] {(wantOn ? "Confinement" : "Levée")} appliqué sur '{key}'.");
    }
  }
}

string ExtractRoomTag(string name){
  if (string.IsNullOrEmpty(name)) return null;
  int i = name.IndexOf(ROOM_TAG_PREFIX); if (i<0) return null;
  int j = name.IndexOf(']', i+ROOM_TAG_PREFIX.Length); if (j<0) return null;
  var rm = name.Substring(i+ROOM_TAG_PREFIX.Length, j-(i+ROOM_TAG_PREFIX.Length)).Trim();
  return string.IsNullOrEmpty(rm) ? null : rm;
}

bool IsProbe(IMyAirVent v){ return IsProbeGlobal(v); }
string NormalizeKey(string s){ return (s??"").Trim().ToUpperInvariant(); }
bool IsEmergencyActive(){ return mode==Mode.EmergencyPhase1 || mode==Mode.EmergencyPhase2; }

bool VentIsContained(IMyAirVent v){
  if (v == null) return false;

  foreach (var key in containActive){
    List<IMyAirVent> list;
    if (containVents.TryGetValue(key, out list) && list != null){
      for (int i=0;i<list.Count;i++){
        if (list[i] == v) return true;
      }
    }
  }

  string rm = ExtractRoomTag(v.CustomName);
  if (!string.IsNullOrEmpty(rm)){
    string rKey = NormalizeKey(rm);
    if (containActive.Contains(rKey) || autoContainedRooms.Contains(rKey)){
      if (IsProbeGlobal(v) || IsProbe(v)) return false;
      return true;
    }
  }

  return false;
}

bool RoomIsContained(string room){ return containActive.Contains(NormalizeKey(room)); }

string ParseContainKey(string raw){
  if (string.IsNullOrEmpty(raw)) return null; raw = raw.Trim();
  int sp = raw.IndexOf(' '); if (sp>=0) return NormalizeKey(raw.Substring(sp+1));
  int us = raw.IndexOf('_'); if (us>=0) return NormalizeKey(raw.Substring(us+1));
  return null;
}

void PlayOneShot(List<IMySoundBlock> list, string fallback = null){
  if (string.IsNullOrEmpty(fallback)) fallback = DEFAULT_ROOM_ALERT_SOUND;
  if (list == null) return;
  for (int i=0;i<list.Count;i++){
    var s = list[i]; if (s==null || !s.IsFunctional) continue;
    PrepareSoundBlock(s, fallback, 5f);
    s.Stop(); s.Play();
  }
}

void SetRoomNonProbeVentsPressurize(string room, bool on){
  List<IMyAirVent> vs;
  if (!roomToVents.TryGetValue(room, out vs) || vs==null) return;
  for (int i=0;i<vs.Count;i++){
    var v = vs[i]; if (v==null || IsProbe(v)) continue;
    v.Enabled = on;
    if (on) v.Depressurize = false;
  }
}

void StartRoomPressuTest(string room){
  string key = NormalizeKey(room);
  SetRoomNonProbeVentsPressurize(room, true);
  roomTestUntil[key] = Now + ROOM_TEST_DURATION;
  roomNextTestAt[key] = Now + ROOM_RETEST_PERIOD;
}

bool IsRoomTestActive(string room){
  string key = NormalizeKey(room);
  double until;
  return roomTestUntil.TryGetValue(key, out until) && Now < until;
}

void EndRoomPressuTest(string room, float probeO2){
  string key = NormalizeKey(room);
  roomTestUntil.Remove(key);
  if (probeO2 <= 0.001f){
    SetRoomNonProbeVentsPressurize(room, false);
  }
}

void BeginPreLock(bool toPressurize){
  SetEnabled(doorsIN,true); SetEnabled(doorsOUT,true);
  CloseAllDoors();
  activeDoor = null; phaseGate=false; openedThisPhase=false;
  modeStart = Now;
  mode = toPressurize ? Mode.PreLockPressurize : Mode.PreLockDepressurize;
}

void MaintainInterlocks(){
  {
    if (globalSecure){
      AbortSasDoorActionsAndClose();
      ForceCloseAndHardLock(doorsIN);
      ForceCloseAndHardLock(doorsOUT);
      ForceNonProbeVentsOff();
      return;
    }

    string gsrc; EmergencyCause gcause;
    bool anyGlob = ComputeGlobalEmergency(out gsrc, out gcause);
    bool isLocalEmerg = IsEmergencyActive();

    if (anyGlob && !isLocalEmerg){
      AbortSasDoorActionsAndClose();
      ForceCloseAndHardLock(doorsIN);
      ForceCloseAndHardLock(doorsOUT);
      ForceNonProbeVentsOff();
      return;
    }
  }

  if (mode == Mode.EmergencyPhase1){
    ForceAllNonProbeVentsOffGlobal();
    SetEnabled(doorsIN,  true);
    SetEnabled(doorsOUT, true);
    return;
  }

  if (mode == Mode.EmergencyPhase2){
    ForceAllNonProbeVentsOffGlobal();
    CloseAllDoors();
    if (AllClosed(doorsIN) && AllClosed(doorsOUT)){
      SetEnabled(doorsIN,  false);
      SetEnabled(doorsOUT, false);
    } else {
      SetEnabled(doorsIN,  true);
      SetEnabled(doorsOUT, true);
    }
    return;
  }

  if (mode == Mode.PreLockPressurize || mode == Mode.PreLockDepressurize){
    CloseAllDoors();
    SetEnabled(doorsIN,  true);
    SetEnabled(doorsOUT, true);
    return;
  }

  if (mode == Mode.Pressurizing || mode == Mode.Depressurizing){
    CloseAllDoors();
    SetEnabled(doorsIN,  false);
    SetEnabled(doorsOUT, false);
    return;
  }

  if (mode == Mode.AfterCyclePressu || mode == Mode.AfterCycleDepressu){
    CloseAllDoors();
    SetEnabled(doorsIN,  false);
    SetEnabled(doorsOUT, false);
    return;
  }

  if (mode == Mode.HoldOpenIn){
    if (activeDoor != null) activeDoor.Enabled = true;
    SetEnabled(doorsOUT, false);
    return;
  }

  if (mode == Mode.HoldOpenOut){
    if (activeDoor != null) activeDoor.Enabled = true;
    SetEnabled(doorsIN, false);
    return;
  }

  if (mode == Mode.ClosingIn){
    if (activeDoor != null) activeDoor.Enabled = true;
    SetEnabled(doorsOUT, false);
    return;
  }

  if (mode == Mode.ClosingOut){
    if (activeDoor != null) activeDoor.Enabled = true;
    SetEnabled(doorsIN, false);
    return;
  }

  bool anyInOpen  = IsAnyOpen(doorsIN);
  bool anyOutOpen = IsAnyOpen(doorsOUT);

  SetEnabled(doorsOUT, !anyInOpen);
  SetEnabled(doorsIN,  !anyOutOpen);

  if (envTarget == EnvTarget.Pressurized)        SetEnabled(doorsOUT, false);
  else if (envTarget == EnvTarget.Depressurized) SetEnabled(doorsIN,  false);
}

void BeginEmergency(EmergencyCause cause){
  emergencyCause = cause;

  backupAllVentsEnabled.Clear();
  for (int i=0;i<allVents.Count;i++){
    var v = allVents[i]; if (IsProbe(v)) continue;
    backupAllVentsEnabled[v.EntityId] = v.Enabled;
  }

  activeDoor = null; phaseGate=false; openedThisPhase=false; emergMidChecked=false;

  ForceAllNonProbeVentsOffGlobal();
  SetEnabled(doorsIN,true); SetEnabled(doorsOUT,true);

  normalSoundOn=false; alarmSoundOn=false;
  normalLastKick = Now - NORMAL_KICK_PERIOD;
  alarmLastKick  = Now - ALARM_KICK_PERIOD;

  modeStart = Now;
  mode = Mode.EmergencyPhase1;
}

void RunStateMachine(){
  if ((globalSecure || IsRemoteAutoEmergency()) && !IsEmergencyActive()) return;
  if (mode == Mode.PreLockPressurize)
  {
    if (AllClosed(doorsIN) && AllClosed(doorsOUT))
    {
      SetEnabled(doorsIN, false); SetEnabled(doorsOUT, false);
      for (int i = 0; i < vents.Count; i++) { vents[i].Enabled = true; vents[i].Depressurize = false; }
      phaseGate = true; openedThisPhase = false; modeStart = Now; mode = Mode.Pressurizing;
    }
    else CloseAllDoors();

  }
  else if (mode == Mode.PreLockDepressurize)
  {
    if (AllClosed(doorsIN) && AllClosed(doorsOUT))
    {
      SetEnabled(doorsIN, false); SetEnabled(doorsOUT, false);
      for (int i = 0; i < vents.Count; i++) { vents[i].Enabled = true; vents[i].Depressurize = true; }
      phaseGate = true; openedThisPhase = false; modeStart = Now; mode = Mode.Depressurizing;
    }
    else CloseAllDoors();

  }
  else if (mode == Mode.Pressurizing)
  {
    for (int i = 0; i < vents.Count; i++) { vents[i].Enabled = true; vents[i].Depressurize = false; }
    CloseAllDoors(); SetEnabled(doorsIN, false); SetEnabled(doorsOUT, false);
    if (Elapsed(modeStart) >= CYCLE_SECONDS)
    {
      phaseGate = false; envTarget = EnvTarget.Pressurized;
      mode = Mode.AfterCyclePressu; modeStart = Now;
    }

  }
  else if (mode == Mode.Depressurizing)
  {
    for (int i = 0; i < vents.Count; i++) { vents[i].Enabled = true; vents[i].Depressurize = true; }
    CloseAllDoors(); SetEnabled(doorsIN, false); SetEnabled(doorsOUT, false);
    if (Elapsed(modeStart) >= CYCLE_SECONDS)
    {
      phaseGate = false; envTarget = EnvTarget.Depressurized;
      mode = Mode.AfterCycleDepressu; modeStart = Now;
    }

  }
  else if (mode == Mode.AfterCyclePressu)
  {
    if (!openedThisPhase) { openedThisPhase = true; BeginHoldOpen(true); }

  }
  else if (mode == Mode.AfterCycleDepressu)
  {
    if (!openedThisPhase) { openedThisPhase = true; BeginHoldOpen(false); }

  }
  else if (mode == Mode.HoldOpenIn)
  {
    if (activeDoor != null) activeDoor.OpenDoor();
    if (Elapsed(modeStart) >= DOOR_HOLD_SECONDS) BeginClosing(true);

  }
  else if (mode == Mode.HoldOpenOut)
  {
    if (activeDoor != null) activeDoor.OpenDoor();
    if (Elapsed(modeStart) >= DOOR_HOLD_SECONDS) BeginClosing(false);

  }
  else if (mode == Mode.ClosingIn)
  {
    if (activeDoor == null || activeDoor.Status == DoorStatus.Closed)
    {
      activeDoor = null; mode = Mode.Idle; EnableAllDoors(); MaintainInterlocks();
      phaseGate = false; openedThisPhase = false;
    }
    else activeDoor.CloseDoor();

  }
  else if (mode == Mode.ClosingOut)
  {
    if (activeDoor == null || activeDoor.Status == DoorStatus.Closed)
    {
      activeDoor = null; mode = Mode.Idle; EnableAllDoors(); MaintainInterlocks();
      phaseGate = false; openedThisPhase = false;
    }
    else activeDoor.CloseDoor();

  }
  else if (mode == Mode.EmergencyPhase1)
  {
    if (Elapsed(modeStart) >= EMERG_P1_SECONDS)
    {
      emergMidChecked = false; mode = Mode.EmergencyPhase2; modeStart = Now;
      CloseAllDoors(); SetEnabled(doorsIN, true); SetEnabled(doorsOUT, true);
    }

  }
  else if (mode == Mode.EmergencyPhase2)
  {
    double t = Elapsed(modeStart);
    if (!emergMidChecked && t >= EMERG_P2_SECONDS * 0.5)
    {
      emergMidChecked = true;
      if (IsAnyOpen(doorsIN) || IsAnyOpen(doorsOUT)) { BeginEmergency(emergencyCause); return; }
    }
  if (t >= EMERG_P2_SECONDS)
  {
  
    RestoreNonProbeVentsToBackup();

    if (AllClosed(doorsIN) && AllClosed(doorsOUT))
    {
      if (emergencyCause == EmergencyCause.DoorDamage && IsAnyDoorSeverelyDamaged()) {
        BeginEmergency(EmergencyCause.DoorDamage); return;
      }

    
      EnvTarget desired = envTarget;
      if (emergencyCause == EmergencyCause.O2Leak) desired = EnvTarget.Depressurized;
      if (desired == EnvTarget.Unknown)
        desired = IsDepressurized() ? EnvTarget.Depressurized : EnvTarget.Pressurized;

      ApplySasTarget(desired);
      return;
    }
    else BeginEmergency(emergencyCause);
  }
  }
}

void MonitorSasCompromise(){

  string _src; EmergencyCause _c;
  bool anyGlob = ComputeGlobalEmergency(out _src, out _c);
  if (anyGlob && !IsEmergencyActive()) return;


  if (IsAnyDoorSeverelyDamaged())
  {
    if (!IsEmergencyActive()) BeginEmergency(EmergencyCause.DoorDamage);
    else emergencyCause = EmergencyCause.DoorDamage;
    return;
  }


  if (envTarget == EnvTarget.Pressurized &&
      (mode==Mode.Idle || mode==Mode.AfterCyclePressu || mode==Mode.HoldOpenIn || mode==Mode.ClosingIn)){
    float o2 = ReadO2();
    if (o2 < LEAK_O2_THRESHOLD){
      if (!IsEmergencyActive()) BeginEmergency(EmergencyCause.O2Leak);
      else if (emergencyCause != EmergencyCause.DoorDamage) emergencyCause = EmergencyCause.O2Leak;
      return;
    }
  }
}

void MonitorRoomLeaks(){
  var rooms = new List<string>(roomToVents.Keys);

  for (int i = 0; i < rooms.Count; i++){
    string room = rooms[i];
    string key  = NormalizeKey(room);

  
    double applyAt;
    if (manualContainApplyAt.TryGetValue(key, out applyAt) && Now < applyAt){
      continue;
    }

  
    IMyAirVent pv = GetRoomProbeVent(room);
    if (pv == null) continue;

    float o2 = pv.GetOxygenLevel();

  
    float prevO2 = 1f;
    if (!roomLastO2.TryGetValue(key, out prevO2)) prevO2 = 1f;
    roomLastO2[key] = o2;

    bool contained = containActive.Contains(key);
    bool isAuto    = autoContainedRooms.Contains(key);

  
    double suspendUntil = 0;
    bool suspended = containTempDoorUnlockUntil.TryGetValue(key, out suspendUntil) && Now < suspendUntil;
    if (suspended) continue;

  
    double testUntil = 0;
    bool hadTestEntry = roomTestUntil.TryGetValue(key, out testUntil);
    bool testing   = hadTestEntry && (Now < testUntil);
    bool justEnded = hadTestEntry && !testing;

  
    if (justEnded){
      EndRoomPressuTest(room, o2); 
      roomTestUntil.Remove(key);
    }

  
    double nextAllowed = -1;
    bool hasNext = roomNextTestAt.TryGetValue(key, out nextAllowed);
    bool canStartNow = !testing && (!hasNext || Now >= nextAllowed);

  
    if (!contained){
    
      if (o2 >= 0f && o2 < ROOM_TEST_TRIGGER && canStartNow){
        StartRoomPressuTest(room);
        continue;
      }
    
      if (o2 >= 0f && o2 < ROOM_DROP_RETEST_TRIGGER && prevO2 > 0.5f && !testing){
        StartRoomPressuTest(room);
        continue;
      }
    
      if (justEnded && o2 < ROOM_LEAK_THRESHOLD){
        ToggleContain(key, true);  
        autoContainedRooms.Add(key);
        continue;
      }
      continue;
    }

  
  
    if (!isAuto){
      containPendingReleaseAt.Remove(key);
      continue;
    }

  
    if (canStartNow){
      StartRoomPressuTest(room);
      continue;
    }

  
    if (justEnded){
      if (o2 >= ROOM_RELEASE_O2){
        if (!containPendingReleaseAt.ContainsKey(key))
          containPendingReleaseAt[key] = Now + RELEASE_DELAY_ON_RECOVER;
      } else {
        containPendingReleaseAt.Remove(key);
      }
    }

  
    double at = 0;
    if (containPendingReleaseAt.TryGetValue(key, out at) && Now >= at){
      containPendingReleaseAt.Remove(key);

    
      bool wantManualAfter = false; manualDesired.TryGetValue(key, out wantManualAfter);
      if (wantManualAfter){
      
        autoContainedRooms.Remove(key);
        Echo($"[AUTO] '{room}' repasse en CONFINEMENT MANUEL (intention utilisateur).");
      } else {
      
        ToggleContain(key, false, true); 
        autoContainedRooms.Remove(key);
      }
    }
  }
}

bool IsAnyDoorSeverelyDamaged(){
  for (int i=0;i<doorsIN.Count;i++) if (IsDoorBad(doorsIN[i])) return true;
  for (int i=0;i<doorsOUT.Count;i++) if (IsDoorBad(doorsOUT[i])) return true;
  return false;
}
bool IsDoorBad(IMyDoor d){
  if (d == null) return false;
  if (!d.IsFunctional) return true;
  var slim = d.CubeGrid.GetCubeBlock(d.Position);
  if (slim == null) return false;
  float maxInt = slim.MaxIntegrity; if (maxInt <= 0f) return false;
  float hp = slim.BuildIntegrity / maxInt;
  return hp < DOOR_MIN_HEALTH;
}

void ToggleContain(string key, bool? forceOn, bool allowAutoOff = false){
  key = NormalizeKey(key);
  bool isActive = containActive.Contains(key);
  bool wantOn   = forceOn.HasValue ? forceOn.Value : !isActive;


  if (!wantOn && isActive && autoContainedRooms.Contains(key) && !allowAutoOff){
    Echo("Confinement AUTO '"+key+"' : levée manuelle interdite. Utilisez 'suspend "+key+"' pour une ouverture temporaire.");
    return;
  }

  if (!wantOn && isActive && IsEmergencyActive() && !allowAutoOff){
    Echo("Confinement '"+key+"' non levé : urgence active.");
    return;
  }

  if (wantOn){
  
    containActive.Add(key);

  
    List<IMyAirVent> vs;
    if (containVents.TryGetValue(key, out vs) && vs != null){
      for (int i=0;i<vs.Count;i++){
        var v = vs[i]; if (v==null) continue;
        if (IsProbe(v)){ v.Enabled = true; v.Depressurize = false; }
        else v.Enabled = false;
      }
    }

  
    List<IMyDoor> ds;
    if (containDoors.TryGetValue(key, out ds) && ds != null){
      for (int i=0;i<ds.Count;i++){
        var d = ds[i]; if (d==null) continue;
        d.Enabled = true;
        d.CloseDoor();
      }
    }

  
    List<IMySoundBlock> ss;
    if (containSounds.TryGetValue(key, out ss) && ss != null){
      PlayOneShot(ss);
    }

  
    List<IMyLightingBlock> ls;
    if (containLights.TryGetValue(key, out ls) && ls != null){
      SetLedsBlinkOnGroup(ls, Red(), 0.5f, 60f);
    }

  
    containTempDoorUnlockUntil.Remove(key);
    containPendingReleaseAt.Remove(key);
    roomTestUntil.Remove(key);
  }
  else {
  
    containActive.Remove(key);

  
    List<IMyDoor> ds;
    if (containDoors.TryGetValue(key, out ds) && ds != null){
      for (int i=0;i<ds.Count;i++){
        var d = ds[i]; if (d==null) continue;
        d.Enabled = true;
      }
    }

  
    List<IMyLightingBlock> ls;
    if (containLights.TryGetValue(key, out ls) && ls != null){
      if (!IsEmergencyActive()) SetLedsOffGroup(ls);
    }

  
  
  
    if (!IsEmergencyActive()){
      RestoreContainZoneNonProbeVents(key, true);
    }

  
    containTempDoorUnlockUntil.Remove(key);
    containPendingReleaseAt.Remove(key);
    roomTestUntil.Remove(key);
    autoContainedRooms.Remove(key);
  }
}

bool ManualRequestContain(string keyRaw, bool wantOn){
  string key = NormalizeKey(keyRaw);


  if (IsEmergencyActive()){
    Echo($"[PROTO] Interdit pendant une URGENCE pour '{keyRaw}'.");
    return false;
  }


  double at;
  if (manualContainApplyAt.TryGetValue(key, out at) && Now < at){
    double remain = System.Math.Max(0.0, at - Now);
    Echo($"[PROTO] Déjà en cours pour '{keyRaw}' ({remain:0.0}s).");
    return false;
  }


  if (!wantOn && autoContainedRooms.Contains(key)){
    Echo($"[PROTO] Levée MANUELLE interdite (auto-confinement) pour '{keyRaw}'.");
    return false;
  }


  bool isActive = containActive.Contains(key);
  if (wantOn == isActive){
    Echo($"[PROTO] Aucun changement pour '{keyRaw}' (état déjà atteint).");
    return false;
  }


  manualDesired[key] = wantOn;


  manualContainTargetOn[key] = wantOn;
  manualContainApplyAt[key]  = Now + MANUAL_CONTAIN_PROTO_SECONDS;
  Echo($"[PROTO] {(wantOn ? "Activation" : "Levée")} de '{keyRaw}' dans {MANUAL_CONTAIN_PROTO_SECONDS:0}s.");
  return true;
}

bool SuspendContain(string room){
  string key = NormalizeKey(room);


  double at;
  if (manualContainApplyAt.TryGetValue(key, out at) && Now < at){
    Echo($"[SUSPEND] Protocole en cours sur '{room}' — suspension ignorée.");
    return false;
  }

  if (!containActive.Contains(key)) {
    Echo("Aucun confinement actif sur '"+room+"' — suspension ignorée.");
    return false;
  }

  containTempDoorUnlockUntil[key] = Now + DOOR_TEMP_UNLOCK_SECONDS;

  List<IMyDoor> ds;
  if (containDoors.TryGetValue(key, out ds) && ds != null){
    for (int i=0;i<ds.Count;i++){ var d = ds[i]; if (d==null) continue; d.Enabled = true; d.OpenDoor(); }
  }
  return true;
}

IMyAirVent GetRoomProbeVent(string room){
  IMyAirVent pv = null;
  if (!roomPrimaryVent.TryGetValue(room, out pv)) return null;
  return IsProbe(pv) ? pv : null;
}

float GetRoomProbeO2(string room){
  var pv = GetRoomProbeVent(room);
  return (pv == null) ? -1f : pv.GetOxygenLevel();
}

bool IsContainSuspended(string room)
{
  string key = NormalizeKey(room);
  double until;
  if (containTempDoorUnlockUntil.TryGetValue(key, out until))
  {
    return Now < until;
  }
  return false;
}
void BeginHoldOpen(bool isIn){
  if (phaseGate) return;
  var list = isIn ? doorsIN : doorsOUT;
  if (list.Count==0){ mode=Mode.Idle; return; }
  activeDoor = list[0]; activeDoor.Enabled=true; activeDoor.OpenDoor();
  if (isIn) SetEnabled(doorsOUT,false); else SetEnabled(doorsIN,false);
  modeStart=Now; mode = isIn ? Mode.HoldOpenIn : Mode.HoldOpenOut;
}
void BeginClosing(bool isIn){
  if (activeDoor==null){ mode=Mode.Idle; return; }
  activeDoor.Enabled=true; activeDoor.CloseDoor();
  modeStart=Now; mode = isIn ? Mode.ClosingIn : Mode.ClosingOut;
}

void UpdateSignals(){
  bool inEmergency = IsEmergencyActive();



  if ((globalSecure && !inEmergency) || IsRemoteAutoEmergency())
  {
    SetLedsBlinkOnGroup(leds, Red(), 0.5f, 60f);

  
    SoundGroupUpdate(sndNormal, false, SOUND_PRESSURE_CYCLE,
                     NORMAL_LOOP_PERIOD, NORMAL_KICK_PERIOD,
                     ref normalSoundOn, ref normalLastKick);

  
    ResetRoomLightsIfNoEmergency();
    return;
  }


  if (inEmergency)
  {
  
    SetLedsBlinkOnGroup(leds, Red(), 0.5f, 60f);

  
    foreach (var kv in roomToLights)
      SetLedsBlinkOnGroup(kv.Value, Red(), 0.5f, 60f);

  
    if (sndAlarm.Count > 0) {
      SoundGroupUpdate(sndNormal, false, SOUND_PRESSURE_CYCLE,
                       NORMAL_LOOP_PERIOD, NORMAL_KICK_PERIOD,
                       ref normalSoundOn, ref normalLastKick);

      SoundGroupUpdate(sndAlarm, true, SOUND_LOCAL_ALARM,
                       ALARM_LOOP_PERIOD, ALARM_KICK_PERIOD,
                       ref alarmSoundOn, ref alarmLastKick);
    } else {
      SoundGroupUpdate(sndNormal, true, SOUND_LOCAL_ALARM,
                       ALARM_LOOP_PERIOD, ALARM_KICK_PERIOD,
                       ref normalSoundOn, ref normalLastKick);
      alarmSoundOn = false;
    }
  
    return;
  }


  ResetRoomLightsIfNoEmergency();


  bool inPhase = (mode==Mode.PreLockPressurize || mode==Mode.PreLockDepressurize ||
                  mode==Mode.Pressurizing      || mode==Mode.Depressurizing);

  if (inPhase) {
    SetLedsBlinkOnGroup(leds, Yellow(), 0.6f, 50f);
  } else {
    if      (envTarget==EnvTarget.Pressurized)   SetLedsSolidOnGroup(leds, Green());
    else if (envTarget==EnvTarget.Depressurized) SetLedsSolidOnGroup(leds, Blue());
    else                                         SetLedsOffGroup(leds);
  }


  bool inCycle = (mode==Mode.Pressurizing || mode==Mode.Depressurizing);
  SoundGroupUpdate(sndNormal, inCycle, SOUND_PRESSURE_CYCLE,
                   NORMAL_LOOP_PERIOD, NORMAL_KICK_PERIOD,
                   ref normalSoundOn, ref normalLastKick);

  SoundGroupUpdate(sndAlarm, false, SOUND_LOCAL_ALARM,
                   ALARM_LOOP_PERIOD, ALARM_KICK_PERIOD,
                   ref alarmSoundOn, ref alarmLastKick);

}


VRageMath.Color Green(){ return new VRageMath.Color(0,255,0); }
VRageMath.Color Yellow(){ return new VRageMath.Color(255,220,0); }
VRageMath.Color Blue(){ return new VRageMath.Color(30,160,255); }
VRageMath.Color Red(){ return new VRageMath.Color(255,40,40); }

VRageMath.Color White(){ return new VRageMath.Color(255,255,255); }

void SetLedsSolidOnGroup(List<IMyLightingBlock> group, VRageMath.Color c){
  for (int i=0;i<group.Count;i++){ var l=group[i]; l.Enabled=true; l.Color=c; l.BlinkIntervalSeconds=0f; l.BlinkLength=50f; l.Intensity=2.0f; l.Radius=System.Math.Max(l.Radius,5f); }
}
void SetLedsBlinkOnGroup(List<IMyLightingBlock> group, VRageMath.Color c, float interval, float lengthPct){
  for (int i=0;i<group.Count;i++){ var l=group[i]; l.Enabled=true; l.Color=c; l.BlinkIntervalSeconds=interval; l.BlinkLength=lengthPct; l.Intensity=2.0f; l.Radius=System.Math.Max(l.Radius,5f); }
}
void SetLedsOffGroup(List<IMyLightingBlock> group){ for (int i=0;i<group.Count;i++) group[i].Enabled=false; }

void PrepareSoundBlock(IMySoundBlock s, string fallbackName, float loopPeriod){
  if (s == null) return;
  s.Enabled = true;
  if (!string.IsNullOrWhiteSpace(fallbackName) && s.SelectedSound != fallbackName) s.SelectedSound = fallbackName;
  s.LoopPeriod = loopPeriod;
  if (s.Volume < 0.1f) s.Volume = 1.0f;
}

void SoundGroupUpdate(List<IMySoundBlock> list, bool wantOn, string fallbackName,
                      float loopPeriod, double kickPeriod, ref bool onFlag, ref double lastKick){
  if (list == null || list.Count == 0) { onFlag = false; return; }

  if (wantOn){
    if (!onFlag){
      for (int i=0;i<list.Count;i++){
        var s=list[i]; if (s == null || !s.IsFunctional) continue;
        PrepareSoundBlock(s, fallbackName, loopPeriod);
        s.Stop(); s.Play();
      }
      onFlag=true; lastKick=Now;
    } else if (Now-lastKick>=kickPeriod){
      for (int i=0;i<list.Count;i++){
        var s=list[i]; if (s == null || !s.IsFunctional) continue;
        PrepareSoundBlock(s, fallbackName, loopPeriod);
        s.Stop(); s.Play();
      }
      lastKick=Now;
    }
  } else {
    if (onFlag){
      for (int i=0;i<list.Count;i++){
        var s=list[i]; if (s == null) continue;
        s.Stop();
      }
      onFlag=false;
    }
  }
}

bool AllClosed(List<IMyDoor> list){ for (int i=0;i<list.Count;i++) if (list[i].Status!=DoorStatus.Closed) return false; return true; }
bool IsAnyOpen(List<IMyDoor> list){ for (int i=0;i<list.Count;i++){ var st=list[i].Status; if (st==DoorStatus.Open || st==DoorStatus.Opening) return true; } return false; }
void CloseAllDoors(){ for (int i=0;i<doorsIN.Count;i++) doorsIN[i].CloseDoor(); for (int i=0;i<doorsOUT.Count;i++) doorsOUT[i].CloseDoor(); }
void EnableAllDoors(){ SetEnabled(doorsIN,true); SetEnabled(doorsOUT,true); }
void SetEnabled(List<IMyDoor> list, bool on){ for (int i=0;i<list.Count;i++) list[i].Enabled=on; }

void RestoreNonProbeVents(bool on){
  for (int i=0;i<allVents.Count;i++){
    var v=allVents[i]; if (IsProbe(v)){ v.Enabled=true; v.Depressurize=false; }
    else { if (VentIsContained(v)) v.Enabled=false; else v.Enabled=on; }
  }
}
void ForceNonProbeVentsOff()
{

  for (int i = 0; i < vents.Count; i++)
  {
    var v = vents[i]; if (v == null) continue;
    if (IsProbe(v)) { v.Enabled = true; v.Depressurize = false; }
    else v.Enabled = false;
  }



  for (int i = 0; i < probeVents.Count; i++)
  {
    var pv = probeVents[i]; if (pv == null) continue;
    pv.Enabled = true;
    pv.Depressurize = false;
  }
}
void RestoreNonProbeVentsToBackup(){
  for (int i = 0; i < allVents.Count; i++){
    var v = allVents[i]; if (v == null) continue;

  
    if (IsProbe(v)) { v.Enabled = true; v.Depressurize = false; continue; }

  
    if (VentIsContained(v)) { v.Enabled = false; continue; }

  
    v.Enabled = true;
    v.Depressurize = false;
  }
}

void EnforceContainDoorsClosedAndLocked(){
  foreach (var key in containActive){
    bool tempUnlock = false; double until;
    if (containTempDoorUnlockUntil.TryGetValue(key, out until)){
      if (Now < until) tempUnlock = true; else containTempDoorUnlockUntil.Remove(key);
    }

    List<IMyDoor> ds;
    if (!containDoors.TryGetValue(key, out ds) || ds == null) continue;

    for (int i=0;i<ds.Count;i++){
      var d = ds[i]; if (d==null) continue;

      if (tempUnlock){
        d.Enabled = true;
        d.OpenDoor();
      } else {
        if (d.Status != DoorStatus.Closed){ d.Enabled = true; d.CloseDoor(); }
        else d.Enabled = false;
      }
    }
  }
}

void ResetRoomLightsIfNoEmergency()
{
  foreach (var kv in roomToLights)
  {
    string room = kv.Key;
    var lights = kv.Value;
    if (!RoomIsContained(room))
    {
      SetLedsOffGroup(lights);
    }
  
  }
}

void AutoCloseDoors(){
  if (mode!=Mode.Idle){ openSince.Clear(); return; }
  TrackOpenTimes(doorsIN); TrackOpenTimes(doorsOUT); TrackOpenTimes(doorsAuto);
  for (int i=0;i<doorsIN.Count;i++)  MaybeAutoClose(doorsIN[i]);
  for (int i=0;i<doorsOUT.Count;i++) MaybeAutoClose(doorsOUT[i]);
  for (int i=0;i<doorsAuto.Count;i++)MaybeAutoClose(doorsAuto[i]);
}
void TrackOpenTimes(List<IMyDoor> list){
  for (int i=0;i<list.Count;i++){
    var d=list[i]; var id=d.EntityId; var st=d.Status;
    if (st==DoorStatus.Open || st==DoorStatus.Opening){ if (!openSince.ContainsKey(id)) openSince[id]=Now; }
    else { if (openSince.ContainsKey(id)) openSince.Remove(id); }
  }
}
void MaybeAutoClose(IMyDoor d){
  var id=d.EntityId;
  if (openSince.ContainsKey(id) && Elapsed(openSince[id])>=AUTOCLOSE_SECONDS){ d.CloseDoor(); openSince.Remove(id); }
}

float ReadO2(){

  if (probeVents != null && probeVents.Count > 0){
    float sum = 0f; int n = 0;
    for (int i = 0; i < probeVents.Count; i++){
      var v = probeVents[i]; if (v == null) continue;
      sum += v.GetOxygenLevel(); n++;
    }
    if (n > 0) return sum / n;
  }

  float sum2 = 0f; int n2 = 0;
  for (int j = 0; j < vents.Count; j++){
    var v2 = vents[j]; if (v2 == null) continue;
    sum2 += v2.GetOxygenLevel(); n2++;
  }
  return (n2 > 0) ? (sum2 / n2) : 0f;
}

bool IsPressurized(){ return ReadO2() >= O2_PRESSURIZED; }
bool IsDepressurized(){ return ReadO2() <= O2_DEPRESSURIZED; }



void DrawLCD(){
  for (int i=0;i<lcds.Count;i++){
    var s = lcds[i];
    s.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    s.BackgroundColor = new VRageMath.Color(0,0,0,255);
    s.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

  
    string gSrc; EmergencyCause gCause;
    bool anyGlobalEmerg = ComputeGlobalEmergency(out gSrc, out gCause);
    bool localEmerg = IsEmergencyActive();
    bool remoteAutoEmerg = IsRemoteAutoEmergency();

    float o2 = ReadO2();
    string o2Str = System.Math.Min(100,(int)(o2*100)).ToString("00") + "%";

  
    double total = 0, remain = 0; float pBar = 0f; string phaseLine = null;
    if (localEmerg){
      if (mode == Mode.EmergencyPhase1){ total = EMERG_P1_SECONDS; phaseLine = "ÉTAT D'URGENCE — Sécurisation"; }
      else if (mode == Mode.EmergencyPhase2){ total = EMERG_P2_SECONDS; phaseLine = "ÉTAT D'URGENCE — Relance système"; }
      remain = System.Math.Max(0.0, total - Elapsed(modeStart));
      pBar = (float)System.Math.Min(1.0, Elapsed(modeStart)/System.Math.Max(0.001,total));
    } else if (mode == Mode.Pressurizing || mode == Mode.Depressurizing){
      total = CYCLE_SECONDS;
      remain = System.Math.Max(0.0, total - Elapsed(modeStart));
      pBar = (float)System.Math.Min(1.0, Elapsed(modeStart)/System.Math.Max(0.001,total));
      phaseLine = (mode==Mode.Pressurizing) ? "PRESSURISATION" : "DÉPRESSURISATION";
    } else if (mode == Mode.HoldOpenIn || mode == Mode.HoldOpenOut){
      total = DOOR_HOLD_SECONDS;
      remain = System.Math.Max(0.0, total - Elapsed(modeStart));
      pBar = (float)System.Math.Min(1.0, Elapsed(modeStart)/System.Math.Max(0.001,total));
      phaseLine = (mode==Mode.HoldOpenIn) ? "PORTE INTÉRIEURE OUVERTE" : "PORTE EXTÉRIEURE OUVERTE";
    }

  
    string inState  = GroupDoorStateLabel(doorsIN);
    string outState = GroupDoorStateLabel(doorsOUT);
    string interlocks = CurrentInterlockLabel(anyGlobalEmerg, remoteAutoEmerg, localEmerg);

  
    string env = EnvLabel(envTarget);

    var sb = new System.Text.StringBuilder(512);
    sb.AppendLine();
    sb.AppendLine("===== " + SAS_NAME + " =====");
    sb.AppendLine();

    if (globalSecure){
      sb.AppendLine("SÉCURITÉ : " + (secureApplied ? "ACTIVE" : "ARMÉE"));
    } else {
      sb.AppendLine("SÉCURITÉ : désactivée");
    }
    if (anyGlobalEmerg){
      sb.AppendLine("URGENCE : ACTIVE — Source: "+gSrc+" — Cause: "+CauseLabel(gCause));
    } else {
      sb.AppendLine("URGENCE : —");
    }
    if (remoteAutoEmerg && !localEmerg){
      sb.AppendLine("URGENCE : verrouillage");
    }

  
    if (!localEmerg && globalSecure && anyGlobalEmerg){
      sb.AppendLine("MODE : SÉCURITÉ");
    }

    sb.AppendLine("--------------------------------");

  
    bool secureRemote = (!localEmerg) && (globalSecure || anyGlobalEmerg);
    if (localEmerg){
      sb.AppendLine("URGENCE LOCALE — Cause : " + CauseLabel(emergencyCause));
      if (!string.IsNullOrEmpty(phaseLine)) sb.AppendLine(phaseLine);
      sb.AppendLine("Restant : " + remain.ToString("0.0") + " s");
      sb.AppendLine();
      sb.AppendLine(MakeBar(pBar, 32));
    }
    else if (secureRemote){
    
      sb.AppendLine("MODE : SÉCURITÉ");
      sb.AppendLine("Statut : " + env);
    }
    else {
      sb.AppendLine("Mode : " + ModeLabel(mode));
      sb.AppendLine("Statut : " + env);
      if (!string.IsNullOrEmpty(phaseLine)){
        sb.AppendLine(phaseLine + " — " + remain.ToString("0.0") + " s");
        sb.AppendLine();
        sb.AppendLine(MakeBar(pBar, 32));
      }
    }

    sb.AppendLine();
    sb.AppendLine("O2 interne : " + o2Str);

  
    sb.AppendLine();
    sb.AppendLine("Portes :  INT = " + inState + "   |   EXT = " + outState);

    s.WriteText(sb.ToString(), false);
  }
}

bool MatchesSasName(IMyTerminalBlock b, string sasName){
  if (b == null) return false;
  var tag = ExtractSasTag(b.CustomName ?? "");
  if (tag == null) return false;
  return string.Equals(NormalizeSasName(tag), NormalizeSasName(sasName), StringComparison.OrdinalIgnoreCase);
}

void GetSasSnapshot(string sasName, out string o2str, out string action){
  o2str = "--"; action = "—";


  var probe = new List<IMyAirVent>();
  var ventsLocal = new List<IMyAirVent>();

  var gProbe = GTS.GetBlockGroupWithName(PROBE_VENTS_GROUP);
  if (gProbe != null){
    var tmp = new List<IMyTerminalBlock>();
    gProbe.GetBlocks(tmp, b => SameConstruct(b) && MatchesSasName(b, sasName));
    for (int i=0;i<tmp.Count;i++){ var v = tmp[i] as IMyAirVent; if (v!=null) probe.Add(v); }
  }
  var gVents = GTS.GetBlockGroupWithName(VENTS_GROUP);
  if (gVents != null){
    var tmp = new List<IMyTerminalBlock>();
    gVents.GetBlocks(tmp, b => SameConstruct(b) && MatchesSasName(b, sasName));
    for (int i=0;i<tmp.Count;i++){ var v = tmp[i] as IMyAirVent; if (v!=null) ventsLocal.Add(v); }
  }

  float sum=0f; int n=0;
  if (probe.Count>0){
    for (int i=0;i<probe.Count;i++){ var v=probe[i]; if (v==null) continue; sum += v.GetOxygenLevel(); n++; }
    if (n>0) o2str = System.Math.Min(100,(int)((sum/n)*100)).ToString("00")+"%";
  } else if (ventsLocal.Count>0){
    for (int i=0;i<ventsLocal.Count;i++){ var v=ventsLocal[i]; if (v==null) continue; sum += v.GetOxygenLevel(); n++; }
    if (n>0) o2str = System.Math.Min(100,(int)((sum/n)*100)).ToString("00")+"%";
  }


  SasState S;
  if (sasStates.TryGetValue(NormalizeSasName(sasName), out S) && S != null){
    bool inEmerg = (S.mode==Mode.EmergencyPhase1 || S.mode==Mode.EmergencyPhase2);

  
    string _gSrc; EmergencyCause _gCause;
    bool anyGlobalEmerg = ComputeGlobalEmergency(out _gSrc, out _gCause);

    if (inEmerg){
      if      (S.emergencyCause==EmergencyCause.O2Leak)     action="FUITE O2";
      else if (S.emergencyCause==EmergencyCause.DoorDamage) action="PORTE ENDOMMAGEE";
      else if (S.emergencyCause==EmergencyCause.Manual)     action="URGENCE (MANUELLE)";
      else                                                  action="URGENCE";
    }
    else if (globalSecure || anyGlobalEmerg){
    
    
      action = "SÉCURITÉ";
    }
    else {
      if      (S.envTarget==EnvTarget.Pressurized)   action="PRESSURISER";
      else if (S.envTarget==EnvTarget.Depressurized) action="DEPRESSURISER";
      else                                           action="—";
    }
  } else {
  
    bool anyDep = false;
    for (int i=0;i<ventsLocal.Count;i++){ var v=ventsLocal[i]; if (v!=null && v.Depressurize){ anyDep=true; break; } }
    if (ventsLocal.Count>0) action = anyDep ? "DEPRESSURISER" : "PRESSURISER";
  }
}

void DrawOverviewLCD(){
  if (lcdOverview.Count == 0) return;

  const int NAME_W = 22;
  const int O2_W   = 4;


  string gSrc; EmergencyCause gCause;
  bool anyGlobalEmerg = ComputeGlobalEmergency(out gSrc, out gCause);
  string secureLine = globalSecure
    ? ("SÉCURITÉ : " + (secureApplied ? "ACTIVE (verrouillage)" : "ARMÉE"))
    : "SÉCURITÉ : désactivée";
  string emergLine  = anyGlobalEmerg
    ? ("URGENCE GLOBALE : ACTIVE — Source: "+gSrc+" — Cause: "+CauseLabel(gCause))
    : "URGENCE GLOBALE : —";


  var allSas = new List<string>(DiscoverAllSasNames());
  allSas.Sort(System.StringComparer.OrdinalIgnoreCase);
  var sasSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
  for (int i=0;i<allSas.Count;i++) sasSet.Add(NormalizeSasName(allSas[i]));


  var rooms = new List<string>(roomToVents.Keys);
  rooms.Sort(System.StringComparer.OrdinalIgnoreCase);
  var filteredRooms = new List<string>();
  for (int i=0;i<rooms.Count;i++){
    string rn = NormalizeSasName(rooms[i]);
    if (!sasSet.Contains(rn)) filteredRooms.Add(rooms[i]);
  }


  for (int i = 0; i < lcdOverview.Count; i++){
    var s = lcdOverview[i];
    s.ContentType     = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    s.BackgroundColor = new VRageMath.Color(0,0,0,255);
    s.Alignment       = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

    var sb = new System.Text.StringBuilder(2048);
    sb.AppendLine("-- Contrôle global --");
    sb.AppendLine(secureLine);
    sb.AppendLine(emergLine);
    sb.AppendLine("");

    sb.AppendLine(PadL("NOM", NAME_W) + " | " + PadL("%O2", O2_W) + " | ACTION");
    sb.AppendLine(new string('-', NAME_W) + "-+-" + new string('-', O2_W) + "-+---------------");

  
    for (int k=0;k<allSas.Count;k++){
      string name = allSas[k];
      string o2S, actS; GetSasSnapshot(name, out o2S, out actS);
      sb.AppendLine(PadL(NormalizeSasName(name), NAME_W) + " | " + PadR(o2S, O2_W) + " | " + actS);
    }

  
    for (int k=0;k<filteredRooms.Count;k++){
      string room = filteredRooms[k];
      string keyN = NormalizeKey(room);

    
      float o2 = 0f; int n = 0;
      IMyAirVent pv = null; roomPrimaryVent.TryGetValue(room, out pv);
      bool hasProbe = (pv != null && IsProbe(pv));
      if (hasProbe){
        o2 = pv.GetOxygenLevel();
      } else {
        List<IMyAirVent> vs;
        if (roomToVents.TryGetValue(room, out vs) && vs != null){
          for (int j=0;j<vs.Count;j++){
            var v = vs[j]; if (IsProbe(v)) continue;
            if (v.Enabled){ o2 += v.GetOxygenLevel(); n++; }
          }
        }
      }
      string o2str = (hasProbe || n>0) ? System.Math.Min(100,(int)(((hasProbe?o2:(o2/n))*100))).ToString("00")+"%" : "--";

      string action = "—";
      bool suspended = IsContainSuspended(room);
      bool contained = RoomIsContained(room);
      bool isAuto    = autoContainedRooms.Contains(keyN);
      float probeO2  = hasProbe ? o2 : -1f;

      double protoAt;
      bool hasProto = manualContainApplyAt.TryGetValue(keyN, out protoAt) && Now < protoAt;
      if (hasProto){
        double remain = System.Math.Max(0.0, protoAt - Now);
        bool tgtOn = false; manualContainTargetOn.TryGetValue(keyN, out tgtOn);
        action = tgtOn ? $"PROTO (activation {remain:0.0}s)" : $"PROTO (levée {remain:0.0}s)";
      }
      else if (suspended){
        action = "SUSPENDU (ouverture temp.)";
      }
      else if (contained){
        if (isAuto){
          bool leakByProbe = hasProbe && probeO2 >= 0f && probeO2 < ROOM_LEAK_THRESHOLD;
          double releaseAt;
          bool pending = containPendingReleaseAt.TryGetValue(keyN, out releaseAt) && Now < releaseAt;
          if      (leakByProbe) action = "AUTO CONF — fuite O2";
          else if (pending)     action = "AUTO CONF — O2 détecté";
          else                  action = "AUTO CONFINEMENT";
        } else {
          action = "CONFINEMENT";
        }
      } else {
        List<IMyAirVent> vs2;
        if (roomToVents.TryGetValue(room, out vs2) && vs2 != null){
          bool anyNonProbe=false, anyEnabled=false, anyDep=false;
          for (int j=0;j<vs2.Count;j++){
            var v = vs2[j]; if (IsProbe(v)) continue;
            anyNonProbe = true;
            if (v.Enabled) anyEnabled = true;
            if (v.Depressurize) anyDep = true;
          }
          if (anyNonProbe) action = !anyEnabled ? "VENTILATION COUPÉE" : (anyDep ? "DEPRESSURISER" : "PRESSURISER");
        }
      }

      sb.AppendLine(PadL(room, NAME_W) + " | " + PadR(o2str, O2_W) + " | " + action);
    }

    s.WriteText(sb.ToString(), false);
  }
}

void DrawRoomLCDs(){
  if (roomLCDs.Count == 0) return;

  foreach (var kv in roomLCDs){
    string room = kv.Key;
    var surfaces = kv.Value;

  
    float o2 = 0f; int n = 0;
    IMyAirVent pv = null; roomPrimaryVent.TryGetValue(room, out pv);
    bool hasProbe = (pv != null && IsProbe(pv));
    if (hasProbe) {
      o2 = pv.GetOxygenLevel();
    } else {
      List<IMyAirVent> vs; 
      if (roomToVents.TryGetValue(room, out vs) && vs != null){
        for (int j = 0; j < vs.Count; j++){
          var v = vs[j]; if (IsProbe(v)) continue;
          if (v.Enabled){ o2 += v.GetOxygenLevel(); n++; }
        }
        if (n > 0) o2 /= n;
      }
    }
    string o2str = (hasProbe || n > 0) ? System.Math.Min(100, (int)(o2 * 100)).ToString("00") + "%" : "--";

  
    string key = NormalizeKey(room);
    bool contained = RoomIsContained(room);
    bool isAuto    = autoContainedRooms.Contains(key);

  
    double unlockUntil = 0;
    bool hasTempUnlock = containTempDoorUnlockUntil.TryGetValue(key, out unlockUntil) && Now < unlockUntil;
    double unlockRemain = hasTempUnlock ? System.Math.Max(0.0, unlockUntil - Now) : 0.0;
    float unlockProg = hasTempUnlock ? (float)((DOOR_TEMP_UNLOCK_SECONDS - unlockRemain) / DOOR_TEMP_UNLOCK_SECONDS) : 0f;

  
    double testUntil = 0;
    bool testing = roomTestUntil.TryGetValue(key, out testUntil) && Now < testUntil;
    double testRemain = testing ? System.Math.Max(0.0, testUntil - Now) : 0.0;
    float testProg = testing ? (float)((ROOM_TEST_DURATION - testRemain) / ROOM_TEST_DURATION) : 0f;

  
    double releaseAt = 0;
    bool hasPendingRelease = containPendingReleaseAt.TryGetValue(key, out releaseAt) && Now < releaseAt;
    double releaseRemain = hasPendingRelease ? System.Math.Max(0.0, releaseAt - Now) : 0.0;
    float releaseProg = hasPendingRelease ? (float)((RELEASE_DELAY_ON_RECOVER - releaseRemain) / RELEASE_DELAY_ON_RECOVER) : 0f;

  
    double nextAllowed = 0, nextTestIn = 0;
    if (contained && isAuto && !testing && !hasTempUnlock && roomNextTestAt.TryGetValue(key, out nextAllowed)){
      if (Now < nextAllowed) nextTestIn = nextAllowed - Now;
    }

  
    bool leakByProbe = hasProbe && (o2 >= 0f && o2 < ROOM_LEAK_THRESHOLD);

  
    double protoAt2 = 0.0;
    bool hasProto2 = manualContainApplyAt.TryGetValue(key, out protoAt2) && Now < protoAt2;
    double protoRemain = hasProto2 ? System.Math.Max(0.0, protoAt2 - Now) : 0.0;
    float  protoProg   = hasProto2 ? (float)((MANUAL_CONTAIN_PROTO_SECONDS - protoRemain) / MANUAL_CONTAIN_PROTO_SECONDS) : 0f;
    bool   protoTargetOn = false; if (hasProto2) manualContainTargetOn.TryGetValue(key, out protoTargetOn);

  
    string action;
    if (hasProto2){
      action = protoTargetOn ? "Protocole d'activation en cours" : "Protocole de levée en cours";
    } else if (testing){
      action = "Test de pressurisation en cours";
    } else if (hasTempUnlock){
      action = "SUSPENDU (ouverture temporaire)";
    } else if (contained){
      if (isAuto){
        if (leakByProbe){
          action = "Auto-confinement — fuite O2";
        } else if (hasPendingRelease){
          action = "Auto-confinement — O2 détecté";
        } else {
          action = "Auto-confinement actif";
        }
      } else {
        action = IsEmergencyActive() ? "Confinement manuel (verrouillé)" : "Confinement manuel";
      }
    } else {
    
      bool anyNonProbe=false, anyEnabled=false, anyDep=false;
      List<IMyAirVent> vs2;
      if (roomToVents.TryGetValue(room, out vs2) && vs2 != null){
        for (int j = 0; j < vs2.Count; j++){
          var v = vs2[j]; if (IsProbe(v)) continue;
          anyNonProbe = true;
          if (v.Enabled) anyEnabled = true;
          if (v.Depressurize) anyDep = true;
        }
      }
      if (anyNonProbe) action = !anyEnabled ? "VENTILATION COUPÉE" : (anyDep ? "DÉPRESSURISER" : "PRESSURISER");
      else action = "EN SURVEILLANCE";
    }

  
    for (int i = 0; i < surfaces.Count; i++){
      var s = surfaces[i];
      s.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
      s.BackgroundColor = new VRageMath.Color(0,0,0,255);
      s.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

      var sb = new System.Text.StringBuilder();
      sb.AppendLine();
      sb.AppendLine("SECTEUR : " + room);
      sb.AppendLine();
      sb.AppendLine("Statut : " + action);
      sb.AppendLine("O2     : " + o2str);

      if (hasProto2){
        sb.AppendLine();
        sb.AppendLine("Application dans : " + protoRemain.ToString("0.0") + " s");
        sb.AppendLine(MakeBar(protoProg, 32));
      } else if (testing){
        sb.AppendLine();
        sb.AppendLine("Fin du test dans : " + testRemain.ToString("0.0") + " s");
        sb.AppendLine(MakeBar(testProg, 32));
      } else if (hasTempUnlock){
        sb.AppendLine();
        sb.AppendLine("Relock dans : " + unlockRemain.ToString("0.0") + " s");
        sb.AppendLine(MakeBar(unlockProg, 32));
      } else if (hasPendingRelease && isAuto){
        sb.AppendLine();
        sb.AppendLine("Levée automatique dans : " + releaseRemain.ToString("0.0") + " s");
        sb.AppendLine(MakeBar(releaseProg, 32));
      } else if (contained && isAuto && nextTestIn > 0){
        sb.AppendLine();
        sb.AppendLine("Prochain test dans : " + nextTestIn.ToString("0.0") + " s");
      }

      if (contained && !hasTempUnlock && !testing && !hasProto2){
        sb.AppendLine();
        sb.AppendLine(isAuto ? "Système : Isolement automatique" : "Système : Isolement manuel");
      } else {
        sb.AppendLine();
        sb.AppendLine("");
      }

      s.WriteText(sb.ToString(), false);
    }
  }
}

string PadL(string txt, int width){ if (txt==null) txt=""; if (txt.Length>width) return (width<=1)?txt.Substring(0,1):(txt.Substring(0,width-1)+"…"); return txt.PadRight(width,' '); }
string PadR(string txt, int width){ if (txt==null) txt=""; if (txt.Length>width) return (width<=1)?txt.Substring(0,1):(txt.Substring(0,width-1)+"…"); return txt.PadLeft(width,' '); }
string MakeBar(float p, int width){
  if (width<3) width=3;
  p = VRageMath.MathHelper.Clamp(p,0f,1f);
  int filled = (int)System.Math.Round(p*width);
  if (filled>width) filled=width;
  var sb = new System.Text.StringBuilder();
  sb.Append('[');
  for (int i=0;i<width;i++) sb.Append(i<filled ? '|' : ' ');
  sb.Append(']');
  sb.Append(' ').Append(((int)(p*100)).ToString("0")).Append('%');
  return sb.ToString();
}

void EchoStatus(){
  Echo("Mode: "+mode+" | Target: "+envTarget+" | Cause: "+emergencyCause);
  Echo("Rooms: "+roomToVents.Count+"");
  Echo("Contain zones: "+containVents.Count+" active="+containActive.Count);
  Echo("Sound: normal="+sndNormal.Count+" alarm="+sndAlarm.Count+" | onN="+normalSoundOn+" onA="+alarmSoundOn+" globalA="+globalAlarmOn);
  if (mode==Mode.Pressurizing || mode==Mode.Depressurizing) Echo("Cycle rem.: "+(CYCLE_SECONDS-Elapsed(modeStart)).ToString("0.0")+" s");
  if (mode==Mode.HoldOpenIn || mode==Mode.HoldOpenOut) Echo("Hold rem.: "+(DOOR_HOLD_SECONDS-Elapsed(modeStart)).ToString("0.0")+" s");
  if (mode==Mode.EmergencyPhase1) Echo("Emerg P1 rem.: "+(EMERG_P1_SECONDS-Elapsed(modeStart)).ToString("0.0")+" s");
  if (mode==Mode.EmergencyPhase2) Echo("Emerg P2 rem.: "+(EMERG_P2_SECONDS-Elapsed(modeStart)).ToString("0.0")+" s");
}

void DebugScanSas()
{
  Echo($"[DEBUG] SAS cible = {SAS_NAME}");
  Echo($" vents={vents.Count}, probes={probeVents.Count}, IN={doorsIN.Count}, OUT={doorsOUT.Count}, lcd={lcds.Count}, leds={leds.Count}, sndN={sndNormal.Count}");
}

HashSet<string> DiscoverAllSasNames() {
  var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
  var groups = new List<IMyBlockGroup>(); GTS.GetBlockGroups(groups);
  for (int i=0;i<groups.Count;i++) {
    var tmp = new List<IMyTerminalBlock>();
    groups[i].GetBlocks(tmp, b => SameConstruct(b));
    for (int k=0;k<tmp.Count;k++) {
      var nm = tmp[k].CustomName ?? "";
      var tag = ExtractSasTag(nm);
      if (!string.IsNullOrEmpty(tag)) set.Add(NormalizeSasName(tag));
    }
  }
  if (set.Count==0) set.Add(SAS_NAME);
  return set;
}
void LoadSasState(string sas) {
  var S = GetState(sas);
  mode = S.mode;
  modeStart = S.modeStart;
  envTarget = S.envTarget;
  emergencyCause = S.emergencyCause;
  openSince = new Dictionary<long,double>(S.openSince);
  phaseGate = S.phaseGate;
  openedThisPhase = S.openedThisPhase;
  normalSoundOn = S.normalSoundOn;
  alarmSoundOn = S.alarmSoundOn;
  normalLastKick = S.normalLastKick;
  alarmLastKick = S.alarmLastKick;


  activeDoor = null;
  if (S.activeDoorId.HasValue) {
    long id = S.activeDoorId.Value;
    for (int i=0;i<doorsIN.Count;i++)  if (doorsIN[i].EntityId==id) { activeDoor = doorsIN[i]; break; }
    if (activeDoor==null) for (int i=0;i<doorsOUT.Count;i++) if (doorsOUT[i].EntityId==id) { activeDoor = doorsOUT[i]; break; }
  }


  backupAllVentsEnabled = new Dictionary<long,bool>(S.backupAllVentsEnabled);
}

void SaveSasState(string sas)
{
  var S = GetState(sas);
  S.mode = mode;
  S.modeStart = modeStart;
  S.envTarget = envTarget;
  S.emergencyCause = emergencyCause;
  S.openSince = new Dictionary<long, double>(openSince);
  S.phaseGate = phaseGate;
  S.openedThisPhase = openedThisPhase;
  S.normalSoundOn = normalSoundOn;
  S.alarmSoundOn = alarmSoundOn;
  S.normalLastKick = normalLastKick;
  S.alarmLastKick = alarmLastKick;
  S.activeDoorId = (activeDoor != null) ? (long?)activeDoor.EntityId : null;

  S.backupAllVentsEnabled = new Dictionary<long, bool>(backupAllVentsEnabled);
}

bool ComputeGlobalEmergency(out string sourceSas, out EmergencyCause cause) {
  sourceSas = null; cause = EmergencyCause.None;
  foreach (var kv in sasStates) {
    var S = kv.Value;
    bool inEmerg = (S.mode==Mode.EmergencyPhase1 || S.mode==Mode.EmergencyPhase2);
    if (inEmerg) { sourceSas = kv.Key; cause = S.emergencyCause; return true; }
  }
  return false;
}

void UpdateGlobalEmergencySignals() {
  string src; EmergencyCause c;
  bool any = ComputeGlobalEmergency(out src, out c);


  if (any && !globalEmergencyActive) {
    globalVentsBackup.Clear();
    for (int i = 0; i < allVents.Count; i++) {
      var v = allVents[i]; if (v == null) continue;
      if (IsProbeGlobal(v)) continue;
      globalVentsBackup[v.EntityId] = v.Enabled;
    }
    ForceAllNonProbeVentsOffGlobal();
    globalEmergencyActive = true;
  } else if (!any && globalEmergencyActive) {
    if (!globalSecure) {
      RestoreNonProbeVentsToBackupGlobal();
      globalVentsBackup.Clear();
    }
    globalEmergencyActive = false;
  }

  bool localEmergencyIsSource = any &&
    string.Equals(NormalizeSasName(src), NormalizeSasName(SAS_NAME), StringComparison.OrdinalIgnoreCase);

  bool wantGlobalLeds  = any || (globalSecure && SECURE_USES_GLOBAL_LEDS);
  bool wantGlobalAlarm = !localEmergencyIsSource && (any || (globalSecure && SECURE_USES_GLOBAL_ALARM));
  string globalAlarmSound = globalSecure ? SOUND_SECURE_ALARM : SOUND_GLOBAL_ALARM;
  float globalAlarmLoop = globalSecure ? SECURE_LOOP_PERIOD : ALARM_LOOP_PERIOD;
  double globalAlarmKick = globalSecure ? SECURE_KICK_PERIOD : ALARM_KICK_PERIOD;


  if (wantGlobalLeds) EmergencyLedsBlinkRedDim(ledsEmergency);
  else                EmergencyLedsSolidWhiteRestore(ledsEmergency);


  foreach (var kv in containLights) {
    if (wantGlobalLeds) SetLedsBlinkOnGroup(kv.Value, Red(), 0.5f, 60f);
    else                SetLedsOffGroup(kv.Value);
  }



  if (!localEmergencyIsSource) {
    var globalSoundBlocks = (sndAlarm.Count > 0) ? sndAlarm : sndNormal;
    SoundGroupUpdate(
      globalSoundBlocks,
      wantGlobalAlarm,         
      globalAlarmSound,
      globalAlarmLoop,
      globalAlarmKick,
      ref globalAlarmOn,
      ref globalAlarmLastKick
    );
  } else {
    globalAlarmOn = false;
  }
}

HashSet<long> allProbeVentIds = new HashSet<long>();
bool IsProbeGlobal(IMyAirVent v)
{
  if (v == null) return false;
  if (allProbeVentIds.Contains(v.EntityId)) return true;
  var nm = v.CustomName ?? "";
  if (nm.IndexOf("[PROBE]", StringComparison.OrdinalIgnoreCase) >= 0) return true;
  if (nm.IndexOf("SAS_PROBE", StringComparison.OrdinalIgnoreCase) >= 0) return true;
  if (nm.IndexOf("PROBE", StringComparison.OrdinalIgnoreCase) >= 0) return true;
  return false;
}

void ForceCloseAndHardLock(List<IMyDoor> list)
{
  for (int i = 0; i < list.Count; i++)
  {
    var d = list[i]; if (d == null) continue;
    if (d.Status != DoorStatus.Closed)
    {
      d.Enabled = true;
      d.CloseDoor(); 
    }
    else
    {
      d.Enabled = false;
    }
  }
}

bool IsRemoteAutoEmergency()
{
  string src; EmergencyCause c;
  bool any = ComputeGlobalEmergency(out src, out c);
  return any && !IsEmergencyActive() && c != EmergencyCause.Manual;
}

void ForceAllNonProbeVentsOffGlobal(){
  for (int i = 0; i < allVents.Count; i++){
    var v = allVents[i]; if (v == null) continue;
    if (IsProbeGlobal(v)) { v.Enabled = true; v.Depressurize = false; }
    else v.Enabled = false;
  }
}

void RestoreNonProbeVentsToBackupGlobal(){
  for (int i = 0; i < allVents.Count; i++){
    var v = allVents[i]; if (v == null) continue;

  
    if (IsProbeGlobal(v)) { v.Enabled = true; v.Depressurize = false; continue; }

  
    if (VentIsContained(v)) { v.Enabled = false; continue; }

  
    v.Enabled = true;
    v.Depressurize = false;
  }
}

void ApplySasTarget(EnvTarget t)
{
  bool dep = (t == EnvTarget.Depressurized);
  for (int i = 0; i < vents.Count; i++)
  {
    var v = vents[i]; if (v == null) continue;
    v.Enabled = true;
    v.Depressurize = dep;
  }
  phaseGate = true; openedThisPhase = false; modeStart = Now;
  mode = dep ? Mode.Depressurizing : Mode.Pressurizing;
}

void RestoreContainZoneNonProbeVents(string key, bool pressurize = true){
  List<IMyAirVent> vs;
  if (!containVents.TryGetValue(key, out vs) || vs == null) return;

  for (int i = 0; i < vs.Count; i++){
    var v = vs[i]; if (v == null) continue;

    if (IsProbe(v)){
      v.Enabled = true;
      v.Depressurize = false;
    } else {
      v.Enabled = true;
      if (pressurize) v.Depressurize = false;
    }
  }
}

void RestoreNonProbeVentsFromSnapshot(Dictionary<long,bool> snap){
  for (int i=0; i<allVents.Count; i++){
    var v = allVents[i]; if (v==null) continue;
    if (IsProbeGlobal(v)) { v.Enabled=true; v.Depressurize=false; continue; }
    if (VentIsContained(v)) { v.Enabled=false; continue; }
    bool prev;
    if (snap != null && snap.TryGetValue(v.EntityId, out prev)) v.Enabled = prev;
    else v.Enabled = true;
  }
}

void ActivateSecure(){
  if (secureApplied) return;
  string _s; EmergencyCause _c;
  bool anyEmerg = ComputeGlobalEmergency(out _s, out _c);

  secureVentsBackup.Clear();
  if (!anyEmerg){
    for (int i=0; i<allVents.Count; i++){
      var v = allVents[i]; if (v==null) continue;
      if (IsProbeGlobal(v)) continue;
      secureVentsBackup[v.EntityId] = v.Enabled;
    }
  }
  AbortSasDoorActionsAndClose();
  ForceAllNonProbeVentsOffGlobal();
  secureApplied = true;
}

void DeactivateSecure(){
  if (!secureApplied) return;

  string _s; EmergencyCause _c;
  bool anyEmerg = ComputeGlobalEmergency(out _s, out _c);
  if (anyEmerg){
    Echo("[SECURE] Non restauré: URGENCE en cours.");
    return;
  }

  var snap = (secureVentsBackup.Count > 0) ? secureVentsBackup
            : (globalVentsBackup.Count > 0 ? globalVentsBackup : null);

  RestoreNonProbeVentsFromSnapshot(snap);
  secureVentsBackup.Clear();
  secureApplied = false;

  if (globalVentsBackup.Count > 0) globalVentsBackup.Clear();
}

void AbortSasDoorActionsAndClose()
{
  activeDoor = null;
  phaseGate = false;
  openedThisPhase = false;

  openSince.Clear();

  if (mode == Mode.HoldOpenIn || mode == Mode.HoldOpenOut ||
      mode == Mode.AfterCyclePressu || mode == Mode.AfterCycleDepressu ||
      mode == Mode.PreLockPressurize || mode == Mode.PreLockDepressurize ||
      mode == Mode.Pressurizing || mode == Mode.Depressurizing ||
      mode == Mode.ClosingIn || mode == Mode.ClosingOut)
    mode = Mode.Idle;

  SetEnabled(doorsIN, true);
  SetEnabled(doorsOUT, true);
  CloseAllDoors();
}
string CauseLabel(EmergencyCause c){
  switch(c){
    case EmergencyCause.O2Leak:     return "Fuite d'O2";
    case EmergencyCause.DoorDamage: return "Porte endommagée";
    case EmergencyCause.Manual:     return "Manuelle";
    default:                        return "—";
  }
}

string ModeLabel(Mode m){
  switch(m){
    case Mode.Idle:                 return "EN ATTENTE";
    case Mode.PreLockPressurize:    return "VERROUILLAGE → PRESSURISATION";
    case Mode.PreLockDepressurize:  return "VERROUILLAGE → DÉPRESSURISATION";
    case Mode.Pressurizing:         return "PRESSURISATION";
    case Mode.Depressurizing:       return "DÉPRESSURISATION";
    case Mode.AfterCyclePressu:     return "ACCÈS INTÉRIEUR (ouverture)";
    case Mode.AfterCycleDepressu:   return "ACCÈS EXTÉRIEUR (ouverture)";
    case Mode.HoldOpenIn:           return "PORTE INTÉRIEURE OUVERTE";
    case Mode.HoldOpenOut:          return "PORTE EXTÉRIEURE OUVERTE";
    case Mode.ClosingIn:            return "FERMETURE PORTE INT";
    case Mode.ClosingOut:           return "FERMETURE PORTE EXT";
    case Mode.EmergencyPhase1:      return "URGENCE (Phase 1)";
    case Mode.EmergencyPhase2:      return "URGENCE (Phase 2)";
    default:                        return m.ToString();
  }
}

string EnvLabel(EnvTarget t){
  switch(t){
    case EnvTarget.Pressurized:   return "Pressurisé";
    case EnvTarget.Depressurized: return "Dépressurisé";
    default:                      return "Indéterminé";
  }
}

bool AnyEnabled(List<IMyDoor> list){
  for (int i=0;i<list.Count;i++) if (list[i].Enabled) return true;
  return false;
}

string GroupDoorStateLabel(List<IMyDoor> list){
  if (IsAnyOpen(list)) return "OUVERT";
  bool allClosed = AllClosed(list);
  bool enabled   = AnyEnabled(list);
  if (allClosed && !enabled) return "VERROUILLÉ";
  if (allClosed)             return "FERMÉ";
  return enabled ? "—" : "VERROUILLÉ";
}

string GroupDoorStateShort(List<IMyDoor> list){
  if (IsAnyOpen(list)) return "O";          
  bool allClosed = AllClosed(list);
  bool enabled   = AnyEnabled(list);
  if (allClosed && !enabled) return "L";  
  if (allClosed)             return "F";    
  return enabled ? "-" : "L";
}

string CurrentInterlockLabel(bool anyGlobalEmerg, bool remoteAutoEmerg, bool localEmerg){
  if (globalSecure) return "SÉCURITÉ globale";
  if (anyGlobalEmerg && !localEmerg) return "Urgence distante";
  if (localEmerg){
    if (mode==Mode.EmergencyPhase2) return "Urgence P2 — lock total";
    return "Urgence P1 — accès libre (90%→lock)";
  }
  if (mode==Mode.Pressurizing || mode==Mode.Depressurizing || 
      mode==Mode.AfterCyclePressu || mode==Mode.AfterCycleDepressu ||
      mode==Mode.PreLockPressurize || mode==Mode.PreLockDepressurize)
    return "Cyclage SAS (IN/OUT verrouillés)";

  bool anyInOpen  = IsAnyOpen(doorsIN);
  bool anyOutOpen = IsAnyOpen(doorsOUT);
  if (anyInOpen && !anyOutOpen)  return "Interlock: OUT verrouillées";
  if (anyOutOpen && !anyInOpen)  return "Interlock: IN verrouillées";
  if (envTarget==EnvTarget.Pressurized)   return "Accès intérieur uniquement";
  if (envTarget==EnvTarget.Depressurized) return "Accès extérieur uniquement";
  return "—";
}

void EmergencyLedsBlinkRedDim(List<IMyLightingBlock> group) {
  for (int i = 0; i < group.Count; i++) {
    var l = group[i]; if (l == null) continue;
    float baseI;
    if (!emergencyBaseIntensity.TryGetValue(l.EntityId, out baseI)) {
      baseI = l.Intensity;
      emergencyBaseIntensity[l.EntityId] = baseI;
    }
    l.Enabled = true;
    l.Color = Red();
    l.BlinkIntervalSeconds = 0.5f;
    l.BlinkLength = 60f;
    l.Intensity = 0.10f;
  }
}

void EmergencyLedsSolidWhiteRestore(List<IMyLightingBlock> group) {
  for (int i = 0; i < group.Count; i++) {
    var l = group[i]; if (l == null) continue;
    float baseI;
    if (!emergencyBaseIntensity.TryGetValue(l.EntityId, out baseI)) {
      baseI = l.Intensity;
      emergencyBaseIntensity[l.EntityId] = baseI;
    }
    l.Enabled = true;
    l.Color = White();
    l.BlinkIntervalSeconds = 0f;
    l.BlinkLength = 50f;
    l.Intensity = baseI;
  }
}
