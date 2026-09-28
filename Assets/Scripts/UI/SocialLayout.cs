using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Runtime social/phone overlay layout. Prefers Resources/SocialLayout.asset.
    /// Edit in Play Mode via 街角专访 → 社交帖子布局编辑器.
    /// </summary>
    public static class SocialLayout
    {
        const string ResourcePath = "SocialLayout";

        static SocialLayoutData _cached;
        static SocialLayoutData _fallback;

        public static SocialLayoutData Asset
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<SocialLayoutData>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;

        public static SocialLayoutData Current
        {
            get
            {
                var a = Asset;
                if (a != null) return a;
                return _fallback ?? (_fallback = CreateFallback());
            }
        }

        static SocialLayoutData CreateFallback()
        {
            var d = ScriptableObject.CreateInstance<SocialLayoutData>();
            d.ApplyDefaults();
            return d;
        }

#if UNITY_EDITOR
        public static string LastSaveMessage { get; private set; } = "";
        public static bool LastSaveOk { get; private set; }
        public static string AssetDiskPath => "Assets/Resources/SocialLayout.asset";

        public static SocialLayoutData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;

            const string folder = "Assets/Resources";
            const string path = folder + "/SocialLayout.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<SocialLayoutData>();
            asset.ApplyDefaults();
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            RecordSave(true, "created " + path + " @ " + Timestamp());
            return asset;
        }

        public static void SaveCurrent()
        {
            var asset = EnsureAsset();
            if (asset == null)
            {
                RecordSave(false, "save failed — no asset @ " + Timestamp());
                return;
            }
            asset.Clamp();
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            RecordSave(true, "saved → " + AssetDiskPath
                      + " | " + asset.width.ToString("F0") + "×" + asset.height.ToString("F0")
                      + " @(" + asset.anchorX.ToString("F3") + "," + asset.anchorY.ToString("F3") + ")"
                      + " detail×" + asset.detailScale.ToString("F2")
                      + " @ " + Timestamp());
        }

        public static void ResetToDefaults()
        {
            var asset = EnsureAsset();
            if (asset == null) return;
            asset.ApplyDefaults();
            SaveCurrent();
        }

        static string Timestamp() => System.DateTime.Now.ToString("HH:mm:ss");

        static void RecordSave(bool ok, string message)
        {
            LastSaveOk = ok;
            LastSaveMessage = message ?? "";
            if (ok) Debug.Log("[SocialLayout] " + LastSaveMessage);
            else Debug.LogWarning("[SocialLayout] " + LastSaveMessage);
        }
#endif
    }
}
