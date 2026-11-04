using UnityEngine;

namespace ZSP.Narrative
{
    using ZSP.AI;
    using ZSP.Player;
    using ZSP.UI;

    /// <summary>
    /// Runtime bridge between the story content, SIS and the scene.
    /// Keeps the interim three-mission slice playable while the official
    /// Season One episodes are re-keyed (see ZSP_SEASON_01_STORY_BIBLE.md).
    /// </summary>
    public class MissionDirector : MonoBehaviour
    {
        [Header("Integration")]
        public StealthSystem stealthSystem;
        public EnemySpawner enemySpawner;
        public HUDController hud;
        public PlayerHealth playerHealth;

        [Header("Mission")]
        public string missionId = "M1";
        public bool autoStartFirstMission = false;

        [Header("Operative Identity")]
        public OperativeIdentity identity = new OperativeIdentity();

        private StoryContent _content;
        private ShadowIntelligenceSystem _sis;
        private MissionConditions _conditions;
        private float _stealthBase = 0.65f;
        private bool _stealthBaseCaptured;

        public MissionConditions Conditions => _conditions;
        public ShadowIntelligenceSystem Sis => _sis;

        private void Awake()
        {
            _content = StoryContent.Load();
            _sis = new ShadowIntelligenceSystem(_content);
            if (stealthSystem != null)
            {
                _stealthBase = stealthSystem.baseDetectionRate;
                _stealthBaseCaptured = true;
            }
            if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        }

        private void Start()
        {
            if (autoStartFirstMission) BeginMission(missionId);
        }

        public MissionConditions BeginMission(string id)
        {
            missionId = id;
            _conditions = _sis.ResolveMission(id);
            if (hud != null && _conditions.Objectives.Count > 0) hud.SetObjective(OISTokens.Replace(_conditions.Objectives[0], identity));
            if (stealthSystem != null)
            {
                var baseRate = _stealthBaseCaptured ? _stealthBase : 0.65f;
                stealthSystem.baseDetectionRate = Mathf.Min(1.5f, baseRate + _conditions.Detection * 0.05f);
            }
            if (enemySpawner != null)
            {
                var count = Mathf.Clamp(2 + _conditions.Reinforcements, 2, 12);
                enemySpawner.Spawn(count);
            }
            foreach (var note in _conditions.Notes) Debug.Log("[SIS] " + note);
            Debug.Log($"[ZSP] Mission {missionId} resolved: detection {_conditions.Detection}, armour {_conditions.Armor}, strategy {_conditions.Strategy}, reinforcements {_conditions.Reinforcements}.");
            return _conditions;
        }

        public void RecordAction(string action, string target, float value)
        {
            _sis.Record(missionId, action, target, value);
        }

        public void ApplyEffects(StoryEffect effects)
        {
            _sis.ApplyEffects(effects, missionId);
            if (effects != null && effects.health != 0 && playerHealth != null)
            {
                if (effects.health < 0) playerHealth.Damage(-effects.health);
                else playerHealth.Heal(effects.health);
            }
        }

        public void CompleteMission(string ending = null)
        {
            _sis.CommitMission(missionId, ending);
            var chain = _sis.VerifyChain();
            Debug.Log($"[SIS] {missionId} committed; chain {(chain.ok ? "OK" : "BROKEN")} {chain.error}");
            Save();
        }

        public void Save()
        {
            var record = new GameSaveRecord
            {
                missionIndex = _sis.State.completed.Count,
                ending = endingFromRecord(),
                currentMissionId = missionId,
                nextMission = string.Empty,
                health = playerHealth != null ? playerHealth.health : 10,
                maxHealth = playerHealth != null ? playerHealth.maxHealth : 10,
                credits = 0,
                identity = identity,
            };
            SISSaveSystem.Save(record, _sis);
            Debug.Log("[SIS] Save written to " + SISSaveSystem.SavePath);
        }

        public bool Load()
        {
            if (SISSaveSystem.Load(_content, out var record, out var sis))
            {
                _sis = sis;
                missionId = string.IsNullOrEmpty(record.nextMission) ? record.currentMissionId : record.nextMission;
                if (record.identity != null) identity = record.identity;
                if (playerHealth != null)
                {
                    playerHealth.maxHealth = record.maxHealth;
                    playerHealth.health = record.health;
                }
                return true;
            }
            return false;
        }

        private string endingFromRecord()
        {
            foreach (var e in _sis.State.endings) if (!string.IsNullOrEmpty(e.ending)) return e.ending;
            return null;
        }
    }
}
