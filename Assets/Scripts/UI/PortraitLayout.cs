using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Runtime portrait layout. Prefers Resources/PortraitLayout.asset; falls back to VnTheme defaults.
    /// </summary>
    public static class PortraitLayout
    {
        const string ResourcePath = "PortraitLayout";

        /// <summary>Minimum slot height (normalized) between bottom and top anchors.</summary>
        public const float MinSlotHeight = 0.06f;

        static PortraitLayoutData _cached;

        public static PortraitLayoutData Asset
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<PortraitLayoutData>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;

        /// <summary>Live values for layout (asset when present, else baked defaults).</summary>
        public static PortraitLayoutData Current
        {
            get
            {
                var a = Asset;
                if (a != null) return a;
                return _fallback ?? (_fallback = CreateFallback());
            }
        }

        static PortraitLayoutData _fallback;

        static PortraitLayoutData CreateFallback()
        {
            var d = ScriptableObject.CreateInstance<PortraitLayoutData>();
            ApplyThemeDefaults(d);
            return d;
        }

        public static void ApplyThemeDefaults(PortraitLayoutData d)
        {
            if (d == null) return;
            d.slotLeft = VnTheme.PortraitSlotLeft;
            d.slotRight = VnTheme.PortraitSlotRight;
            d.slotTop = VnTheme.PortraitSlotTop;
            d.slotBottom = VnTheme.PortraitSlotBottom;
            d.heightScale = VnTheme.PortraitHeightScale;
            d.centerBias = VnTheme.PortraitSlotCenterBias;
            d.offsetY = 0f;
        }

        public static float SlotLeft => Current.slotLeft;
        public static float SlotRight => Current.slotRight;
        public static float SlotTop => Current.slotTop;
        public static float SlotBottom => Current.slotBottom;
        public static float HeightScale => Current.heightScale;
        public static float CenterBias => Current.centerBias;
        public static float OffsetY => Current.offsetY;

#if UNITY_EDITOR
        public static PortraitLayoutData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;

            const string folder = "Assets/Resources";
            const string path = folder + "/PortraitLayout.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<PortraitLayoutData>();
            ApplyThemeDefaults(asset);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            Debug.Log("[PortraitLayout] created " + path);
            return asset;
        }

        public static void SaveCurrent()
        {
            var asset = EnsureAsset();
            if (asset == null) return;
            asset.Clamp();
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            Debug.Log("[PortraitLayout] saved slot=("
                      + asset.slotLeft.ToString("F3") + ", " + asset.slotBottom.ToString("F3") + ", "
                      + asset.slotRight.ToString("F3") + ", " + asset.slotTop.ToString("F3")
                      + ") scale=" + asset.heightScale.ToString("F2")
                      + " bias=" + asset.centerBias.ToString("F2")
                      + " offsetY=" + asset.offsetY.ToString("F3"));
        }

        public static void SaveSlotFromTransform(RectTransform rt)
        {
            if (rt == null) return;
            var asset = EnsureAsset();
            if (asset == null) return;
            asset.slotLeft = rt.anchorMin.x;
            asset.slotBottom = rt.anchorMin.y;
            asset.slotRight = rt.anchorMax.x;
            asset.slotTop = rt.anchorMax.y;
            asset.Clamp();
            SaveCurrent();
        }

        public static void ResetToThemeDefaults()
        {
            var asset = EnsureAsset();
            if (asset == null) return;
            ApplyThemeDefaults(asset);
            SaveCurrent();
        }
#endif
    }
}
