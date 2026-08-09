using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Title-screen element anchors. Runtime prefers ScriptableObject; falls back to defaults.
    /// Edit in Play Mode via 街角专访 → 主菜单布局编辑器.
    /// </summary>
    public static class TitleMenuLayout
    {
        const string ResourcePath = "TitleMenuLayout";

        public static readonly Dictionary<string, Vector4> Defaults = new Dictionary<string, Vector4>
        {
            { "magazine_host", new Vector4(0.07f, 0.08f, 0.93f, 0.96f) },
            { "left_page", new Vector4(0.04f, 0.08f, 0.48f, 0.92f) },
            { "feature_art", new Vector4(0.06f, 0.38f, 0.94f, 0.96f) },
            { "logo_cn", new Vector4(0.08f, 0.78f, 0.92f, 0.96f) },
            { "logo_en", new Vector4(0.10f, 0.68f, 0.90f, 0.80f) },
            { "quote_box", new Vector4(0.08f, 0.10f, 0.92f, 0.36f) },
            { "subtitle", new Vector4(0.16f, 0.14f, 0.88f, 0.32f) },
            { "blurb_deco", new Vector4(0.55f, 0.02f, 0.98f, 0.22f) },
            { "right_page", new Vector4(0.52f, 0.10f, 0.96f, 0.92f) },
            { "contents_header", new Vector4(0.06f, 0.86f, 0.94f, 0.96f) },
            { "title_actions", new Vector4(0.16f, 0.20f, 0.84f, 0.82f) },
            { "tagline", new Vector4(0.08f, 0.06f, 0.92f, 0.18f) },
            { "prop_translator", new Vector4(0.01f, 0.02f, 0.14f, 0.42f) },
            { "prop_notes", new Vector4(0.86f, 0.02f, 0.99f, 0.38f) },
            { "prop_polaroid_a", new Vector4(0.00f, 0.55f, 0.12f, 0.88f) },
            { "prop_polaroid_b", new Vector4(0.88f, 0.52f, 0.995f, 0.86f) },
            { "prop_scraps", new Vector4(0.78f, 0.00f, 0.92f, 0.22f) },
        };

        public static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            { "magazine_host", "杂志整体" },
            { "left_page", "左页区域" },
            { "feature_art", "左页插画" },
            { "logo_cn", "中文标题" },
            { "logo_en", "英文副标" },
            { "quote_box", "引用框" },
            { "subtitle", "左页文案" },
            { "blurb_deco", "左页装饰" },
            { "right_page", "右页区域" },
            { "contents_header", "CONTENTS 头" },
            { "title_actions", "菜单按钮区" },
            { "tagline", "右页标语" },
            { "prop_translator", "翻译器" },
            { "prop_notes", "笔记本" },
            { "prop_polaroid_a", "拍立得A" },
            { "prop_polaroid_b", "拍立得B" },
            { "prop_scraps", "散页" },
        };

        static TitleMenuLayoutData _cached;

        public static TitleMenuLayoutData Asset
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<TitleMenuLayoutData>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;

        public static bool TryGet(string id, out Vector4 rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(id)) return false;
            var asset = Asset;
            if (asset != null && asset.TryGet(id, out rect))
                return true;
            return Defaults.TryGetValue(id, out rect);
        }

        public static void Apply(RectTransform rt, string id, Vector2 defaultMin, Vector2 defaultMax)
        {
            if (rt == null) return;
            if (TryGet(id, out var r))
            {
                rt.anchorMin = new Vector2(r.x, r.y);
                rt.anchorMax = new Vector2(r.z, r.w);
            }
            else
            {
                rt.anchorMin = defaultMin;
                rt.anchorMax = defaultMax;
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        public static void SaveRectFromTransform(string id, RectTransform rt)
        {
            if (string.IsNullOrEmpty(id) || rt == null) return;
            var asset = EnsureAsset();
            if (asset == null) return;
            var rect = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
            asset.SetRect(id, rect);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            var name = DisplayNames.TryGetValue(id, out var n) ? n : id;
            Debug.Log($"[TitleMenu] saved {name} ({id}) = ({rect.x:F3}, {rect.y:F3}, {rect.z:F3}, {rect.w:F3})");
        }

        public static TitleMenuLayoutData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;

            const string folder = "Assets/Resources";
            const string path = folder + "/TitleMenuLayout.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<TitleMenuLayoutData>();
            foreach (var kv in Defaults)
                asset.entries.Add(new TitleRectEntry { id = kv.Key, rect = kv.Value });

            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            Debug.Log("[TitleMenu] created " + path);
            return asset;
        }
#endif
    }
}
