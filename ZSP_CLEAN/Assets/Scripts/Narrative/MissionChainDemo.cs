using System.Collections.Generic;
using UnityEngine;

namespace ZSP.Narrative
{
    /// <summary>
    /// Tiny in-editor demonstration of the Adaptive Mission Chain. It records a
    /// short behaviour history, generates missions, commits consequences and
    /// reconciles the ledger. Use it to verify the system before wiring it into
    /// the real mission director.
    /// </summary>
    public class MissionChainDemo : MonoBehaviour
    {
        [TextArea(4, 12)] public string output;

        private void Start()
        {
            var chain = new AdaptiveMissionChain();

            chain.RecordBehavior("M000",
                new BehaviorEvent("", ActionType.StealthTakedown, "guard_a"),
                new BehaviorEvent("", ActionType.StealthAvoid, "patrol_b"),
                new BehaviorEvent("", ActionType.Interrogate, "courier"));

            MissionInstance first = chain.GenerateNextMission();
            Log(chain, first);
            chain.CompleteMission(first.missionId, first.templateId,
                new Dictionary<string, float> { { "C1", 0.15f } }, exposed: false, alliance: "neutral");

            chain.RecordBehavior(first.missionId,
                new BehaviorEvent("", ActionType.Gunfight, "compound"),
                new BehaviorEvent("", ActionType.VehicleChase, "highway"),
                new BehaviorEvent("", ActionType.TrustContact, "C1"));

            MissionInstance second = chain.GenerateNextMission();
            Log(chain, second);
            chain.CompleteMission(second.missionId, second.templateId,
                new Dictionary<string, float> { { "C1", 0.2f }, { "C2", 0.1f } }, exposed: true, alliance: "reform");

            MissionInstance third = chain.GenerateNextMission();
            Log(chain, third);

            ReplayResult replay = chain.Reconcile();
            output += "\n--- Replay ---\n";
            output += "Chain valid: " + chain.ledger.VerifyChain() + "\n";
            output += "Completed: " + string.Join(", ", replay.completed) + "\n";
            output += "Contradictions: " + replay.contradictions.Count + "\n";
            foreach (var kv in replay.state)
                output += "  " + kv.Key + " = " + kv.Value + "\n";

            Debug.Log("[ZSP] Mission Chain Demo\n" + output);
        }

        private void Log(AdaptiveMissionChain chain, MissionInstance mission)
        {
            output += $"Mission {mission.missionId}: {mission.displayName} " +
                      $"(difficulty {mission.difficulty}, detection {mission.enemyDetection}, armor {mission.enemyArmor})\n";
            foreach (string objective in mission.objectives)
                output += "  objective: " + objective + "\n";
            foreach (string note in mission.notes)
                output += "  note: " + note + "\n";
        }
    }
}
