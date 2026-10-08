using UnityEngine;

namespace StreetCat.UI
{
    [CreateAssetMenu(menuName = "Street Cat/Dialogue Font Colors", fileName = "DialogueFontColors")]
    public sealed class DialogueFontColorData : ScriptableObject
    {
        public static readonly Color Ink = new Color(42f / 255f, 18f / 255f, 2f / 255f, 1f);

        public Color speakerName = Ink;
        public Color dialogue = Ink;
        public Color narration = Ink;
        public Color inner = Ink;
        public Color system = Ink;
        public Color status = Ink;
        public Color clickHint = Ink;
        public Color choice = Ink;

        public void ApplyDefaults()
        {
            speakerName = Ink;
            dialogue = Ink;
            narration = Ink;
            inner = Ink;
            system = Ink;
            status = Ink;
            clickHint = Ink;
            choice = Ink;
        }
    }
}
