using System.Text;
using UnityEngine;

namespace ZSP.Narrative
{
    /// <summary>
    /// Play-Mode console demonstration of the Shadow Intelligence System.
    /// Runs a deterministic three-mission scenario and verifies the ledger.
    /// Attach to any GameObject and press Play; no scene setup is required.
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
            sis.Record("M1", "trust_contact", "nyasha", 1);
            sis.Record("M1", "approach", "service_roof", 2);
            sis.Record("M1", "investigate", "office", 1);
            sis.Record("M1", "investigate", "loading_bay", 1);
            sis.Record("M1", "interrogate", "tendai", 1);
            sis.ApplyEffects(new StoryEffect
            {
                trust = new() { new StoryTrustDelta { contact = "nyasha", delta = 0.15f } },
                intel = new() { "archive_fragment", "nightjar_manifest" },
                flags = new() { "accepted_job", "tendai_cooperative", "archive_taken", "clean_exfil" },
                witnesses = new() { new StoryWitnessDelta { id = "tendai", status = "spared", cooperative = true } },
                keyCharacters = new() { new StoryKeyCharDelta { id = "tendai", status = "alive" } },
            }, "M1");
            sis.CommitMission("M1");

            var m2 = LogMissionStart("M2", sis);
            sis.Record("M2", "trust_contact", "nyasha", 1);
            sis.Record("M2", "trust_contact", "tariro", 1);
            sis.Record("M2", "approach", "service_corridor", 2);
            sis.Record("M2", "investigate", "locker", 1);
            sis.Record("M2", "interrogate", "fingers", 1);
            sis.ApplyEffects(new StoryEffect
            {
                trust = new() { new StoryTrustDelta { contact = "tariro", delta = 0.3f }, new StoryTrustDelta { contact = "nyasha", delta = 0.1f } },
                intel = new() { "payment_ledger", "fingers_testimony", "chiweshe_memo" },
                flags = new() { "partner_tariro", "fingers_cooperative", "fingers_spared", "ledger_published" },
                witnesses = new() { new StoryWitnessDelta { id = "fingers", status = "spared", cooperative = true } },
                factions = new() { new StoryFactionDelta { id = "nightjar", influence = -1, heat = 2, exposed = true, alliance = "" }, new StoryFactionDelta { id = "civic_front", influence = 2, heat = 1, exposed = false, alliance = "" } },
            }, "M2");
            sis.CommitMission("M2");

            var m3 = LogMissionStart("M3", sis);
            sis.Record("M3", "approach", "east_service", 2);
            sis.Record("M3", "stealth_avoid", "kaguvi_security", 1);
            sis.ApplyEffects(new StoryEffect
            {
                intel = new() { "shadow_protocol_core", "mhofu_keycard", "fingers_testimony" },
                flags = new() { "inside_quiet", "nyasha_captured", "tariro_targeted", "chose_expose" },
                witnesses = new() { new StoryWitnessDelta { id = "mhofu", status = "spared", cooperative = false } },
                ending = "expose",
            }, "M3");
            sis.CommitMission("M3", "expose");

            var chain = sis.VerifyChain();
            Debug.Log($"[SIS DEMO] Ledger height {sis.Ledger.Count}; chain {(chain.ok ? "OK" : "BROKEN")} {chain.error}");
            Debug.Log("[SIS DEMO] Final state: " + Snapshot(sis));

            if (overwriteSave)
            {
                var record = new GameSaveRecord { missionIndex = 3, ending = "expose", currentMissionId = "M3", health = 10, maxHealth = 10, credits = 0 };
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
