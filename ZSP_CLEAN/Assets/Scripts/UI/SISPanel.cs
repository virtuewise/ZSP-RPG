using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ZSP.UI
{
    using ZSP.Narrative;

    /// <summary>
    /// Debug/audit panel for the Shadow Intelligence System.
    /// Hidden during normal play; shown with F9 or from the pause menu.
    /// </summary>
    public class SISPanel : MonoBehaviour
    {
        public Text titleText;
        public Text bodyText;
        public KeyCode toggleKey = KeyCode.F9;

        private ShadowIntelligenceSystem _sis;
        private bool _visible;

        private void Awake()
        {
            SetVisible(_visible);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!_visible);
            if (_visible && _sis != null) Refresh();
        }

        public void Bind(ShadowIntelligenceSystem sis)
        {
            _sis = sis;
            Refresh();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (titleText != null) titleText.gameObject.SetActive(visible);
            if (bodyText != null) bodyText.gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            if (_sis == null)
            {
                if (bodyText != null) bodyText.text = "No SIS bound.";
                return;
            }
            if (titleText != null) titleText.text = "SHADOW INTELLIGENCE — ADAPTIVE ENEMY DOCTRINE (audit)";
            var sb = new StringBuilder();
            sb.AppendLine("CONTACTS");
            foreach (var t in _sis.State.trust)
            {
                var compromised = _sis.IsCompromised(t.id);
                sb.AppendLine($"  {t.id,-10} trust {t.value:F2}  exposure {_sis.Exposure(t.id):F2}{(compromised ? "  EXPOSED" : "")}");
            }
            sb.AppendLine();
            sb.AppendLine("WITNESSES");
            foreach (var w in _sis.State.witnesses) sb.AppendLine($"  {w.id,-10} {w.status,-9} cooperative={w.cooperative}");
            sb.AppendLine();
            sb.AppendLine("FACTIONS");
            foreach (var f in _sis.State.factions) sb.AppendLine($"  {f.id,-12} influence {f.influence,2}  heat {f.heat,2}  exposed={f.exposed}");
            sb.AppendLine();
            sb.AppendLine("COMPLETED");
            sb.AppendLine("  " + string.Join(", ", _sis.State.completed));
            sb.AppendLine();
            sb.AppendLine("FLAGS");
            sb.AppendLine("  " + string.Join(", ", _sis.State.flags));
            if (bodyText != null) bodyText.text = sb.ToString();
        }
    }
}
