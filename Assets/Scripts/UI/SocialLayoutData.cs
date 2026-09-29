using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Persisted phone/social-feed frame layout (Resources/SocialLayout.asset).</summary>
    [CreateAssetMenu(menuName = "Street Cat/Social Layout", fileName = "SocialLayout")]
    public class SocialLayoutData : ScriptableObject
    {
        [Tooltip("Phone frame width in canvas pixels (reference 1920-wide UI).")]
        public float width = 540f;

        [Tooltip("Phone frame height in canvas pixels.")]
        public float height = 790f;

        [Tooltip("Normalized anchor X (0–1).")]
        public float anchorX = 0.5f;

        [Tooltip("Normalized anchor Y (0–1, bottom origin).")]
        public float anchorY = 0.58f;

        [Tooltip("Extra scale when showing post detail.")]
        public float detailScale = 1.06f;

        public void Clamp()
        {
            width = Mathf.Clamp(width, 220f, 1400f);
            height = Mathf.Clamp(height, 360f, 2000f);
            anchorX = Mathf.Clamp01(anchorX);
            anchorY = Mathf.Clamp01(anchorY);
            detailScale = Mathf.Clamp(detailScale, 0.85f, 1.4f);
        }

        public void ApplyDefaults()
        {
            width = 620f;
            height = 1020f;
            anchorX = 0.5f;
            anchorY = 0.58f;
            detailScale = 1.06f;
        }
    }
}
