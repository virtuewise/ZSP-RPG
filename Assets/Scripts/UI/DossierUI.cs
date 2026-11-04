using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ZSP.UI
{
    using ZSP.Narrative;

    /// <summary>
    /// The diegetic dossier: the player's field report.
    /// Shows operative identity, campaign progress and SIS reputation derived data.
    /// Toggle with F8 by default; bind a ShadowIntelligenceSystem via BindSIS().
    /// </summary>
    public class DossierUI : MonoBehaviour
    {
        public Text dossierText;
        public GameObject panelRoot;
        public KeyCode toggleKey = KeyCode.F8;
        public OperativeIdentity identity = new OperativeIdentity();

        private ShadowIntelligenceSystem _sis;
        private bool _visible;

        private void Awake()
        {
            SetVisible(false);
        }

        private void Start()
        {
            var director = FindObjectOfType<ZSP.Narrative.MissionDirector>();
            if (director != null)
            {
                identity = director.identity;
                if (director.Sis is ShadowIntelligenceSystem sis) BindSIS(sis);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!_visible);
            if (_visible) Refresh();
        }

        public void BindSIS(ShadowIntelligenceSystem sis)
        {
            _sis = sis;
            Refresh();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (panelRoot != null) panelRoot.SetActive(visible);
            else if (dossierText != null) dossierText.gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            if (dossierText == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("PLAYER OPERATIVE PROFILE");
            sb.AppendLine();
            sb.AppendLine($"  callsign     {identity.callsign}");
            sb.AppendLine($"  display      {identity.displayName}");
            sb.AppendLine($"  campaign     The Black Ledger");
            sb.AppendLine("  protagonist  Tinashe Mukasa");
            sb.AppendLine();

            if (_sis != null)
            {
                var state = _sis.State;
                sb.AppendLine("FIELD REPORT");
                sb.AppendLine($"  episodes     {state.completed.Count}/12");
                var behaviour = _sis.Behavior;
                var stealth = behaviour.Count(b => b.action == "stealth_avoid" || b.action == "stealth_takedown");
                var gunfire = behaviour.Count(b => b.action == "gunfight");
                var spared = behaviour.Count(b => b.action == "witness_spared");
                var killed = behaviour.Count(b => b.action == "witness_killed");
                var style = gunfire > stealth ? "STYLE: LOUD" : stealth > gunfire ? "STYLE: QUIET" : "STYLE: UNREADABLE";
                var mercy = spared > killed ? "MERCY-MAJORITY" : killed > spared ? "NO SURVIVORS" : "UNDETERMINED";
                var heat = _sis.FactionInt("meridian", f => f.heat);
                var reputation = heat >= 4 ? "HUNTED" : heat >= 2 ? "KNOWN" : "UNKNOWN";
                sb.AppendLine($"  {style}   {mercy}");
                sb.AppendLine($"  reputation   {reputation}");
                sb.AppendLine();
                sb.AppendLine("  witnesses:   " + string.Join(", ", state.witnesses.ConvertAll(w => $"{w.id}:{w.status}")));
                sb.AppendLine("  completed:   " + string.Join(", ", state.completed));
                sb.AppendLine();
                sb.AppendLine("  SUBJECT: " + identity.callsign + " — STATUS: ACTIVE THREAT");
            }
            else
            {
                sb.AppendLine("FIELD REPORT: unavailable (no SIS bound)");
            }
            dossierText.text = sb.ToString();
        }
    }
}
