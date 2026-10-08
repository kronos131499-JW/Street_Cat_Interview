using StreetCat.Narrative;
using TMPro;
using UnityEngine;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        LineSpeaker dialogueInkKind = LineSpeaker.Character;

        public void RefreshDialogueFontColors()
        {
            if (artPackParchmentActive)
            {
                ApplyParchmentDialogueColors();
                return;
            }
            var colors = DialogueFontColors.Current;
            if (colors == null) return;
            if (nameText != null) nameText.color = colors.speakerName;
            ApplyDialogueBodyColor(dialogueInkKind);
            if (statusText != null) statusText.color = colors.status;
            if (clickHintText != null) clickHintText.color = colors.clickHint;
            if (choiceRoot != null)
            {
                var labels = choiceRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (var i = 0; i < labels.Length; i++)
                    SetChoiceLabelColor(labels[i], colors.choice);
            }
        }

        void ApplyParchmentDialogueColors()
        {
            if (nameText != null) nameText.color = ArtPackInk;
            ApplyDialogueBodyColor(dialogueInkKind);
            if (statusText != null) statusText.color = ArtPackInkMuted;
            if (clickHintText != null) clickHintText.color = ArtPackInkMuted;
            if (choiceRoot != null)
            {
                var labels = choiceRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (var i = 0; i < labels.Length; i++)
                    SetChoiceLabelColor(labels[i], ArtPackInk);
            }
        }

        static void SetChoiceLabelColor(TextMeshProUGUI label, Color color)
        {
            if (label == null) return;
            var hover = label.GetComponentInParent<ChoiceHoverLabel>();
            if (hover != null)
                hover.SetIdle(color);
            else
                label.color = color;
        }

        void ApplyDialogueBodyColor(LineSpeaker kind)
        {
            if (bodyText == null) return;
            if (artPackParchmentActive)
            {
                switch (kind)
                {
                    case LineSpeaker.Narration:
                        bodyText.color = ArtPackInkMuted;
                        break;
                    case LineSpeaker.Inner:
                        bodyText.color = ArtPackInkInner;
                        break;
                    case LineSpeaker.System:
                        bodyText.color = ArtPackInkSystem;
                        break;
                    default:
                        bodyText.color = ArtPackInk;
                        break;
                }
                bodyText.ForceMeshUpdate(true);
                return;
            }
            var colors = DialogueFontColors.Current;
            switch (kind)
            {
                case LineSpeaker.Narration:
                    bodyText.color = colors.narration;
                    break;
                case LineSpeaker.Inner:
                    bodyText.color = colors.inner;
                    break;
                case LineSpeaker.System:
                    bodyText.color = colors.system;
                    break;
                default:
                    bodyText.color = colors.dialogue;
                    break;
            }
        }
    }
}
