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
            speakerName = new Color(0.02f, 0.02f, 0.02f, 1f);
            dialogue = Color.black;
            narration = new Color(0.12f, 0.10f, 0.09f, 1f);
            inner = new Color(0.10f, 0.12f, 0.16f, 1f);
            system = new Color(0.35f, 0.16f, 0.05f, 1f);
            status = new Color(0.12f, 0.10f, 0.09f, 1f);
            clickHint = new Color(0.12f, 0.10f, 0.09f, 0.85f);
            choice = Color.black;
        }
    }
}
