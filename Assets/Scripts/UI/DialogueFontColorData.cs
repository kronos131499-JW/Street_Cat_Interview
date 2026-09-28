using UnityEngine;

namespace StreetCat.UI
{
    [CreateAssetMenu(menuName = "Street Cat/Dialogue Font Colors", fileName = "DialogueFontColors")]
    public sealed class DialogueFontColorData : ScriptableObject
    {
        public Color speakerName = new Color(0.96f, 0.94f, 0.90f, 1f);
        public Color dialogue = new Color(0.96f, 0.94f, 0.90f, 1f);
        public Color narration = new Color(0.68f, 0.66f, 0.62f, 1f);
        public Color inner = new Color(0.76f, 0.84f, 0.90f, 1f);
        public Color system = new Color(0.88f, 0.76f, 0.42f, 1f);
        public Color status = new Color(0.68f, 0.66f, 0.62f, 1f);
        public Color clickHint = new Color(0.68f, 0.66f, 0.62f, 0.55f);
        public Color choice = new Color(0.96f, 0.94f, 0.90f, 1f);

        public void ApplyDefaults()
        {
            speakerName = VnTheme.TextPrimary;
            dialogue = VnTheme.TextPrimary;
            narration = VnTheme.TextMuted;
            inner = VnTheme.TextInner;
            system = VnTheme.TextSystem;
            status = VnTheme.TextMuted;
            clickHint = new Color(VnTheme.TextMuted.r, VnTheme.TextMuted.g, VnTheme.TextMuted.b, 0.55f);
            choice = VnTheme.TextPrimary;
        }
    }
}
