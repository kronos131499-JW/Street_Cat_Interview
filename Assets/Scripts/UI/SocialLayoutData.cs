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

        [Tooltip("Enlarged post width. 0 = not set yet.")]
        public float zoomWidth;

        [Tooltip("Enlarged post height. 0 = not set yet.")]
        public float zoomHeight;

        [Tooltip("Enlarged post anchor X.")]
        public float zoomAnchorX = 0.5f;

        [Tooltip("Enlarged post anchor Y.")]
        public float zoomAnchorY = 0.5f;

        public bool HasZoomFrame => zoomWidth > 40f && zoomHeight > 40f;

        public void EnsureZoomDefaults()
        {
            if (HasZoomFrame) return;
            zoomWidth = Mathf.Clamp(Mathf.Max(width, 40f) * 1.32f, 220f, 1600f);
            zoomHeight = Mathf.Clamp(Mathf.Max(height, 40f) * 1.32f, 360f, 2000f);
            zoomAnchorX = 0.5f;
            zoomAnchorY = 0.5f;
        }

        public void Clamp()
        {
            width = Mathf.Clamp(width, 220f, 1400f);
            height = Mathf.Clamp(height, 360f, 2000f);
            anchorX = Mathf.Clamp01(anchorX);
            anchorY = Mathf.Clamp01(anchorY);
            detailScale = Mathf.Clamp(detailScale, 0.85f, 1.4f);
            if (zoomWidth > 1f)
                zoomWidth = Mathf.Clamp(zoomWidth, 220f, 1600f);
            if (zoomHeight > 1f)
                zoomHeight = Mathf.Clamp(zoomHeight, 360f, 2000f);
            if (HasZoomFrame)
            {
                zoomAnchorX = Mathf.Clamp01(zoomAnchorX);
                zoomAnchorY = Mathf.Clamp01(zoomAnchorY);
            }
        }

        public void ApplyDefaults()
        {
            width = 620f;
            height = 1020f;
            anchorX = 0.5f;
            anchorY = 0.58f;
            detailScale = 1.06f;
            zoomWidth = 820f;
            zoomHeight = 1340f;
            zoomAnchorX = 0.5f;
            zoomAnchorY = 0.5f;
        }
    }
}
