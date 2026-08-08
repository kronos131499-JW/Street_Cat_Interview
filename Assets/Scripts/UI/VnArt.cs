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
        /// Exact script 【背景：…】 labels → Resources/VnArt/Backgrounds keys (正式背景图).
        /// </summary>
        static readonly Dictionary<string, string> BackgroundExact = new Dictionary<string, string>
        {
            { "编辑部_傍晚", "bg_editorial_dusk" },
            { "沈禾办公室_傍晚", "bg_shenhe_office_dusk" },
            { "编辑部_工位_傍晚", "bg_editorial_desk_dusk" },
            { "编辑部工位_上午", "bg_editorial_desk_morning" },
            { "沈禾办公室_上午", "bg_shenhe_office_morning" },
            { "槐安社区_午后", "bg_huaian_afternoon" },
            { "槐安社区_社区平面图", "bg_huaian_map" },
            { "流浪猫投喂点", "bg_feeding_spot" },
            { "流浪猫投喂点_告示牌", "bg_feeding_sign" },
            { "晒太阳的猫_放松", "bg_cat_relax" },
            { "晒太阳的猫_警惕", "bg_cat_alert" },
            { "晒太阳的猫_躲闪", "bg_cat_hide" },
            { "晒太阳的猫_躲藏", "bg_cat_hide" },
            { "自动贩卖机", "bg_vending" },
            { "木质长椅", "bg_bench" },
            { "快递柜", "bg_locker" },
            { "保安亭_午后", "bg_guard_afternoon" },
            { "保安亭_傍晚", "bg_guard_dusk" },
            { "咖啡馆_午后", "bg_cafe_afternoon" },
            { "文章发布页面", "bg_huaian_afternoon" },
        };

        /// <summary>
        /// Maps Chinese location labels / mode hints to background (or title key-art) keys.
        /// </summary>
        public static string ResolveBackground(string backgroundLabel)
        {
            if (string.IsNullOrEmpty(backgroundLabel))
                return "bg_editorial_dusk";

            var raw = backgroundLabel.Replace("　", "_").Replace(" ", "").Trim();

            // Already a resource key
            if (raw.StartsWith("bg_") || raw.StartsWith("kv_"))
                return raw;

            if (BackgroundExact.TryGetValue(raw, out var exact))
                return exact;

            // Filename noise: 木质长椅bg / 告示牌png
            var cleaned = raw;
            if (cleaned.EndsWith("bg") || cleaned.EndsWith("BG"))
                cleaned = cleaned.Substring(0, cleaned.Length - 2);
            if (cleaned.EndsWith("png") || cleaned.EndsWith("PNG"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            cleaned = cleaned.Trim('_');
            if (BackgroundExact.TryGetValue(cleaned, out exact))
                return exact;
            raw = cleaned;

            var label = raw.Replace("_", "");

            if (label.Contains("Title") || label.Contains("标题") || label.Contains("街角专访"))
                return "kv_title_street_interview";

            if (label.Contains("后日谈") || label.Contains("几天后") || label.Contains("文章发布"))
                return "bg_huaian_afternoon";

            if (label.Contains("咖啡"))
                return "bg_cafe_afternoon";

            if (label.Contains("平面图"))
                return "bg_huaian_map";

            if (label.Contains("告示") || label.Contains("挂牌"))
                return "bg_feeding_sign";

            if (label.Contains("投喂"))
                return "bg_feeding_spot";

            if (label.Contains("贩卖"))
                return "bg_vending";

            if (label.Contains("长椅"))
                return "bg_bench";

            if (label.Contains("快递"))
                return "bg_locker";

            if (label.Contains("躲闪") || label.Contains("躲藏"))
                return "bg_cat_hide";
            if (label.Contains("警惕") && label.Contains("猫"))
                return "bg_cat_alert";
            if (label.Contains("放松") && label.Contains("猫"))
                return "bg_cat_relax";
            if (label.Contains("晒太阳"))
                return "bg_cat_relax";

            if (raw.Contains("保安亭") && raw.Contains("午后"))
                return "bg_guard_afternoon";
            if (raw.Contains("保安亭") && (raw.Contains("傍晚") || raw.Contains("黄昏")))
                return "bg_guard_dusk";

            if (label.Contains("沈禾") && label.Contains("办公") && label.Contains("上午"))
                return "bg_shenhe_office_morning";
            if (label.Contains("沈禾") && label.Contains("办公"))
                return "bg_shenhe_office_dusk";

            if ((label.Contains("工位") || label.Contains("写稿") || label.Contains("笔记")) && label.Contains("上午"))
                return "bg_editorial_desk_morning";
            if (label.Contains("工位") || label.Contains("写稿") || label.Contains("笔记") || label.Contains("记者笔记"))
                return "bg_editorial_desk_dusk";

            if (label.Contains("采访") && label.Contains("林"))
                return "bg_cafe_afternoon";
            if (label.Contains("采访"))
                return "bg_guard_dusk";

            if (label.Contains("槐安") || (label.Contains("社区") && !label.Contains("杂志") && !label.Contains("编辑")))
                return "bg_huaian_afternoon";

            if (label.Contains("杂志") || label.Contains("此间") || label.Contains("编辑部"))
                return "bg_editorial_dusk";

            if (label.Contains("保安亭"))
                return "bg_guard_dusk";

            if (label.Contains("午后"))
                return "bg_huaian_afternoon";

            return "bg_editorial_dusk";
        }

        /// <summary>
        /// Maps speaker + line kind (+ optional expression tag) to a portrait key.
        /// Narration/system → null. Expression may appear in speakerName as
        /// 「小凌（惊讶）」/「小凌-思考」/「小凌/worried」 or as a separate state arg.
        /// </summary>
        public static string ResolvePortrait(string speakerName, LineSpeaker kind, string expression = null)
        {
            if (kind == LineSpeaker.Narration || kind == LineSpeaker.System)
                return null;

            var name = speakerName ?? "";
            // GameUI treats Inner as narration (no portrait); keep a key available
            // for callers that opt in via Character lines.
            if (kind == LineSpeaker.Inner)
            {
                if (string.IsNullOrEmpty(name) || name.Contains("小凌") || name.Contains("笔记"))
                    return ResolveXiaolingExpression(name, expression);
            }

            if (string.IsNullOrEmpty(name))
                return null;

            if (name.Contains("小凌"))
                return ResolveXiaolingExpression(name, expression);
            if (name.Contains("沈禾"))
            {
                var e = (expression ?? "").Trim();
                if (e.Contains("笑") || e.Contains("认可") || e.Contains("amused") || name.Contains("笑"))
                    return "ch_shenhe_amused";
                return "ch_shenhe_default";
            }
            if (name.Contains("大福"))
            {
                var e = (expression ?? "").Trim();
                if (e.Contains("警") || e.Contains("wary") || name.Contains("警"))
                    return "ch_dafu_wary";
                return "ch_dafu_default";
            }
            if (name.Contains("林"))
                return "ch_lin_default";
            if (name.Contains("保安"))
                return "ch_guard_default";
            if (name.Contains("梨花") || name.Contains("李华"))
                return "ch_lihua_default";

            return null;
        }

        /// <summary>
        /// Infer 小凌 expression from line text when script has no ·立绘 tag.
        /// Returns null to keep sticky / default.
        /// </summary>
        public static string SuggestXiaolingExpression(string text, LineSpeaker kind)
        {
            if (kind == LineSpeaker.Inner)
            {
                if (string.IsNullOrEmpty(text)) return "思考";
                if (text.Contains("居然") || text.Contains("真听懂") || text.Contains("好神奇") || text.Contains("？！"))
                    return "惊讶";
                if (text.Contains("奇怪") || text.Contains("呃") || text.Contains("直接说"))
                    return "局促";
                return "思考";
            }

            if (string.IsNullOrEmpty(text)) return null;

            if (text.Contains("啥") || text.Contains("真听懂了") || text.Contains("居然")
                || text.Contains("？！") || text.Contains("?!"))
                return "惊讶";

            if (text.Contains("听起来很像") || text.Contains("工资是") || text.Contains("别看")
                || text.Contains("证明他的钱") || text.Contains("算了"))
                return "吐槽";

            if (text.Contains("呃") || text.Contains("怎么改都") || text.Contains("不对味")
                || (text.Contains("……") && text.Length <= 10))
                return "局促";

            if (text.Contains("记者") || text.Contains("采访") || text.Contains("问题吗")
                || text.Contains("大福。") || text.StartsWith("叔叔"))
                return "认真";

            if (text.EndsWith("？") || text.EndsWith("?") || text.Contains("感觉")
                || text.Contains("这什么") || text.Contains("所以翻译"))
                return "思考";

            return null;
        }

        /// <summary>
        /// Filename / state → Resources key for 小凌 portraits.
        /// 常态→default, 惊讶→surprised, 思考→thinking, 认真→serious,
        /// 局促→worried (+ awkward alias), 吐槽→smile (+ sassy alias).
        /// </summary>
        public static string ResolveXiaolingExpression(string speakerOrTag, string expression = null)
        {
            var tag = expression ?? "";
            if (string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(speakerOrTag))
            {
                // Pull parenthetical / dash / slash state from speaker label
                var s = speakerOrTag;
                int l = s.IndexOf('（');
                int r = s.IndexOf('）');
                if (l >= 0 && r > l)
                    tag = s.Substring(l + 1, r - l - 1);
                else
                {
                    l = s.IndexOf('(');
                    r = s.IndexOf(')');
                    if (l >= 0 && r > l)
                        tag = s.Substring(l + 1, r - l - 1);
                    else if (s.Contains("-"))
                        tag = s.Substring(s.LastIndexOf('-') + 1);
                    else if (s.Contains("/"))
                        tag = s.Substring(s.LastIndexOf('/') + 1);
                }
            }

            tag = (tag ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(tag) || tag == "常态" || tag == "default" || tag == "normal")
                return "ch_xiaoling_default";
            if (tag == "惊讶" || tag == "surprised" || tag == "shock" || tag == "surprise")
                return "ch_xiaoling_surprised";
            if (tag == "思考" || tag == "thinking" || tag == "think")
                return "ch_xiaoling_thinking";
            if (tag == "认真" || tag == "serious" || tag == "earnest")
                return "ch_xiaoling_serious";
            if (tag == "局促" || tag == "worried" || tag == "nervous")
                return "ch_xiaoling_worried";
            if (tag == "awkward")
                return "ch_xiaoling_awkward";
            if (tag == "吐槽" || tag == "smile" || tag == "smirk")
                return "ch_xiaoling_smile";
            if (tag == "sassy")
                return "ch_xiaoling_sassy";

            return "ch_xiaoling_default";
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
