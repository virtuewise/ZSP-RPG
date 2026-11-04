using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ZSP.Narrative
{
    public enum ActionType
    {
        StealthTakedown,
        StealthAvoid,
        Gunfight,
        VehicleChase,
        Melee,
        Interrogate,
        TrustContact,
        ExposeOrg,
        Upgrade
    }

    [Serializable]
    public class BehaviorEvent
    {
        public string missionId;
        public ActionType action;
        public string target;
        public float value;
        public int ts;

        public BehaviorEvent() { }

        public BehaviorEvent(string missionId, ActionType action, string target = "", float value = 1f)
        {
            this.missionId = missionId;
            this.action = action;
            this.target = target;
            this.value = value;
        }
    }

    public class BehaviorProfile
    {
        public int totalActions;
        public float stealthRatio;
        public float aggressionRatio;
        public int trustEvents;
        public int exposeEvents;
        public float vehicleRatio;
        public int interrogationCount;
        public Dictionary<string, float> contactTrust = new Dictionary<string, float>();
    }

    public class BehaviorLog
    {
        public readonly List<BehaviorEvent> events = new List<BehaviorEvent>();

        public void Record(BehaviorEvent ev)
        {
            if (ev.ts == 0) ev.ts = events.Count;
            events.Add(ev);
        }

        public BehaviorProfile Profile(ContactGraph contacts)
        {
            int n = Mathf.Max(1, events.Count);
            int stealth = 0, aggro = 0, vehicle = 0, trust = 0, expose = 0, interr = 0;

            foreach (BehaviorEvent e in events)
            {
                switch (e.action)
                {
                    case ActionType.StealthTakedown:
                    case ActionType.StealthAvoid: stealth++; break;
                    case ActionType.Gunfight:
                    case ActionType.Melee: aggro++; break;
                    case ActionType.VehicleChase: vehicle++; break;
                    case ActionType.TrustContact: trust++; break;
                    case ActionType.ExposeOrg: expose++; break;
                    case ActionType.Interrogate: interr++; break;
                }
            }

            return new BehaviorProfile
            {
                totalActions = events.Count,
                stealthRatio = (float)stealth / n,
                aggressionRatio = (float)aggro / n,
                trustEvents = trust,
                exposeEvents = expose,
                vehicleRatio = (float)vehicle / n,
                interrogationCount = interr,
                contactTrust = contacts.TrustWeights()
            };
        }

        public string Digest()
        {
            var sb = new StringBuilder();
            foreach (BehaviorEvent e in events)
            {
                sb.Append(e.missionId).Append('|')
                  .Append(e.action).Append('|')
                  .Append(e.target).Append('|')
                  .Append(e.value.ToString("R")).Append('|')
                  .Append(e.ts).Append(';');
            }
            return Hash(sb.ToString());
        }

        public int Seed()
        {
            string d = Digest();
            uint val = uint.Parse(d.Substring(0, 8), System.Globalization.NumberStyles.HexNumber);
            return unchecked((int)val);
        }

        public static string Hash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }

    public class ContactGraph
    {
        private readonly Dictionary<string, float> trust = new Dictionary<string, float>();
        private readonly Dictionary<string, float> centrality = new Dictionary<string, float>();

        public void AddContact(string id, string displayName, float importance)
        {
            if (!trust.ContainsKey(id)) trust[id] = 0f;
            centrality[id] = Mathf.Clamp01(importance);
        }

        public void Trust(string id, float delta)
        {
            if (!trust.ContainsKey(id)) trust[id] = 0f;
            trust[id] = Mathf.Clamp01(trust[id] + delta);
        }

        public Dictionary<string, float> TrustWeights() => new Dictionary<string, float>(trust);

        public float Exposure(string id)
        {
            float t = trust.ContainsKey(id) ? trust[id] : 0f;
            float c = centrality.ContainsKey(id) ? centrality[id] : 0f;
            return t * c;
        }

        public List<string> CriticalSources(float threshold = 0.3f)
        {
            var list = new List<string>();
            foreach (var kv in trust) if (kv.Value >= threshold) list.Add(kv.Key);
            return list;
        }

        public List<string> Vulnerabilities(float threshold = 0.25f)
        {
            var list = new List<string>();
            foreach (var kv in trust) if (Exposure(kv.Key) >= threshold) list.Add(kv.Key);
            return list;
        }
    }

    [Serializable]
    public class ConsequenceCommit
    {
        public string missionId;
        public string parentDigest;
        public Dictionary<string, string> delta = new Dictionary<string, string>();
        public string prevHash;
        public string commitHash;

        public string ComputeHash()
        {
            var sb = new StringBuilder();
            sb.Append(missionId).Append('|').Append(parentDigest).Append('|').Append(prevHash).Append('|');
            var keys = new List<string>(delta.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (string key in keys)
                sb.Append(key).Append('=').Append(delta[key]).Append(';');
            return BehaviorLog.Hash(sb.ToString());
        }
    }

    public class ReplayResult
    {
        public Dictionary<string, string> state = new Dictionary<string, string>();
        public List<string> contradictions = new List<string>();
        public List<string> completed = new List<string>();
    }

    public class ConsequenceLedger
    {
        public readonly List<ConsequenceCommit> commits = new List<ConsequenceCommit>();
        public string lastHash = "GENESIS";

        public ConsequenceCommit Commit(string missionId, string parentDigest, Dictionary<string, string> delta)
        {
            var c = new ConsequenceCommit
            {
                missionId = missionId,
                parentDigest = parentDigest,
                delta = delta,
                prevHash = lastHash
            };
            c.commitHash = c.ComputeHash();
            commits.Add(c);
            lastHash = c.commitHash;
            return c;
        }

        public bool VerifyChain()
        {
            string prev = "GENESIS";
            foreach (ConsequenceCommit c in commits)
            {
                c.prevHash = prev;
                if (c.commitHash != c.ComputeHash()) return false;
                prev = c.commitHash;
            }
            return true;
        }

        public ReplayResult Replay(Dictionary<string, string> initial)
        {
            var result = new ReplayResult();
            foreach (var kv in initial) result.state[kv.Key] = kv.Value;

            foreach (ConsequenceCommit c in commits)
            {
                foreach (var kv in c.delta)
                {
                    string k = kv.Key, v = kv.Value;

                    if (k == "completed_missions")
                    {
                        result.completed.Add(v);
                        continue;
                    }

                    if (k.StartsWith("trust:"))
                    {
                        float prevTrust;
                        if (float.TryParse(result.state.TryGetValue(k, out string p) ? p : "0", out prevTrust))
                        {
                            float nv;
                            float.TryParse(v, out nv);
                            if (nv < prevTrust - 0.0001f)
                                result.contradictions.Add($"{c.missionId}: trust regression on {k}");
                            result.state[k] = Mathf.Max(prevTrust, nv).ToString("R");
                        }
                        else result.state[k] = v;
                        continue;
                    }

                    if (k == "org_exposed")
                    {
                        bool wasTrue = result.state.TryGetValue(k, out string old) && old == "True";
                        if (wasTrue && v == "False")
                            result.contradictions.Add($"{c.missionId}: org_exposed reverted True->False");
                        result.state[k] = (wasTrue || v == "True") ? "True" : "False";
                        continue;
                    }

                    if (k == "difficulty_floor")
                    {
                        int oldV = result.state.TryGetValue(k, out string s) ? int.Parse(s) : 1;
                        int newV = int.Parse(v);
                        result.state[k] = Mathf.Max(oldV, newV).ToString();
                        continue;
                    }

                    if (k == "alliance" || k == "template_last")
                    {
                        result.state[k] = v;
                        continue;
                    }

                    if (result.state.TryGetValue(k, out string existing) && existing != v)
                        result.contradictions.Add($"{c.missionId}: conflict on '{k}' ({existing} vs {v})");

                    result.state[k] = v;
                }
            }

            return result;
        }
    }

    [Serializable]
    public class MissionTemplate
    {
        public string id;
        public string displayName;
        public int baseDifficulty;
        public bool narrativeInvariant;
        public string[] tags;

        public MissionTemplate(string id, string displayName, int baseDifficulty,
            bool narrativeInvariant, params string[] tags)
        {
            this.id = id;
            this.displayName = displayName;
            this.baseDifficulty = baseDifficulty;
            this.narrativeInvariant = narrativeInvariant;
            this.tags = tags ?? new string[0];
        }
    }

    [Serializable]
    public class MissionInstance
    {
        public string missionId;
        public string templateId;
        public string displayName;
        public int difficulty;
        public int enemyDetection;
        public int enemyArmor;
        public List<string> objectives = new List<string>();
        public List<string> notes = new List<string>();
        public int seed;
    }

    /// <summary>
    /// Adaptive Mission Chain — C# port of the Python prototype.
    /// Behaviour-indexed consequence ledger + deterministic replay reconciliation.
    /// </summary>
    public class AdaptiveMissionChain
    {
        public const int AdaptationBudget = 10;

        public readonly BehaviorLog behavior = new BehaviorLog();
        public readonly ContactGraph contacts = new ContactGraph();
        public readonly ConsequenceLedger ledger = new ConsequenceLedger();
        public readonly List<MissionTemplate> templates = new List<MissionTemplate>();
        public readonly List<string> completed = new List<string>();

        public readonly Dictionary<string, string> narrativeState = new Dictionary<string, string>
        {
            { "org_exposed", "False" },
            { "alliance", "neutral" },
            { "difficulty_floor", "1" }
        };

        private int missionCounter;

        public AdaptiveMissionChain()
        {
            RegisterDefaultTemplates();
            RegisterDefaultContacts();
        }

        private void RegisterDefaultTemplates()
        {
            templates.Add(new MissionTemplate("T_STEALTH_INFIL", "Stealth Infiltration", 4, false, "stealth"));
            templates.Add(new MissionTemplate("T_SHOOTOUT", "Compound Shootout", 5, false, "combat"));
            templates.Add(new MissionTemplate("T_VEHICLE", "Vehicle Pursuit", 5, false, "vehicle"));
            templates.Add(new MissionTemplate("T_INTERROGATE", "Interrogation", 3, false, "intel"));
            templates.Add(new MissionTemplate("T_CONTACT_MEET", "Contact Rendezvous", 3, false, "social"));
            templates.Add(new MissionTemplate("T_EXPOSE", "Expose Operation", 7, true, "climax"));
            templates.Add(new MissionTemplate("T_VULN_CONTACT", "Contact Vulnerability", 6, false, "social", "risk"));
            templates.Add(new MissionTemplate("T_DETECTION_RAID", "Detection-Heavy Raid", 6, false, "stealth", "adapted"));
        }

        private void RegisterDefaultContacts()
        {
            contacts.AddContact("C1", "Tafadzwa", 0.9f);
            contacts.AddContact("C2", "Muponda", 0.6f);
            contacts.AddContact("C3", "Rudo", 0.4f);
        }

        public void RecordBehavior(string missionId, params BehaviorEvent[] evs)
        {
            foreach (BehaviorEvent e in evs)
            {
                e.missionId = missionId;
                behavior.Record(e);
            }
        }

        public MissionInstance GenerateNextMission()
        {
            BehaviorProfile profile = behavior.Profile(contacts);
            AdaptationAllocation alloc = AllocateAdaptation(profile);

            var rng = new System.Random(behavior.Seed() ^ (completed.Count * 7919));
            MissionTemplate chosen = PickTemplate(profile, rng);

            missionCounter++;
            var mission = new MissionInstance
            {
                missionId = "M" + missionCounter.ToString("000"),
                templateId = chosen.id,
                displayName = chosen.displayName,
                seed = behavior.Seed()
            };

            mission.objectives.Add("Complete " + chosen.displayName);

            if (chosen.id == "T_VULN_CONTACT")
            {
                List<string> vulns = contacts.Vulnerabilities();
                if (vulns.Count > 0)
                {
                    mission.objectives.Add($"Investigate contact {vulns[0]}");
                    mission.notes.Add($"Contact {vulns[0]} exposure crossed threshold: {contacts.Exposure(vulns[0]):F3}");
                }
            }

            if (chosen.id == "T_CONTACT_MEET")
            {
                List<string> sources = contacts.CriticalSources();
                if (sources.Count > 0)
                    mission.notes.Add("Critical sources available: " + string.Join(", ", sources));
            }

            int floor = int.Parse(narrativeState["difficulty_floor"]);
            int diff = chosen.baseDifficulty + alloc.strategy + floor - 1;
            if (profile.exposeEvents > 0)
            {
                diff += 2;
                mission.notes.Add("Organization already exposed: heightened security");
            }

            mission.difficulty = Mathf.Clamp(diff, 1, 10);
            mission.enemyDetection = Mathf.Clamp(chosen.baseDifficulty / 2 + alloc.detection, 0, 10);
            mission.enemyArmor = Mathf.Clamp(alloc.armor, 0, 10);
            return mission;
        }

        private MissionTemplate PickTemplate(BehaviorProfile profile, System.Random rng)
        {
            var weights = new List<float>();
            float total = 0f;

            foreach (MissionTemplate t in templates)
            {
                float w = 1f;
                if (HasTag(t, "stealth")) w += profile.stealthRatio * 2f;
                if (HasTag(t, "combat")) w += profile.aggressionRatio * 2f;
                if (HasTag(t, "vehicle")) w += profile.vehicleRatio * 2f;
                if (HasTag(t, "social")) w += 0.5f * (profile.trustEvents + 1) / 5f;
                if (t.id == "T_VULN_CONTACT" && contacts.Vulnerabilities().Count > 0) w += 2f;
                if (t.id == "T_DETECTION_RAID" && profile.stealthRatio > 0.4f) w += 1.5f;
                weights.Add(w);
                total += w;
            }

            float roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < templates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return templates[i];
            }
            return templates[templates.Count - 1];
        }

        private static bool HasTag(MissionTemplate t, string tag)
        {
            foreach (string s in t.tags) if (s == tag) return true;
            return false;
        }

        private struct AdaptationAllocation
        {
            public int detection;
            public int armor;
            public int strategy;
        }

        private static AdaptationAllocation AllocateAdaptation(BehaviorProfile p)
        {
            int det = (int)(p.stealthRatio * AdaptationBudget * 0.6f);
            int armor = (int)(p.aggressionRatio * AdaptationBudget * 0.5f);
            int strat = (int)(p.vehicleRatio * AdaptationBudget * 0.4f);

            if (p.exposeEvents > 0) strat += 3;
            if (p.trustEvents > 2) det += 2;

            int total = det + armor + strat;
            if (total > AdaptationBudget)
            {
                float scale = (float)AdaptationBudget / Mathf.Max(1, total);
                det = (int)(det * scale);
                armor = (int)(armor * scale);
                strat = AdaptationBudget - det - armor;
            }

            return new AdaptationAllocation { detection = det, armor = armor, strategy = strat };
        }

        public void CompleteMission(string missionId, string templateId,
            Dictionary<string, float> trustDeltas = null, bool exposed = false, string alliance = null)
        {
            completed.Add(templateId + ":" + missionId);

            if (trustDeltas != null)
                foreach (var kv in trustDeltas) contacts.Trust(kv.Key, kv.Value);

            if (exposed) narrativeState["org_exposed"] = "True";
            if (exposed)
            {
                int floor = int.Parse(narrativeState["difficulty_floor"]);
                narrativeState["difficulty_floor"] = Mathf.Min(10, floor + 2).ToString();
            }
            if (!string.IsNullOrEmpty(alliance)) narrativeState["alliance"] = alliance;

            var delta = new Dictionary<string, string>
            {
                { "completed_missions", missionId },
                { "template_last", templateId },
                { "org_exposed", narrativeState["org_exposed"] },
                { "alliance", narrativeState["alliance"] },
                { "difficulty_floor", narrativeState["difficulty_floor"] }
            };

            foreach (var kv in contacts.TrustWeights())
                delta["trust:" + kv.Key] = kv.Value.ToString("R");

            ledger.Commit(missionId, behavior.Digest(), delta);
        }

        public ReplayResult Reconcile()
        {
            var initial = new Dictionary<string, string>
            {
                { "org_exposed", "False" },
                { "alliance", "neutral" },
                { "difficulty_floor", "1" }
            };
            return ledger.Replay(initial);
        }
    }
}
