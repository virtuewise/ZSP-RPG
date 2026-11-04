using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ZSP.Narrative
{
    [Serializable]
    public class StorySeason { public string id; public string title; public string logline; public string disclaimer; }

    [Serializable]
    public class StoryCharacter { public string id; public string name; public string role; public string faction; public int age; public string bio; public float defaultTrust; public List<string> tags = new(); }

    [Serializable]
    public class StoryFaction { public string id; public string name; public string kind; public string description; public int influence; public int heat; public bool exposed; public string alliance; }

    [Serializable]
    public class StoryContact { public string id; public string name; public float centrality; public float trust; public bool available; }

    [Serializable]
    public class StoryIntel { public string id; public string name; public string description; public string source; public List<string> tags = new(); }

    [Serializable]
    public class StoryWitness { public string id; public string name; public string faction; public string role; public float loyalty; public float fear; public bool canTestify; public string description; }

    [Serializable]
    public class StoryEpilogue { public string id; public string title; public string text; }

    [Serializable]
    public class StoryLocationRoute { public string id; public string name; public string kind; public int baseDifficulty; public int stealth; public int speed; public int risk; public string description; }

    [Serializable]
    public class StoryLocation { public string id; public string name; public string region; public string description; public List<StoryLocationRoute> routes = new(); }

    [Serializable]
    public class StoryCheck { public string stat; public int difficulty; public int bonus; }

    [Serializable]
    public class StoryTrustDelta { public string contact; public float delta; }

    [Serializable]
    public class StoryFactionDelta { public string id; public int influence; public int heat; public bool exposed; public string alliance; }

    [Serializable]
    public class StoryWitnessDelta { public string id; public string status; public bool cooperative; }

    [Serializable]
    public class StoryKeyCharDelta { public string id; public string status; }

    [Serializable]
    public class StoryBehaviorEvent { public string action; public string target; public float value = 1f; }

    [Serializable]
    public class StoryEffect
    {
        public List<StoryTrustDelta> trust = new();
        public List<StoryFactionDelta> factions = new();
        public List<string> intel = new();
        public List<string> flags = new();
        public List<StoryWitnessDelta> witnesses = new();
        public List<StoryKeyCharDelta> keyCharacters = new();
        public int credits;
        public int health;
        public string ending;
        public List<StoryBehaviorEvent> behavior = new();
    }

    [Serializable]
    public class StoryVariant { public List<string> requirements = new(); public string text; }

    [Serializable]
    public class StoryBranch { public string text; public StoryEffect effects; public string next; }

    [Serializable]
    public class StoryConditionalEffect { public List<string> requirements = new(); public StoryEffect effects; }

    [Serializable]
    public class StoryChoice
    {
        public string id;
        public string text;
        public List<string> requirements = new();
        public StoryEffect effects;
        public string next;
        public StoryCheck check;
        public StoryBranch success;
        public StoryBranch failure;
        public List<StoryConditionalEffect> conditionalEffects = new();
    }

    [Serializable]
    public class StoryArea
    {
        public string id;
        public string name;
        public string text;
        public string stat;
        public int difficulty;
        public List<string> intel = new();
        public StoryEffect effects;
        public StoryEffect failureEffects;
    }

    [Serializable]
    public class StoryBeat
    {
        public string id;
        public string type;
        public string speaker;
        public string text;
        public List<string> routes = new();
        public int maxSearches;
        public List<StoryArea> areas = new();
        public List<StoryChoice> choices = new();
        public List<StoryVariant> variants = new();
        public int enemies;
        public string next;
        public string nextMission;
    }

    [Serializable]
    public class StoryMission
    {
        public string id;
        public int order;
        public string title;
        public string location;
        public string premise;
        public List<string> invariants = new();
        public List<string> objectives = new();
        public List<string> contacts = new();
        public List<string> witnesses = new();
        public List<string> keyCharacters = new();
        public List<string> endings = new();
        public string start;
        public List<StoryBeat> beats = new();
    }

    [Serializable]
    internal class StorySeasonFile { public int schemaVersion; public StorySeason season; public List<StoryCharacter> characters = new(); public List<StoryFaction> factions = new(); public List<StoryContact> contacts = new(); }

    [Serializable]
    internal class StoryLocationsFile { public int schemaVersion; public List<StoryLocation> locations = new(); }

    [Serializable]
    internal class StoryCodexFile { public int schemaVersion; public List<StoryIntel> intel = new(); public List<StoryWitness> witnesses = new(); public List<StoryEpilogue> epilogues = new(); }

    [Serializable]
    internal class StoryMissionFile { public int schemaVersion; public List<StoryMission> missions = new(); }

    /// <summary>
    /// Loads and indexes the Season 1 content from Resources/Content.
    /// The same JSON files are consumed by the Python SIS prototype.
    /// </summary>
    public sealed class StoryContent
    {
        public StorySeason Season;
        public readonly Dictionary<string, StoryCharacter> Characters = new();
        public readonly Dictionary<string, StoryFaction> Factions = new();
        public readonly Dictionary<string, StoryContact> Contacts = new();
        public readonly Dictionary<string, StoryLocation> Locations = new();
        public readonly Dictionary<string, StoryIntel> Intel = new();
        public readonly Dictionary<string, StoryWitness> Witnesses = new();
        public readonly Dictionary<string, StoryEpilogue> Epilogues = new();
        public readonly Dictionary<string, StoryMission> Missions = new();
        public readonly Dictionary<string, Dictionary<string, StoryBeat>> Beats = new();
        public List<string> MissionOrder = new();

        public static StoryContent Load()
        {
            var content = new StoryContent();
            var season = LoadJson<StorySeasonFile>("Content/season");
            if (season == null) throw new InvalidDataException("Resources/Content/season.json could not be loaded");
            content.Season = season.season;
            foreach (var c in season.characters) content.Characters[c.id] = c;
            foreach (var f in season.factions) content.Factions[f.id] = f;
            foreach (var c in season.contacts) content.Contacts[c.id] = c;

            var locations = LoadJson<StoryLocationsFile>("Content/locations");
            if (locations == null) throw new InvalidDataException("Resources/Content/locations.json could not be loaded");
            foreach (var l in locations.locations) content.Locations[l.id] = l;

            var codex = LoadJson<StoryCodexFile>("Content/codex");
            if (codex == null) throw new InvalidDataException("Resources/Content/codex.json could not be loaded");
            foreach (var i in codex.intel) content.Intel[i.id] = i;
            foreach (var w in codex.witnesses) content.Witnesses[w.id] = w;
            foreach (var e in codex.epilogues) content.Epilogues[e.id] = e;

            foreach (var asset in Resources.LoadAll<TextAsset>("Content/missions"))
            {
                var file = JsonUtility.FromJson<StoryMissionFile>(asset.text);
                if (file == null) continue;
                foreach (var mission in file.missions)
                {
                    content.Missions[mission.id] = mission;
                    var beats = new Dictionary<string, StoryBeat>();
                    foreach (var beat in mission.beats) beats[beat.id] = beat;
                    content.Beats[mission.id] = beats;
                }
            }
            if (content.Missions.Count == 0) throw new InvalidDataException("No missions found under Resources/Content/missions");
            content.MissionOrder = content.Missions.Values.OrderBy(m => m.order).Select(m => m.id).ToList();
            content.Validate();
            return content;
        }

        private static T LoadJson<T>(string resourcePath) where T : class
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            return asset == null ? null : JsonUtility.FromJson<T>(asset.text);
        }

        public StoryMission Mission(string id) => Missions.TryGetValue(id, out var mission) ? mission : null;

        public StoryBeat Beat(string missionId, string beatId)
            => Beats.TryGetValue(missionId, out var beats) && beats.TryGetValue(beatId, out var beat) ? beat : null;

        public StoryLocation LocationFor(string missionId)
        {
            var mission = Mission(missionId);
            return mission != null && Locations.TryGetValue(mission.location, out var location) ? location : null;
        }

        public string CharacterName(string id) => Characters.TryGetValue(id, out var c) ? c.name : id;

        public void Validate()
        {
            foreach (var mission in Missions.Values)
            {
                if (string.IsNullOrEmpty(mission.id) || string.IsNullOrEmpty(mission.start))
                    throw new InvalidDataException("Mission missing id/start");
                if (!Beats.ContainsKey(mission.id) || !Beats[mission.id].ContainsKey(mission.start))
                    throw new InvalidDataException($"Mission {mission.id} start beat missing");
                if (!mission.beats.Any(b => b.type == "mission_end"))
                    throw new InvalidDataException($"Mission {mission.id} has no mission_end beat");
                foreach (var cid in mission.contacts ?? new List<string>())
                    if (!Contacts.ContainsKey(cid)) throw new InvalidDataException($"Mission {mission.id}: unknown contact {cid}");
                foreach (var wid in mission.witnesses ?? new List<string>())
                    if (!Witnesses.ContainsKey(wid)) throw new InvalidDataException($"Mission {mission.id}: unknown witness {wid}");
                foreach (var kid in mission.keyCharacters ?? new List<string>())
                    if (!Characters.ContainsKey(kid)) throw new InvalidDataException($"Mission {mission.id}: unknown key character {kid}");

                var knownBeats = Beats[mission.id];
                foreach (var beat in mission.beats)
                {
                    if (!string.IsNullOrEmpty(beat.next) && !knownBeats.ContainsKey(beat.next))
                        throw new InvalidDataException($"Mission {mission.id} beat {beat.id}: unknown next beat {beat.next}");
                    if (!string.IsNullOrEmpty(beat.nextMission) && !Missions.ContainsKey(beat.nextMission))
                        throw new InvalidDataException($"Mission {mission.id} beat {beat.id}: unknown next mission {beat.nextMission}");

                    if (beat.choices == null) continue;
                    foreach (var choice in beat.choices)
                    {
                        if (!string.IsNullOrEmpty(choice.next) && !knownBeats.ContainsKey(choice.next))
                            throw new InvalidDataException($"Mission {mission.id} choice {beat.id}/{choice.id}: unknown next beat {choice.next}");
                        ValidateBranchTarget(mission.id, beat.id, choice.id, "success", choice.success, knownBeats);
                        ValidateBranchTarget(mission.id, beat.id, choice.id, "failure", choice.failure, knownBeats);
                    }
                }
            }
            if (!Missions.ContainsKey("M1") || !Missions.ContainsKey("M2") || !Missions.ContainsKey("M3"))
                throw new InvalidDataException("Season 1 requires M1/M2/M3");
            if (Missions["M3"].endings == null || Missions["M3"].endings.Count == 0)
                throw new InvalidDataException("M3 must have endings");
        }

        private static void ValidateBranchTarget(string missionId, string beatId, string choiceId, string branchName,
            StoryBranch branch, Dictionary<string, StoryBeat> knownBeats)
        {
            if (branch == null || string.IsNullOrEmpty(branch.next)) return;
            if (!knownBeats.ContainsKey(branch.next))
                throw new InvalidDataException($"Mission {missionId} choice {beatId}/{choiceId}/{branchName}: unknown next beat {branch.next}");
        }
    }
}
