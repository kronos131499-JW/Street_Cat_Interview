using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// UI string tables from Resources/Loc/ui_{zh|en}.json.
    /// Named UiLoc (not Loc) to avoid clashing with namespace StreetCat.Loc.
    /// </summary>
    public static class UiLoc
    {
        [Serializable]
        class TableFile
        {
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        class Entry
        {
            public string key;
            public string value;
        }

        static readonly Dictionary<string, string> map = new Dictionary<string, string>();
        static bool loaded;
        static GameLanguage loadedFor = (GameLanguage)(-1);

        public static void Reload()
        {
            loaded = false;
            Load();
        }

        public static string T(string key, string fallback = null)
        {
            Load();
            if (!string.IsNullOrEmpty(key) && map.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                return v;
            return fallback ?? key ?? "";
        }

        static void Load()
        {
            GameSettings.EnsureLoaded();
            if (loaded && loadedFor == GameSettings.Language) return;

            map.Clear();
            loaded = true;
            loadedFor = GameSettings.Language;

            var code = GameSettings.Language == GameLanguage.En ? "en" : "zh";
            var asset = Resources.Load<TextAsset>("Loc/ui_" + code);
            if (asset == null)
            {
                Debug.LogWarning("[Loc] Missing Resources/Loc/ui_" + code + ".json");
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<TableFile>(asset.text);
                if (file?.entries == null) return;
                foreach (var e in file.entries)
                {
                    if (e == null || string.IsNullOrEmpty(e.key)) continue;
                    map[e.key] = e.value ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Loc] Failed to parse ui_" + code + ": " + ex.Message);
            }
        }
    }
}
