using System.Text;
using UnityEngine;

namespace ZSP.Narrative
{
    /// <summary>
    /// Play-Mode console demonstration of the Shadow Intelligence System,
    /// re-keyed to Season One Episodes 01-03.
    /// </summary>
    public class ShadowIntelligenceDemo : MonoBehaviour
    {
        [Header("Demo")]
        public bool runOnStart = true;
        public bool overwriteSave = true;

        private void Start()
        {
            if (runOnStart) Run();
        }

        [ContextMenu("Run SIS Demo")]
        public void Run()
        {
            var content = StoryContent.Load();
            var sis = new ShadowIntelligenceSystem(content);

            LogMissionStart("M1", sis);
            sis.Record("M1", "trust_contact", "amara", 1);
            sis.Record("M1", "approach", "back_alley", 2);
            sis.Record("M1", "investigate", "locker", 1);
            sis.Record("M1", "interrogate", "lovemore", 1);
            sis.ApplyEffects(new StoryEffect
            {
                trust = new() { new StoryTrustDelta { contact = "amara", delta = 0.2f } },
                intel = new() { "activation_message", "cleaner_roster" },
                flags = new() { "accepted_message", "lovemore_saved", "evaded_flat_sweep" },
                witnesses = new() { new StoryWitnessDelta { id = "lovemore", status = "spared", cooperative = true } },
                keyCharacters = new() { new StoryKeyCharDelta { id = "mhara", status = "missing" } },
            }, "M1");
            sis.CommitMission("M1");

            LogMissionStart("M2", sis);
            sis.Record("M2", "trust_contact", "amara", 1);
            sis.Record("M2", "approach", "drain", 4);
            sis.Record("M2", "investigate", "phone", 1);
            sis.Record("M2", "investigate", "cleaner", 1);
            sis.ApplyEffects(new StoryEffect
            {
                trust = new() { new StoryTrustDelta { contact = "amara", delta = 0.1f } },
                intel = new() { "mhara_phone", "msg09_account", "cleaner_roster" },
                flags = new() { "recovered_mhara_phone", "msg09_traced", "lovemore_informant" },
                factions = new() { new StoryFactionDelta { id = "meridian", influence = 0, heat = 2, exposed = false, alliance = "" } },
            }, "M2");
            sis.CommitMission("M2");

            LogMissionStart("M3", sis);
            sis.Record("M3", "trust_contact", "amara", 1);
            sis.Record("M3", "stealth_avoid", "meridian_breach", 1);
            sis.ApplyEffects(new StoryEffect
            {
                trust = new() { new StoryTrustDelta { contact = "amara", delta = 0.2f } },
                intel = new() { "tafadzwa_tip", "safehouse_leak" },
                flags = new() { "tafadzwa_extracted", "completed_ep03" },
                witnesses = new() { new StoryWitnessDelta { id = "tafadzwa", status = "spared", cooperative = true } },
                ending = "interlude",
            }, "M3");
            sis.CommitMission("M3", "interlude");

            var chain = sis.VerifyChain();
            Debug.Log($"[SIS DEMO] Ledger height {sis.Ledger.Count}; chain {(chain.ok ? "OK" : "BROKEN")} {chain.error}");
            Debug.Log("[SIS DEMO] Final state: " + Snapshot(sis));

            if (overwriteSave)
            {
                var record = new GameSaveRecord { missionIndex = 3, ending = "interlude", currentMissionId = "M3", health = 10, maxHealth = 10, credits = 0 };
                SISSaveSystem.Save(record, sis);
                if (SISSaveSystem.Load(content, out var loaded, out var restored))
                {
                    Debug.Log($"[SIS DEMO] Save round-trip OK: {loaded.sis.ledger.Count} commits restored.");
                }
            }
        }

        private MissionConditions LogMissionStart(string missionId, ShadowIntelligenceSystem sis)
        {
            var conditions = sis.ResolveMission(missionId);
            var sb = new StringBuilder();
            sb.AppendLine("========================================================");
            sb.AppendLine($"MISSION {missionId}: {conditions.title}");
            sb.AppendLine(conditions.locationName);
            sb.AppendLine("Objectives:");
            foreach (var objective in conditions.Objectives) sb.AppendLine("  - " + objective);
            sb.AppendLine($"Enemy: detection {conditions.Detection}, armour {conditions.Armor}, strategy {conditions.Strategy}, reinforcements {conditions.Reinforcements} (adaptation {conditions.Adaptation}/10)");
            foreach (var witness in conditions.Witnesses)
                sb.AppendLine($"Witness {witness.name}: {witness.role} ({witness.effect})");
            foreach (var contact in conditions.Contacts)
                sb.AppendLine($"Contact {contact.name}: trust {contact.trust:F2}, exposure {contact.exposure:F2}{(contact.compromised ? " EXPOSED" : "")}{(contact.available ? "" : " UNAVAILABLE")}");
            foreach (var note in conditions.Notes) sb.AppendLine("  SIS: " + note);
            Debug.Log(sb.ToString());
            return conditions;
        }

        private static string Snapshot(ShadowIntelligenceSystem sis)
        {
            var s = sis.State;
            var sb = new StringBuilder();
            sb.Append("witnesses: ");
            foreach (var w in s.witnesses) sb.Append($"{w.id}={w.status}/{w.cooperative} ");
            sb.Append("contacts: ");
            foreach (var t in s.trust) sb.Append($"{t.id}={t.value:F2} ");
            sb.Append("flags: ").Append(string.Join(", ", s.flags));
            sb.Append(" intel: ").Append(string.Join(", ", s.intel));
            return sb.ToString();
        }
    }
}
