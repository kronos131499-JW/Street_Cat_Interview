using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Per-BGM loudness multipliers (on top of peak normalization).
    /// Edited via 街角专访 → BGM 默认响度.
    /// </summary>
    [CreateAssetMenu(fileName = "BgmLoudness", menuName = "StreetCat/BGM Loudness", order = 20)]
    public class BgmLoudnessData : ScriptableObject
    {
        public const string ResourcePath = "BgmLoudness";
        public const float DefaultMultiplier = 1f;
        public const float MinMultiplier = 0.15f;
        public const float MaxMultiplier = 3f;

        [Serializable]
        public class Entry
        {
            public string key;
            [Range(MinMultiplier, MaxMultiplier)]
            public float multiplier = DefaultMultiplier;
        }

        public List<Entry> entries = new List<Entry>();

        static BgmLoudnessData _cached;

        public static BgmLoudnessData Asset
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = Resources.Load<BgmLoudnessData>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;

        public float GetMultiplier(string key)
        {
            if (string.IsNullOrEmpty(key) || entries == null) return DefaultMultiplier;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.key == key)
                    return Mathf.Clamp(e.multiplier, MinMultiplier, MaxMultiplier);
            }
            return DefaultMultiplier;
        }

        public void SetMultiplier(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            value = Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
            if (entries == null) entries = new List<Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].key == key)
                {
                    entries[i].multiplier = value;
                    return;
                }
            }
            entries.Add(new Entry { key = key, multiplier = value });
        }

#if UNITY_EDITOR
        public static BgmLoudnessData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;

            const string folder = "Assets/Resources";
            const string path = folder + "/BgmLoudness.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = CreateInstance<BgmLoudnessData>();
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            Debug.Log("[BgmLoudness] created " + path);
            return asset;
        }

        public void EditorSave()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            InvalidateCache();
            _cached = this;
        }
#endif
    }
}
