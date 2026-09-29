using System.Collections.Generic;
using StreetCat.Narrative;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// All playable character portrait keys grouped for in-game debug picking.
    /// </summary>
    public static class PortraitDebugCatalog
    {
        public readonly struct Group
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string[] Keys;

            public Group(string id, string label, params string[] keys)
            {
                Id = id;
                Label = label;
                Keys = keys;
            }
        }

        public static readonly Group[] All =
        {
            new Group("xiaoling", "小凌",
                "ch_xiaoling_default", "ch_xiaoling_surprised", "ch_xiaoling_thinking",
                "ch_xiaoling_serious", "ch_xiaoling_worried", "ch_xiaoling_awkward",
                "ch_xiaoling_smile", "ch_xiaoling_sassy"),
            new Group("shenhe", "沈禾",
                "ch_shenhe_default", "ch_shenhe_helpless", "ch_shenhe_serious", "ch_shenhe_amused"),
            new Group("dafu", "大福",
                "ch_dafu_default", "ch_dafu_wary", "ch_dafu_annoyed", "ch_dafu_recall",
                "ch_dafu_curious", "ch_dafu_relaxed"),
            new Group("lin", "林女士",
                "ch_lin_default", "ch_lin_pressure", "ch_lin_firm", "ch_lin_tired",
                "ch_lin_guarded", "ch_lin_recall"),
            new Group("guard", "保安叔叔",
                "ch_guard_default", "ch_guard_puzzled", "ch_guard_wry", "ch_guard_recall"),
            new Group("lihua", "梨花",
                "ch_lihua_default"),
        };

        static readonly Dictionary<string, Group> ById = BuildById();

        static Dictionary<string, Group> BuildById()
        {
            var d = new Dictionary<string, Group>();
            foreach (var g in All)
                d[g.Id] = g;
            return d;
        }

        public static Group? Find(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            return ById.TryGetValue(characterId, out var g) ? g : (Group?)null;
        }

        /// <summary>Map runtime speaker label to catalog character id.</summary>
        public static string MatchCharacterId(string speakerName, LineSpeaker kind)
        {
            if (kind == LineSpeaker.Inner)
                return "xiaoling";

            var name = speakerName ?? "";
            if (string.IsNullOrEmpty(name))
                return null;

            if (name.Contains("小凌") || name.Contains("Ling"))
                return "xiaoling";
            if (name.Contains("沈禾") || name.Contains("Shen He") || name.Contains("ShenHe"))
                return "shenhe";
            if (name.Contains("大福") || name.Contains("Dafu"))
                return "dafu";
            if (name.Contains("林女士") || name.Contains("Ms. Lin") || name.Contains("Ms Lin"))
                return "lin";
            if (name == "Lin" || name.StartsWith("Lin ") || name.EndsWith(" Lin"))
                return "lin";
            if (name.Contains("保安") || name.Contains("Security Guard") || name.Contains("Uncle Guard") || name.Contains("Guard"))
                return "guard";
            if (name.Contains("梨花") || name.Contains("李华") || name.Contains("狸花") || name.Contains("Lihua"))
                return "lihua";

            return null;
        }

        public static string LabelForKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (key.EndsWith("_default")) return "常态";

            var tail = key.Contains("_") ? key.Substring(key.LastIndexOf('_') + 1) : key;
            switch (tail)
            {
                case "surprised": return "惊讶";
                case "thinking": return "思考";
                case "serious": return "认真";
                case "worried": return "局促";
                case "awkward": return "awkward";
                case "smile": return "吐槽";
                case "sassy": return "sassy";
                case "helpless": return "无奈";
                case "amused": return "认可";
                case "wary": return "警惕";
                case "annoyed": return "不满";
                case "recall": return "回忆";
                case "curious": return "好奇";
                case "relaxed": return "放松";
                case "pressure": return "压力";
                case "firm": return "坚定";
                case "tired": return "疲惫";
                case "guarded": return "防备";
                case "puzzled": return "疑惑";
                case "wry": return "苦笑";
                default: return tail;
            }
        }

        public static string PreviewSpeakerName(string characterId)
        {
            var g = Find(characterId);
            return g?.Label ?? characterId ?? "";
        }
    }
}
