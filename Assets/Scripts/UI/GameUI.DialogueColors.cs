using StreetCat.Narrative;
using TMPro;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        LineSpeaker dialogueInkKind = LineSpeaker.Character;

        public void RefreshDialogueFontColors()
        {
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
                    if (labels[i] != null) labels[i].color = colors.choice;
            }
        }

        void ApplyDialogueBodyColor(LineSpeaker kind)
        {
            if (bodyText == null) return;
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
