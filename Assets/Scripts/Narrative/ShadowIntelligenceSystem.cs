using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ZSP.Narrative
{
    [Serializable] public class TrustEntry { public string id; public float value; }
    [Serializable] public class FactionEntry { public string id; public int influence; public int heat; public bool exposed; public string alliance; }
    [Serializable] public class WitnessEntry { public string id; public string status; public bool cooperative; }
    [Serializable] public class KeyCharEntry { public string id; public string status; }
    [Serializable] public class EndingEntry { public string mission; public string ending; }

    [Serializable]
    public class SISState
    {
        public List<TrustEntry> trust = new();
        public List<FactionEntry> factions = new();
        public List<WitnessEntry> witnesses = new();
        public List<KeyCharEntry> keyCharacters = new();
        public List<string> intel = new();
        public List<string> flags = new();
        public List<string> completed = new();
        public List<EndingEntry> endings = new();

        public SISState Clone()
        {
            return new SISState
            {
                trust = trust.Select(e => new TrustEntry { id = e.id, value = e.value }).ToList(),
                factions = factions.Select(e => new FactionEntry { id = e.id, influence = e.influence, heat = e.heat, exposed = e.exposed, alliance = e.alliance }).ToList(),
                witnesses = witnesses.Select(e => new WitnessEntry { id = e.id, status = e.status, cooperative = e.cooperative }).ToList(),
                keyCharacters = keyCharacters.Select(e => new KeyCharEntry { id = e.id, status = e.status }).ToList(),
                intel = intel.ToList(),
                flags = flags.ToList(),
                completed = completed.ToList(),
                endings = endings.Select(e => new EndingEntry { mission = e.mission, ending = e.ending }).ToList(),
            };
        }
    }

    [Serializable] public class BehaviorEventRecord { public string missionId; public string action; public string target; public float value; public int seq; }
    [Serializable] public class LedgerCommit { public string missionId; public string behaviorDigest; public SISState delta; public string prevHash; public string commitHash; }
    [Serializable] public class SISSaveRecord { public int version = 1; public SISState state; public List<BehaviorEventRecord> behavior; public List<LedgerCommit> ledger; }

    public sealed class MissionRouteCondition { public string id, name, kind; public bool available; public int difficulty; public string reason; }
    public sealed class MissionWitnessCondition { public string id, name, status, role, effect; }
    public sealed class MissionContactCondition { public string id, name; public float trust, exposure; public bool compromised, available; }
    public sealed class MissionConditions
    {
        public string missionId, title, locationName;
        public readonly Dictionary<string, MissionRouteCondition> Entry = new();
        public int Detection, Armor, Strategy, Reinforcements, Adaptation;
        public readonly List<MissionWitnessCondition> Witnesses = new();
        public readonly List<MissionContactCondition> Contacts = new();
        public readonly List<string> Objectives = new();
        public readonly List<string> Endings = new();
        public readonly List<string> Notes = new();
        public int Seed;
    }

    /// <summary>
    /// C# port of the Python Shadow Intelligence System reference engine.
    /// </summary>
    public sealed class ShadowIntelligenceSystem
    {
        public StoryContent Content { get; }
        public SISState State { get; private set; }
        public List<BehaviorEventRecord> Behavior { get; private set; } = new();
        public List<LedgerCommit> Ledger { get; private set; } = new();
        private SISState _pending;

        public ShadowIntelligenceSystem(StoryContent content)
        {
            Content = content;
            State = InitialState(content);
            _pending = new SISState();
        }

        private static SISState InitialState(StoryContent content)
        {
            var state = new SISState();
            foreach (var c in content.Contacts.Values) state.trust.Add(new TrustEntry { id = c.id, value = c.trust });
            foreach (var f in content.Factions.Values) state.factions.Add(new FactionEntry { id = f.id, influence = f.influence, heat = f.heat, exposed = f.exposed, alliance = f.alliance });
            foreach (var w in content.Witnesses.Values) { state.witnesses.Add(new WitnessEntry { id = w.id, status = "unknown", cooperative = false }); }
            foreach (var c in content.Characters.Values) state.keyCharacters.Add(new KeyCharEntry { id = c.id, status = "unknown" });
            foreach (var i in content.Intel.Values) { /* intel starts unearned */ }
            foreach (var m in content.Missions.Values) state.endings.Add(new EndingEntry { mission = m.id, ending = "" });
            return state;
        }

        // ---- state access ----

        public float Trust(string id) => FindTrust(State, id)?.value ?? 0f;
        public float Exposure(string id)
        {
            Content.Contacts.TryGetValue(id, out var contact);
            return Trust(id) * (contact?.centrality ?? 0f);
        }
        public int TrustEvents(string id) => Behavior.Count(e => e.action == "trust_contact" && e.target == id);
        public bool IsCompromised(string id)
        {
            var threshold = id == "tafadzwa" ? 0.40f : id == "muponda" ? 0.50f : 0.55f;
            var minEvents = id == "amara" ? 3 : 2;
            return HasFlag($"contact_compromised_{id}") || (Exposure(id) >= threshold && TrustEvents(id) >= minEvents);
        }
        public bool ContactAvailable(string id)
        {
            if (Content.Contacts.TryGetValue(id, out var contact) && !contact.available) return false;
            if (Witnesses().TryGetValue(id, out var witness) && (witness.status == "killed" || witness.status == "captured")) return false;
            if (id == "amara" && HasFlag("amara_captured")) return false;
            if (id == "tafadzwa" && HasFlag("tafadzwa_captured")) return false;
            return Trust(id) > 0f;
        }

        public string WitnessStatus(string id) => Witnesses().TryGetValue(id, out var w) ? w.status : "unknown";
        public bool WitnessCooperative(string id) => Witnesses().TryGetValue(id, out var w) && w.cooperative;
        public string KeyCharStatus(string id) => State.keyCharacters.FirstOrDefault(e => e.id == id)?.status ?? "unknown";
        public bool HasIntel(string id) => State.intel.Contains(id);
        public bool HasFlag(string flag) => State.flags.Contains(flag);
        public int FactionInt(string id, Func<FactionEntry, int> selector) => State.factions.FirstOrDefault(e => e.id == id) == null ? 0 : selector(State.factions.First(e => e.id == id));
        public bool FactionBool(string id, Func<FactionEntry, bool> selector) => State.factions.FirstOrDefault(e => e.id == id) != null && selector(State.factions.First(e => e.id == id));
        public string FactionString(string id, Func<FactionEntry, string> selector) => State.factions.FirstOrDefault(e => e.id == id) == null ? "" : selector(State.factions.First(e => e.id == id));

        private Dictionary<string, WitnessEntry> Witnesses() => State.witnesses.ToDictionary(w => w.id);

        private static TrustEntry FindTrust(SISState s, string id) => s.trust.FirstOrDefault(e => e.id == id);

        // ---- behaviour ----

        public BehaviorEventRecord Record(string missionId, string action, string target, float value)
        {
            var record = new BehaviorEventRecord { missionId = missionId, action = action, target = target ?? string.Empty, value = value, seq = Behavior.Count };
            Behavior.Add(record);
            return record;
        }

        public string BehaviorDigest()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < Behavior.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonUtility.ToJson(Behavior[i]));
            }
            sb.Append(']');
            using var sha = SHA256.Create();
            return ShaHex(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        private static string ShaHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ---- apply effects ----

        public void ApplyEffects(StoryEffect effects, string missionId, bool inferBehavior = true)
        {
            if (effects == null) return;
            foreach (var item in effects.trust)
            {
                if (!Content.Contacts.ContainsKey(item.contact)) continue;
                var value = Mathf.Clamp01(Trust(item.contact) + item.delta);
                SetTrust(item.contact, value);
            }
            foreach (var item in effects.factions)
            {
                if (!Content.Factions.ContainsKey(item.id)) continue;
                var influence = Mathf.Clamp(FactionInt(item.id, f => f.influence) + item.influence, 0, 10);
                var heat = Mathf.Clamp(FactionInt(item.id, f => f.heat) + item.heat, 0, 10);
                var exposed = FactionBool(item.id, f => f.exposed) || item.exposed;
                var alliance = string.IsNullOrEmpty(item.alliance) ? FactionString(item.id, f => f.alliance) : item.alliance;
                SetFaction(item.id, influence, heat, exposed, alliance);
            }
            foreach (var intel in effects.intel) if (Content.Intel.ContainsKey(intel)) AddIntel(intel);
            foreach (var flag in effects.flags) AddFlag(flag);
            foreach (var item in effects.witnesses)
            {
                if (!Content.Witnesses.ContainsKey(item.id)) continue;
                SetWitness(item.id, string.IsNullOrEmpty(item.status) ? null : item.status, item.cooperative);
            }
            foreach (var item in effects.keyCharacters) if (Content.Characters.ContainsKey(item.id)) SetKeyChar(item.id, item.status);
            if (!string.IsNullOrEmpty(effects.ending)) SetEnding(missionId, effects.ending);

            if (effects.behavior != null && effects.behavior.Count > 0)
            {
                foreach (var b in effects.behavior) Record(missionId, b.action, b.target, b.value);
            }
            else if (inferBehavior)
            {
                foreach (var t in effects.trust) Record(missionId, "trust_contact", t.contact, 1);
                foreach (var w in effects.witnesses)
                {
                    if (w.status == "spared") Record(missionId, "witness_spared", w.id, 1);
                    else if (w.status == "killed") Record(missionId, "witness_killed", w.id, 1);
                }
                foreach (var k in effects.keyCharacters) if (k.status == "dead") Record(missionId, "key_character_killed", k.id, 1);
                foreach (var f in effects.factions) if (f.exposed) Record(missionId, "expose_org", f.id, 1);
            }
        }

        private void SetTrust(string id, float value)
        {
            Upsert(State.trust, _pending.trust, new TrustEntry { id = id, value = value }, e => e.id == id);
        }

        private void SetFaction(string id, int influence, int heat, bool exposed, string alliance)
        {
            var entry = new FactionEntry { id = id, influence = influence, heat = heat, exposed = exposed, alliance = alliance };
            Upsert(State.factions, _pending.factions, entry, e => e.id == id);
        }

        private void AddIntel(string id) { if (!State.intel.Contains(id)) { State.intel.Add(id); _pending.intel.Add(id); } }
        private void AddFlag(string id) { if (!State.flags.Contains(id)) { State.flags.Add(id); _pending.flags.Add(id); } }

        private void SetWitness(string id, string status, bool cooperative)
        {
            var existing = State.witnesses.FirstOrDefault(e => e.id == id);
            var entry = new WitnessEntry
            {
                id = id,
                status = string.IsNullOrEmpty(status) ? (existing?.status ?? "unknown") : status,
                cooperative = (existing?.cooperative ?? false) || cooperative,
            };
            Upsert(State.witnesses, _pending.witnesses, entry, e => e.id == id);
        }

        private void SetKeyChar(string id, string status)
        {
            var entry = new KeyCharEntry { id = id, status = status };
            Upsert(State.keyCharacters, _pending.keyCharacters, entry, e => e.id == id);
        }

        private void SetEnding(string missionId, string ending)
        {
            var existing = State.endings.FirstOrDefault(e => e.mission == missionId);
            var entry = new EndingEntry { mission = missionId, ending = ending };
            Upsert(State.endings, _pending.endings, entry, e => e.mission == missionId);
        }

        private static void Upsert<T>(List<T> state, List<T> pending, T entry, Func<T, bool> match)
        {
            var index = state.FindIndex(match);
            if (index >= 0) state[index] = entry; else state.Add(entry);
            var pIndex = pending.FindIndex(match);
            if (pIndex >= 0) pending[pIndex] = entry; else pending.Add(entry);
        }

        public void CommitMission(string missionId, string ending = null)
        {
            if (!State.completed.Contains(missionId)) { State.completed.Add(missionId); _pending.completed.Add(missionId); }
            if (!string.IsNullOrEmpty(ending)) SetEnding(missionId, ending);
            var delta = _pending.Clone();
            var prevHash = Ledger.Count > 0 ? Ledger[Ledger.Count - 1].commitHash : "GENESIS";
            var body = missionId + "|" + BehaviorDigest() + "|" + JsonUtility.ToJson(delta) + "|" + prevHash;
            using var sha = SHA256.Create();
            var commit = new LedgerCommit { missionId = missionId, behaviorDigest = BehaviorDigest(), delta = delta, prevHash = prevHash, commitHash = ShaHex(sha.ComputeHash(Encoding.UTF8.GetBytes(body))) };
            Ledger.Add(commit);
            _pending = new SISState();
        }

        public (bool ok, string error) VerifyChain()
        {
            var prev = "GENESIS";
            for (int i = 0; i < Ledger.Count; i++)
            {
                var c = Ledger[i];
                if (c.prevHash != prev) return (false, $"commit {i}: prevHash mismatch");
                var body = c.missionId + "|" + c.behaviorDigest + "|" + JsonUtility.ToJson(c.delta) + "|" + c.prevHash;
                using var sha = SHA256.Create();
                if (c.commitHash != ShaHex(sha.ComputeHash(Encoding.UTF8.GetBytes(body)))) return (false, $"commit {i}: commitHash mismatch");
                prev = c.commitHash;
            }
            return (true, string.Empty);
        }

        // ---- mission resolution ----

        public MissionConditions ResolveMission(string missionId)
        {
            var mission = Content.Mission(missionId) ?? throw new InvalidOperationException("Unknown mission " + missionId);
            ApplyMissionStartEffects(mission);
            var location = Content.LocationFor(missionId);
            var conditions = new MissionConditions { missionId = missionId, title = mission.title, locationName = location?.name ?? mission.location };
            var frontRatio = 0f;
            var entries = 0;
            var profileTotal = Mathf.Max(1, Behavior.Count);
            var stealthRatio = Behavior.Count(e => e.action == "stealth_takedown" || e.action == "stealth_avoid") / (float)profileTotal;
            var aggressionRatio = Behavior.Count(e => e.action == "gunfight" || e.action == "melee" || e.action == "key_character_killed") / (float)profileTotal;
            var vehicleRatio = Behavior.Count(e => e.action == "vehicle_chase") / (float)profileTotal;
            foreach (var e in Behavior.Where(e => e.action == "approach"))
            {
                entries++;
                if (Mathf.Abs(e.value - 1f) < 0.01f) frontRatio += 1f;
            }
            if (entries > 0) frontRatio /= entries;

            foreach (var route in location.routes)
            {
                int difficulty = route.baseDifficulty;
                var reasons = new List<string>();
                bool available = true;
                if (route.id == "hill_path" && !(Trust("muponda") >= 0.5f || HasFlag("muponda_route"))) { available = false; reasons.Add("Muponda has not offered the path."); }
                if (route.id == "press_convoy" && !(Trust("tafadzwa") >= 0.4f || HasFlag("ledger_published"))) { available = false; reasons.Add("Tafadzwa cannot attach you to the convoy."); }
                if (route.id == "victor_keycard" && !(WitnessStatus("victor") == "spared" && HasIntel("victor_keycard"))) { available = false; reasons.Add("Victor's keycard is not available."); }
                if (route.kind == "front" && entries >= 2 && frontRatio >= 0.6f) { difficulty += 2; reasons.Add("Front approaches used repeatedly; entrance reinforced."); }
                if (stealthRatio >= 0.5f) { difficulty += 1; reasons.Add("Stealth profile raised camera budget."); }
                if (FactionInt("meridian", f => f.heat) >= 4 && (route.kind == "front" || route.kind == "side")) { difficulty += 1; reasons.Add("Meridian is on high alert."); }
                if (missionId == "M3" && route.id == "main_gate") { difficulty += 1; reasons.Add("The main gate is a killing box."); }
                if (WitnessStatus("victor") == "killed" && route.id == "east_service") { difficulty -= 1; reasons.Add("Victor's death left east patrols disorganised."); }
                conditions.Entry[route.id] = new MissionRouteCondition { id = route.id, name = route.name, kind = route.kind, difficulty = Mathf.Max(1, difficulty), available = available, reason = string.Join(" ", reasons) };
            }

            int adaptationDetection = Mathf.FloorToInt(stealthRatio * 4f);
            int adaptationArmor = Mathf.FloorToInt(aggressionRatio * 4f);
            int adaptationStrategy = Mathf.FloorToInt(vehicleRatio * 3f);
            int adaptationReinforcements = 0;
            if (entries >= 2 && frontRatio >= 0.6f) { adaptationStrategy += 2; adaptationReinforcements += 1; conditions.Notes.Add("Front-door pattern: Meridian reinforced entrances."); }
            if (Behavior.Count(e => e.action == "witness_spared") >= 1) { adaptationStrategy += 1; conditions.Notes.Add("Spared witnesses: Meridian is asking questions."); }
            if (Content.Contacts.Keys.Any(IsCompromised)) { adaptationDetection += 1; conditions.Notes.Add("A contact is exposed; Meridian watches your network."); }
            if (FactionInt("meridian", f => f.heat) >= 4) { adaptationDetection += 1; adaptationArmor += 1; conditions.Notes.Add("Meridian heat is high."); }
            if (Behavior.Count(e => e.action == "key_character_killed") >= 1) { adaptationStrategy += 1; conditions.Notes.Add("A key character died; Meridian reorganised."); }
            if (HasFlag("lovemore_informant")) { adaptationDetection = Mathf.Max(0, adaptationDetection - 1); conditions.Notes.Add("Lovemore's patrol schedule reduces detection."); }
            if (HasFlag("lovemore_liability")) { adaptationDetection += 1; conditions.Notes.Add("Lovemore may have warned Meridian."); }
            int adaptationTotal = adaptationDetection + adaptationArmor + adaptationStrategy + adaptationReinforcements;
            if (adaptationTotal > 10)
            {
                float scale = 10f / adaptationTotal;
                adaptationDetection = Mathf.FloorToInt(adaptationDetection * scale);
                adaptationArmor = Mathf.FloorToInt(adaptationArmor * scale);
                adaptationStrategy = Mathf.FloorToInt(adaptationStrategy * scale);
                adaptationReinforcements = Mathf.FloorToInt(adaptationReinforcements * scale);
            }
            int order = mission.order;
            conditions.Detection = Mathf.Clamp(order + 1 + adaptationDetection, 0, 10);
            conditions.Armor = Mathf.Clamp(order + adaptationArmor, 0, 10);
            conditions.Strategy = Mathf.Clamp(order + adaptationStrategy, 0, 10);
            conditions.Reinforcements = Mathf.Max(0, order - 1) + adaptationReinforcements;
            conditions.Adaptation = adaptationDetection + adaptationArmor + adaptationStrategy + adaptationReinforcements;

            foreach (var wid in mission.witnesses)
            {
                var status = WitnessStatus(wid);
                if (status == "unknown" || status == "killed") continue;
                string role = "witness", effect = "";
                if (wid == "lovemore") { role = WitnessCooperative(wid) ? "informant" : "liability"; effect = WitnessCooperative(wid) ? "reveals patrol routes" : "may warn Meridian"; }
                else if (wid == "tafadzwa") { role = WitnessCooperative(wid) ? "whistleblower" : "liability"; effect = WitnessCooperative(wid) ? "testimony available" : "will not testify"; }
                else if (wid == "victor") { role = "potential_ally"; effect = "keycard route possible"; }
                conditions.Witnesses.Add(new MissionWitnessCondition { id = wid, name = Content.Witnesses[wid].name, status = status, role = role, effect = effect });
                conditions.Notes.Add($"Witness {Content.Witnesses[wid].name}: {role} ({effect}).");
            }

            foreach (var cid in mission.contacts)
            {
                var contact = Content.Contacts[cid];
                bool compromised = IsCompromised(cid);
                conditions.Contacts.Add(new MissionContactCondition { id = cid, name = contact.name, trust = Trust(cid), exposure = Exposure(cid), compromised = compromised, available = ContactAvailable(cid) });
                if (compromised) conditions.Notes.Add($"Contact {contact.name} is exposed (trust {Trust(cid):F2}, exposure {Exposure(cid):F2}).");
            }

            conditions.Objectives.AddRange(mission.objectives);
            if (missionId == "M3")
            {
                if (HasFlag("amara_captured")) conditions.Objectives.Add("Rescue Amara from the estate");
                if (HasFlag("tafadzwa_targeted")) conditions.Objectives.Add("Protect Tafadzwa");
                if (WitnessStatus("tafadzwa") == "captured") conditions.Objectives.Add("Recover Tafadzwa from Meridian");
                if (WitnessStatus("lovemore") == "killed" && WitnessStatus("tafadzwa") == "killed") conditions.Objectives.Add("Hack the estate terminal for the core file");
            }
            conditions.Endings.AddRange(mission.endings ?? new List<string>());
            conditions.Seed = (int)(BehaviorDigest().Substring(0, 8).Aggregate(0u, (h, c) => h * 131 + (uint)c));
            return conditions;
        }

        private void ApplyMissionStartEffects(StoryMission mission)
        {
            int order = mission.order;
            if (order >= 2 && IsCompromised("amara")) AddFlag("amara_captured");
            if (mission.id == "M2" && WitnessStatus("lovemore") == "spared")
            {
                if (WitnessCooperative("lovemore")) { AddIntel("cleaner_roster"); AddFlag("lovemore_informant"); }
                else { AddFlag("lovemore_liability"); if (!HasFlag("lovemore_liability_applied")) { SetFaction("meridian", FactionInt("meridian", f => f.influence), FactionInt("meridian", f => f.heat) + 1, FactionBool("meridian", f => f.exposed), FactionString("meridian", f => f.alliance)); AddFlag("lovemore_liability_applied"); } }
            }
            if (mission.id == "M3")
            {
                if (WitnessStatus("tafadzwa") == "spared" && WitnessCooperative("tafadzwa")) { AddIntel("tafadzwa_tip"); AddFlag("tafadzwa_whistleblower"); }
                if (WitnessStatus("victor") == "spared") { AddIntel("victor_keycard"); AddFlag("victor_ally"); }
                if (IsCompromised("tafadzwa")) AddFlag("tafadzwa_targeted");
            }
        }

        // ---- save / load ----

        public SISSaveRecord Export() => new SISSaveRecord { version = 1, state = State.Clone(), behavior = Behavior, ledger = Ledger };
        public static ShadowIntelligenceSystem FromRecord(StoryContent content, SISSaveRecord record)
        {
            var sis = new ShadowIntelligenceSystem(content) { State = record.state };
            sis.Behavior = new List<BehaviorEventRecord>(record.behavior ?? new List<BehaviorEventRecord>());
            sis.Ledger = new List<LedgerCommit>(record.ledger ?? new List<LedgerCommit>());
            sis._pending = new SISState();
            return sis;
        }
    }
}
