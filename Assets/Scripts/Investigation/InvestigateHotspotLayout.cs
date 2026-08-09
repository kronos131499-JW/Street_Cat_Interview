using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Investigation
{
    /// <summary>
    /// Investigate hotspot rects. Runtime reads ScriptableObject asset when present;
    /// falls back to built-in defaults for 槐安社区_社区平面图.
    /// </summary>
    public static class InvestigateHotspotLayout
    {
        const string ResourcePath = "InvestigateHotspotLayout";

        /// <summary>
        /// Defaults for community guide map (bg_huaian_map):
        /// 01 投喂点 · 02 狸花猫 · 03 贩卖机 · 04 长椅 · 05 快递柜 / 保安亭.
        /// </summary>
        public static readonly Dictionary<string, Vector4> DefaultHuaianMap = new Dictionary<string, Vector4>
        {
            { "cat_house", new Vector4(0.10f, 0.58f, 0.26f, 0.78f) },
            { "food_bowl", new Vector4(0.22f, 0.52f, 0.32f, 0.64f) },
            { "water_bowl", new Vector4(0.28f, 0.50f, 0.38f, 0.62f) },
            { "sign", new Vector4(0.14f, 0.48f, 0.24f, 0.58f) },
            { "tabby", new Vector4(0.40f, 0.56f, 0.56f, 0.74f) },
            { "vending", new Vector4(0.70f, 0.56f, 0.90f, 0.78f) },
            { "bench", new Vector4(0.40f, 0.34f, 0.58f, 0.50f) },
            { "locker", new Vector4(0.64f, 0.12f, 0.84f, 0.32f) },
            { "guard_booth", new Vector4(0.48f, 0.10f, 0.64f, 0.30f) },
        };

        static InvestigateHotspotLayoutData _cached;

        public static InvestigateHotspotLayoutData Asset
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<InvestigateHotspotLayoutData>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;

        public static bool TryGet(string hotspotId, string backgroundKey, out Vector4 rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(hotspotId)) return false;

            var asset = Asset;
            if (asset != null && asset.TryGet(hotspotId, out rect))
                return true;

            return DefaultHuaianMap.TryGetValue(hotspotId, out rect);
        }

#if UNITY_EDITOR
        public static void SaveRectFromTransform(string hotspotId, RectTransform rt)
        {
            if (string.IsNullOrEmpty(hotspotId) || rt == null) return;
            var asset = EnsureAsset();
            if (asset == null) return;

            var rect = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
            asset.SetRect(hotspotId, rect);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            Debug.Log($"[InvestigateHotspot] saved {hotspotId} = ({rect.x:F3}, {rect.y:F3}, {rect.z:F3}, {rect.w:F3})");
        }

        public static InvestigateHotspotLayoutData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;

            const string folder = "Assets/Resources";
            const string path = folder + "/InvestigateHotspotLayout.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<InvestigateHotspotLayoutData>();
            foreach (var kv in DefaultHuaianMap)
                asset.entries.Add(new HotspotRectEntry { id = kv.Key, rect = kv.Value });

            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            Debug.Log("[InvestigateHotspot] created " + path);
            return asset;
        }
#endif
    }
}
