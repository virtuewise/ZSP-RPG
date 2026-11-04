using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ZSP.Narrative
{
    /// <summary>
    /// ZSP Operative Identity System (OIS) — the player-facing identity layer.
    /// Tinashe Mukasa remains the fixed protagonist; the player chooses how the
    /// campaign identifies them. Display strings are substituted at render time
    /// and are never hashed into the SIS ledger.
    /// </summary>
    [Serializable]
    public class OperativeIdentity
    {
        public string displayName = "Tinashe Mukasa";
        public string callsign = "SHADOW-07";
        public string nickname = "";

        public const string DefaultDisplay = "Tinashe Mukasa";
        public const string DefaultCallsign = "SHADOW-07";
        public const string DefaultNickname = "";

        private static readonly string[] Blocked = { "faggot", "nigger", "nigga", "kike", "tranny", "spic", "chink", "retard", "hitler" };
        private static readonly Regex CallsignPattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9\- ]{0,11}$", RegexOptions.Compiled);

        public OperativeIdentity() { }

        public OperativeIdentity(string callsign, string displayName = null, string nickname = null)
        {
            this.displayName = NormalizeDisplay(displayName ?? DefaultDisplay);
            this.callsign = NormalizeCallsign(callsign ?? DefaultCallsign);
            this.nickname = NormalizeNickname(nickname ?? DefaultNickname);
        }

        public static OperativeIdentity Default => new OperativeIdentity { displayName = DefaultDisplay, callsign = DefaultCallsign, nickname = DefaultNickname };

        public static string NormalizeDisplay(string value)
        {
            value = Clean(value);
            if (value.Length == 0) return DefaultDisplay;
            if (value.Length > 28) throw new ArgumentException("Display name: 28 characters max.");
            foreach (var ch in value)
            {
                if (!(char.IsLetter(ch) || ch == ' ' || ch == '\'' || ch == '’' || ch == '-' || ch == '.'))
                    throw new ArgumentException("Display name: letters, spaces, apostrophes and hyphens only.");
            }
            CheckBlocked(value);
            return value;
        }

        public static string NormalizeCallsign(string value)
        {
            value = Clean(value).ToUpperInvariant();
            if (value.Length == 0) return DefaultCallsign;
            if (!CallsignPattern.IsMatch(value))
                throw new ArgumentException("Callsign: A-Z, 0-9, hyphen and space only; 12 characters max.");
            CheckBlocked(value);
            return value;
        }

        public static string NormalizeNickname(string value)
        {
            value = Clean(value);
            if (value.Length == 0) return DefaultNickname;
            if (value.Length > 14) throw new ArgumentException("Nickname: 14 characters max.");
            CheckBlocked(value);
            return value;
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.Normalize(NormalizationForm.FormKC))
            {
                if (char.GetUnicodeCategory(ch) == UnicodeCategory.Format) continue; // zero-width / format characters
                sb.Append(ch);
            }
            var cleaned = sb.ToString().Trim();
            var collapsed = new StringBuilder(cleaned.Length);
            var lastWasSpace = false;
            foreach (var ch in cleaned)
            {
                var isSpace = char.IsWhiteSpace(ch);
                if (isSpace && lastWasSpace) continue;
                collapsed.Append(isSpace ? ' ' : ch);
                lastWasSpace = isSpace;
            }
            return collapsed.ToString();
        }

        private static void CheckBlocked(string value)
        {
            var lowered = value.ToLowerInvariant().Replace(" ", string.Empty).Replace("-", string.Empty);
            foreach (var term in Blocked)
                if (lowered.Contains(term))
                    throw new ArgumentException("That name is not available.");
        }
    }

    public static class OISTokens
    {
        public static string Replace(string text, OperativeIdentity identity)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var nick = string.IsNullOrWhiteSpace(identity.nickname) ? identity.callsign : identity.nickname;
            return text
                .Replace("{OIS_DISPLAY}", identity.displayName)
                .Replace("{OIS_FULL}", identity.displayName)
                .Replace("{OIS_CALLSIGN}", identity.callsign)
                .Replace("{OIS_NICKNAME}", nick);
        }
    }
}
