using System;
using System.IO;
using UnityEngine;

namespace ZSP.Narrative
{
    [Serializable]
    public class GameSaveRecord
    {
        public int version = 1;
        public int missionIndex;
        public string ending;
        public string currentMissionId;
        public string nextMission;
        public int health = 10;
        public int maxHealth = 10;
        public int credits;
        public OperativeIdentity identity;
        public SISSaveRecord sis;
    }

    /// <summary>
    /// JSON save system. Writes to Application.persistentDataPath so saves survive Android/iOS updates.
    /// </summary>
    public static class SISSaveSystem
    {
        private const string FileName = "zsp_save.json";

        public static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(GameSaveRecord record, ShadowIntelligenceSystem sis)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            record.version = 1;
            record.sis = sis.Export();
            File.WriteAllText(SavePath, JsonUtility.ToJson(record, true));
        }

        public static bool Load(StoryContent content, out GameSaveRecord record, out ShadowIntelligenceSystem sis)
        {
            record = null;
            sis = null;
            if (!File.Exists(SavePath)) return false;
            var json = File.ReadAllText(SavePath);
            record = JsonUtility.FromJson<GameSaveRecord>(json);
            if (record == null || record.sis == null) return false;
            record.identity = record.identity ?? OperativeIdentity.Default;
            sis = ShadowIntelligenceSystem.FromRecord(content, record.sis);
            return true;
        }

        public static bool Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            return true;
        }
    }
}
