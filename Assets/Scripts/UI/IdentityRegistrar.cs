using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZSP.UI
{
    using ZSP.Narrative;

    /// <summary>
    /// First-launch identity registration for the Operative Identity System.
    /// Tinashe Mukasa remains the fixed protagonist; the player chooses display name,
    /// callsign and optional nickname. Bind confirms against OperativeIdentity.
    /// </summary>
    public class IdentityRegistrar : MonoBehaviour
    {
        [Header("Inputs")]
        public InputField displayNameField;
        public InputField callsignField;
        public InputField nicknameField;

        [Header("Readout")]
        public Text previewText;
        public Text errorText;
        public Button confirmButton;

        [Header("Binding")]
        public MissionDirector director;
        public bool autoBindOnStart = true;
        public bool beginMissionOnConfirm = false;
        public string missionToBeginOnConfirm = "M1";

        public event Action<OperativeIdentity> Confirmed;

        private void Start()
        {
            if (autoBindOnStart && director == null)
                director = FindObjectOfType<MissionDirector>();

            if (director != null)
            {
                if (displayNameField != null) displayNameField.text = director.identity.displayName;
                if (callsignField != null) callsignField.text = director.identity.callsign;
                if (nicknameField != null) nicknameField.text = director.identity.nickname;
            }

            if (displayNameField != null) displayNameField.onValueChanged.AddListener(_ => RefreshPreview());
            if (callsignField != null) callsignField.onValueChanged.AddListener(_ => RefreshPreview());
            if (nicknameField != null) nicknameField.onValueChanged.AddListener(_ => RefreshPreview());
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);

            RefreshPreview();
        }

        private OperativeIdentity BuildIdentity()
        {
            var display = displayNameField != null ? displayNameField.text : OperativeIdentity.DefaultDisplay;
            var callsign = callsignField != null && !string.IsNullOrWhiteSpace(callsignField.text) ? callsignField.text : OperativeIdentity.DefaultCallsign;
            var nickname = nicknameField != null ? nicknameField.text : string.Empty;
            return new OperativeIdentity(callsign, display, nickname);
        }

        public void RefreshPreview()
        {
            try
            {
                var identity = BuildIdentity();
                if (previewText != null)
                {
                    previewText.text =
                        "PLAYER OPERATIVE PROFILE\n" +
                        $"  callsign       {identity.callsign}\n" +
                        $"  display name   {identity.displayName}\n" +
                        $"  nickname       {(string.IsNullOrEmpty(identity.nickname) ? "-" : identity.nickname)}\n" +
                        "  protagonist    Tinashe Mukasa\n" +
                        "  campaign       The Black Ledger";
                }
                if (errorText != null) errorText.text = string.Empty;
                if (confirmButton != null) confirmButton.interactable = true;
            }
            catch (ArgumentException ex)
            {
                if (previewText != null) previewText.text = string.Empty;
                if (errorText != null) errorText.text = ex.Message;
                if (confirmButton != null) confirmButton.interactable = false;
            }
        }

        public void OnConfirm()
        {
            OperativeIdentity identity;
            try
            {
                identity = BuildIdentity();
            }
            catch (ArgumentException ex)
            {
                if (errorText != null) errorText.text = ex.Message;
                return;
            }
            if (director != null)
            {
                director.identity = identity;
                director.Save();
                if (beginMissionOnConfirm && !string.IsNullOrEmpty(missionToBeginOnConfirm))
                    director.BeginMission(missionToBeginOnConfirm);
            }
            Confirmed?.Invoke(identity);
            gameObject.SetActive(false);
            Debug.Log($"[OIS] Identity registered: {identity.callsign} ({identity.displayName})");
        }
    }
}
