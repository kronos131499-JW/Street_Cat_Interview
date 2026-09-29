using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// zh→en overlay for hardcoded investigation / talk / notebook strings.
    /// Chinese in C# stays authoritative; EN lives in Resources/Loc/hardtext_en.json.
    /// </summary>
    public static class HardTextLoc
    {
        [Serializable]
        class TableFile
        {
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        class Entry
        {
            public string zh;
            public string en;
        }

        static readonly Dictionary<string, string> map = new Dictionary<string, string>();
        static bool loaded;

        public static void Reload()
        {
            loaded = false;
            Load();
        }

        /// <summary>English mapping only. Does not follow the current language setting.</summary>
        public static bool TryEnglish(string zh, out string en)
        {
            en = null;
            if (string.IsNullOrEmpty(zh)) return false;
            Load();
            return map.TryGetValue(zh, out en) && !string.IsNullOrEmpty(en);
        }

        /// <summary>Return English when language is EN and a mapping exists; otherwise zh unchanged.</summary>
        public static string T(string zh)
        {
            if (string.IsNullOrEmpty(zh) || !GameSettings.IsEnglish)
                return zh;
            Load();
            if (map.TryGetValue(zh, out var en) && !string.IsNullOrEmpty(en))
                return en;

            // Inspect() appends this suffix to an already-localized description.
            const string seenSuffix = "\n（已经看过了。）";
            if (zh.EndsWith(seenSuffix, StringComparison.Ordinal))
            {
                var head = zh.Substring(0, zh.Length - seenSuffix.Length);
                return T(head) + "\n" + T("（已经看过了。）");
            }

            return zh;
        }

        /// <summary>Localize a string that may include a known Chinese prefix + dynamic suffix (counts, etc.).</summary>
        public static string TPrefix(string zhPrefix, string suffix)
        {
            return T(zhPrefix) + suffix;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            map.Clear();
            var asset = Resources.Load<TextAsset>("Loc/hardtext_en");
            if (asset == null)
            {
                Debug.LogWarning("[HardTextLoc] Missing Resources/Loc/hardtext_en.json");
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<TableFile>(asset.text);
                if (file?.entries == null) return;
                foreach (var e in file.entries)
                {
                    if (e == null || string.IsNullOrEmpty(e.zh) || string.IsNullOrEmpty(e.en))
                        continue;
                    map[e.zh] = e.en;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[HardTextLoc] Parse failed: " + ex.Message);
            }
        }
    }
}
