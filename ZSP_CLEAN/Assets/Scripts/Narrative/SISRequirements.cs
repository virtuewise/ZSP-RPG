using System;
using System.Linq;

namespace ZSP.Narrative
{
    /// <summary>
    /// Requirement grammar shared by the Python and Unity SIS implementations.
    /// Supported forms:
    ///   flag:<id>            intel:<id>      route:<id>
    ///   trust:<id>:<0..1>    witness:<id>:<status>  keychar:<id>:<status>
    ///   faction:<id>:<field>:<value>
    ///   contact_available:<id>  contact_compromised:<id>
    ///   any:<req>,<req>,...
    /// </summary>
    public static class SISRequirements
    {
        public static bool RequirementMet(string req, ShadowIntelligenceSystem sis)
        {
            if (string.IsNullOrWhiteSpace(req)) return true;
            if (req.StartsWith("any:", StringComparison.OrdinalIgnoreCase))
                return req.Substring(4).Split(',').Any(part => RequirementMet(part, sis));

            var parts = req.Split(':');
            var kind = parts[0];
            if (parts.Length < 2) return false;

            switch (kind)
            {
                case "flag": return sis.HasFlag(parts[1]);
                case "intel": return sis.HasIntel(parts[1]);
                case "route": return sis.HasFlag("route_" + parts[1]);
                case "trust": return parts.Length >= 3 && sis.Trust(parts[1]) >= float.Parse(parts[2]);
                case "witness": return parts.Length >= 3 && sis.WitnessStatus(parts[1]) == parts[2];
                case "keychar": return parts.Length >= 3 && sis.KeyCharStatus(parts[1]) == parts[2];
                case "contact_available": return sis.ContactAvailable(parts[1]);
                case "contact_compromised": return sis.IsCompromised(parts[1]);
                case "faction":
                    if (parts.Length < 4) return false;
                    var fid = parts[1];
                    var field = parts[2];
                    var expected = string.Join(":", parts.Skip(3));
                    return field switch
                    {
                        "influence" => sis.FactionInt(fid, f => f.influence) >= int.Parse(expected),
                        "heat" => sis.FactionInt(fid, f => f.heat) >= int.Parse(expected),
                        "exposed" => sis.FactionBool(fid, f => f.exposed) == bool.Parse(expected),
                        "alliance" => string.Equals(sis.FactionString(fid, f => f.alliance), expected, StringComparison.OrdinalIgnoreCase),
                        _ => false,
                    };
                default: return false;
            }
        }

        public static bool VariantMet(StoryVariant variant, ShadowIntelligenceSystem sis)
            => variant != null && (variant.requirements == null || variant.requirements.All(r => RequirementMet(r, sis)));
    }
}
