using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZSP.UI
{
    using ZSP.Narrative;

    /// <summary>
    /// Minimal dialogue runner for Season One episodes.
    /// Assign a line Text, a choice container and a choice button prefab.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public Text lineText;
        public Transform choiceRoot;
        public Button choiceButtonPrefab;
        public GameObject panelRoot;
        public OperativeIdentity identity = new OperativeIdentity();

        public event Action<StoryChoice> ChoiceSelected;

        public void ShowLine(string speaker, string text)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (lineText != null)
            {
                var resolved = string.IsNullOrEmpty(speaker) ? text : speaker + "\n\n" + text;
                lineText.text = OISTokens.Replace(resolved, identity);
                lineText.supportRichText = true;
            }
            ClearChoices();
        }

        public void ShowChoices(IList<StoryChoice> choices, ShadowIntelligenceSystem sis)
        {
            ClearChoices();
            if (choiceRoot == null || choiceButtonPrefab == null)
            {
                Debug.LogWarning("[ZSP] DialogueUI is missing choiceRoot or choiceButtonPrefab.");
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var available = true;
                if (choice.requirements != null && sis != null)
                {
                    foreach (var req in choice.requirements)
                    {
                        if (!SISRequirements.RequirementMet(req, sis)) { available = false; break; }
                    }
                }

                var button = Instantiate(choiceButtonPrefab, choiceRoot);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = OISTokens.Replace((available ? "▸ " : "✖ ") + choice.text, identity);
                button.interactable = available;
                if (available)
                {
                    var captured = choice;
                    button.onClick.AddListener(() => ChoiceSelected?.Invoke(captured));
                }
            }
        }

        public void ClearChoices()
        {
            if (choiceRoot == null) return;
            for (var i = choiceRoot.childCount - 1; i >= 0; i--)
            {
                var child = choiceRoot.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ClearChoices();
        }
    }
}
