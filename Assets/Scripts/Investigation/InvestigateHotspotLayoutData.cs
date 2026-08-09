using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Investigation
{
    [Serializable]
    public class HotspotRectEntry
    {
        public string id;
        public Vector4 rect; // xMin, yMin, xMax, yMax (0–1, bottom-left origin)
    }

    /// <summary>
    /// Editable investigate hotspot layout. Prefer this asset over hard-coded defaults.
    /// Create / save via menu: 街角专访 → 调查热点编辑器.
    /// </summary>
    [CreateAssetMenu(menuName = "Street Cat/Investigate Hotspot Layout", fileName = "InvestigateHotspotLayout")]
    public class InvestigateHotspotLayoutData : ScriptableObject
    {
        public List<HotspotRectEntry> entries = new List<HotspotRectEntry>();

        public bool TryGet(string id, out Vector4 rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(id) || entries == null) return false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id)
                {
                    rect = entries[i].rect;
                    return true;
                }
            }
            return false;
        }

        public void SetRect(string id, Vector4 rect)
        {
            if (string.IsNullOrEmpty(id)) return;
            rect = ClampRect(rect);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id)
                {
                    entries[i].rect = rect;
                    return;
                }
            }
            entries.Add(new HotspotRectEntry { id = id, rect = rect });
        }

        public static Vector4 ClampRect(Vector4 r)
        {
            float xMin = Mathf.Clamp01(Mathf.Min(r.x, r.z));
            float xMax = Mathf.Clamp01(Mathf.Max(r.x, r.z));
            float yMin = Mathf.Clamp01(Mathf.Min(r.y, r.w));
            float yMax = Mathf.Clamp01(Mathf.Max(r.y, r.w));
            if (xMax - xMin < 0.02f) xMax = Mathf.Min(1f, xMin + 0.02f);
            if (yMax - yMin < 0.02f) yMax = Mathf.Min(1f, yMin + 0.02f);
            return new Vector4(xMin, yMin, xMax, yMax);
        }
    }
}
