using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Investigation
{
    /// <summary>
    /// Normalized hotspot rects (xMin, yMin, xMax, yMax) over stage backgrounds.
    /// Tunable constants — approximate placements on community / booth art.
    /// </summary>
    public static class InvestigateHotspotLayout
    {
        /// <summary>Rects for bg_huaian_community (Unity anchors, y=0 bottom).</summary>
        public static readonly Dictionary<string, Vector4> HuaianCommunity = new Dictionary<string, Vector4>
        {
            // Near entrance / right of guard booth hedge
            { "locker", new Vector4(0.20f, 0.28f, 0.34f, 0.52f) },
            // Feeding cluster along center-left hedges
            { "cat_house", new Vector4(0.34f, 0.34f, 0.46f, 0.50f) },
            { "food_bowl", new Vector4(0.44f, 0.26f, 0.54f, 0.38f) },
            { "water_bowl", new Vector4(0.52f, 0.26f, 0.62f, 0.38f) },
            // Notice board (right foreground)
            { "sign", new Vector4(0.80f, 0.32f, 0.97f, 0.58f) },
            // Bushes / mid vegetation
            { "tabby", new Vector4(0.42f, 0.42f, 0.56f, 0.58f) },
            // Mid-right wall / path edge (stand-in for vending)
            { "vending", new Vector4(0.64f, 0.30f, 0.76f, 0.52f) },
            // Path-side seating
            { "bench", new Vector4(0.28f, 0.18f, 0.42f, 0.32f) },
        };

        public static bool TryGet(string hotspotId, string backgroundKey, out Vector4 rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(hotspotId)) return false;

            // Default / community stage
            if (string.IsNullOrEmpty(backgroundKey) ||
                backgroundKey.Contains("huaian") ||
                backgroundKey.Contains("community") ||
                backgroundKey == "bg_huaian_community")
            {
                return HuaianCommunity.TryGetValue(hotspotId, out rect);
            }

            return HuaianCommunity.TryGetValue(hotspotId, out rect);
        }
    }
}
