using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Investigation
{
    /// <summary>
    /// Normalized hotspot rects (xMin, yMin, xMax, yMax) over stage backgrounds.
    /// Tuned for 槐安社区_社区平面图 numbered markers (y=0 bottom).
    /// </summary>
    public static class InvestigateHotspotLayout
    {
        /// <summary>
        /// Community guide map (bg_huaian_map):
        /// 01 投喂点 upper-left · 02 狸花猫 upper-mid · 03 贩卖机 upper-right ·
        /// 04 长椅 center · 05 快递柜 / 保安亭 bottom-right.
        /// </summary>
        public static readonly Dictionary<string, Vector4> HuaianMap = new Dictionary<string, Vector4>
        {
            // 01 流浪猫投喂点 — upper-left shelter + bowls
            { "cat_house", new Vector4(0.10f, 0.58f, 0.26f, 0.78f) },
            { "food_bowl", new Vector4(0.22f, 0.52f, 0.32f, 0.64f) },
            { "water_bowl", new Vector4(0.28f, 0.50f, 0.38f, 0.62f) },
            { "sign", new Vector4(0.14f, 0.48f, 0.24f, 0.58f) },
            // 02 灌木丛边晒太阳的猫
            { "tabby", new Vector4(0.40f, 0.56f, 0.56f, 0.74f) },
            // 03 自动贩卖机
            { "vending", new Vector4(0.70f, 0.56f, 0.90f, 0.78f) },
            // 04 木质长椅 — map center plaza
            { "bench", new Vector4(0.40f, 0.34f, 0.58f, 0.50f) },
            // 05 快递柜 — bottom-right near gate
            { "locker", new Vector4(0.64f, 0.12f, 0.84f, 0.32f) },
            // 保安亭 — bottom-right, left of lockers / inside gate
            { "guard_booth", new Vector4(0.48f, 0.10f, 0.64f, 0.30f) },
        };

        public static bool TryGet(string hotspotId, string backgroundKey, out Vector4 rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(hotspotId)) return false;

            // Prefer map layout for the community guide / any huaian art
            if (string.IsNullOrEmpty(backgroundKey) ||
                backgroundKey.Contains("huaian") ||
                backgroundKey.Contains("map") ||
                backgroundKey.Contains("community") ||
                backgroundKey.Contains("平面"))
            {
                return HuaianMap.TryGetValue(hotspotId, out rect);
            }

            return HuaianMap.TryGetValue(hotspotId, out rect);
        }
    }
}
