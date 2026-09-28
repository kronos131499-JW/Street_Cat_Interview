using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// VN stage portrait slot + fit tuning. Edit in Play Mode via 街角专访 → 立绘布局编辑器.
    /// </summary>
    [CreateAssetMenu(menuName = "Street Cat/Portrait Layout", fileName = "PortraitLayout")]
    public class PortraitLayoutData : ScriptableObject
    {
        [Header("Slot (screen anchors 0–1, bottom-left origin)")]
        [Tooltip("Left edge of portrait region")]
        public float slotLeft = 0.72f;
        [Tooltip("Right edge of portrait region")]
        public float slotRight = 0.99f;
        [Tooltip("Top edge (below top HUD)")]
        public float slotTop = 0.98f;
        [Tooltip("Bottom edge — keep above dialogue panel")]
        public float slotBottom = 0.34f;

        [Header("Fit within slot")]
        [Tooltip("Extra scale after aspect fit (1 = fill slot height)")]
        public float heightScale = 1.10f;
        [Tooltip("0.5 = centered; >0.5 shifts right")]
        [Range(0f, 1f)]
        public float centerBias = 0.58f;
        [Tooltip("Vertical nudge (+ = up, − = down within slot)")]
        [Range(-0.35f, 0.35f)]
        public float offsetY = 0f;

        public void Clamp()
        {
            slotLeft = Mathf.Clamp01(slotLeft);
            slotRight = Mathf.Clamp01(slotRight);
            slotTop = Mathf.Clamp01(slotTop);
            slotBottom = Mathf.Clamp01(slotBottom);
            if (slotRight - slotLeft < 0.06f)
                slotRight = Mathf.Min(1f, slotLeft + 0.06f);
            float minBottom = VnTheme.DialogueTop + 0.02f;
            if (slotBottom < minBottom)
                slotBottom = minBottom;
            float minTop = slotBottom + PortraitLayout.MinSlotHeight;
            if (slotTop < minTop)
                slotTop = minTop;
            if (slotTop > 1f)
                slotTop = 1f;
            heightScale = Mathf.Clamp(heightScale, 0.45f, 1.8f);
            centerBias = Mathf.Clamp01(centerBias);
            offsetY = Mathf.Clamp(offsetY, -0.35f, 0.35f);
        }

        public void CopyFrom(PortraitLayoutData other)
        {
            if (other == null) return;
            slotLeft = other.slotLeft;
            slotRight = other.slotRight;
            slotTop = other.slotTop;
            slotBottom = other.slotBottom;
            heightScale = other.heightScale;
            centerBias = other.centerBias;
            offsetY = other.offsetY;
        }
    }
}
