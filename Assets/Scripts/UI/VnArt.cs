using System.Collections.Generic;
using StreetCat.Narrative;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Loads VN stage art / portraits from Resources/VnArt with a small cache.
    /// </summary>
    public static class VnArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetBg(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("kv_"))
                return Load("VnArt/KeyArt/" + key);
            return Load("VnArt/Backgrounds/" + key);
        }

        public static Sprite GetPortrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load("VnArt/Characters/" + key);
        }

        public static Sprite GetUi(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load("VnArt/UI/" + key);
        }

        public static Sprite GetProp(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load("VnArt/Props/" + key);
        }

        /// <summary>
        /// Maps Chinese location labels / mode hints to background (or title key-art) keys.
        /// </summary>
        public static string ResolveBackground(string backgroundLabel)
        {
            if (string.IsNullOrEmpty(backgroundLabel))
                return "bg_magazine_office";

            // Keep underscore tokens for booth afternoon vs dusk before stripping
            var raw = backgroundLabel.Replace("　", "_").Replace(" ", "");
            var label = raw.Replace("_", "");

            if (label.Contains("Title") || label.Contains("标题") || label.Contains("街角专访"))
                return "kv_title_street_interview";

            if (label.Contains("后日谈") || label.Contains("几天后"))
                return "bg_epilogue_morning";

            if (label.Contains("写稿") || label.Contains("笔记") || label.Contains("记者笔记"))
                return "bg_writing_desk";

            if (label.Contains("采访"))
                return "bg_interview_corner";

            // 保安亭_午后 before generic community / 午后
            if (raw.Contains("保安亭") && raw.Contains("午后"))
                return "bg_guard_booth_afternoon";

            if (raw.Contains("保安亭") && (raw.Contains("傍晚") || raw.Contains("黄昏")))
                return "bg_guard_booth_dusk";

            if (label.Contains("沈禾") && label.Contains("办公"))
                return "bg_shenhe_office";

            if (label.Contains("工位"))
                return "bg_workstation";

            if (label.Contains("槐安") || (label.Contains("社区") && !label.Contains("杂志")))
                return "bg_huaian_community";

            // Magazine office: 杂志 / 此间 / （杂志社）傍晚
            if (label.Contains("杂志") || label.Contains("此间"))
                return "bg_magazine_office";

            // Bare 傍晚 → dusk booth (script uses 保安亭_傍晚; UI may pass 傍晚)
            if (label.Contains("傍晚") || label.Contains("保安亭"))
                return "bg_guard_booth_dusk";

            if (label.Contains("午后"))
                return "bg_huaian_community";

            return "bg_magazine_office";
        }

        /// <summary>
        /// Maps speaker + line kind to a portrait key. Narration/system → null.
        /// </summary>
        public static string ResolvePortrait(string speakerName, LineSpeaker kind)
        {
            if (kind == LineSpeaker.Narration || kind == LineSpeaker.System)
                return null;

            var name = speakerName ?? "";
            if (kind == LineSpeaker.Inner)
            {
                // Inner monologue defaults to Xiaoling
                if (string.IsNullOrEmpty(name) || name.Contains("小凌") || name.Contains("笔记"))
                    return "ch_xiaoling_default";
            }

            if (string.IsNullOrEmpty(name))
                return null;

            if (name.Contains("小凌"))
                return "ch_xiaoling_default";
            if (name.Contains("沈禾"))
                return "ch_shenhe_default";
            if (name.Contains("大福"))
                return "ch_dafu_default";
            if (name.Contains("林"))
                return "ch_lin_default";
            if (name.Contains("保安"))
                return "ch_guard_default";
            if (name.Contains("梨花") || name.Contains("李华"))
                return "ch_lihua_default";

            return null;
        }

        static Sprite Load(string resourcesPath)
        {
            if (Cache.TryGetValue(resourcesPath, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>(resourcesPath);
            Cache[resourcesPath] = sprite;
            return sprite;
        }
    }
}
