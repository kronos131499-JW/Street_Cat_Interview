using System;
using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Interview
{
    public class DafuRuleEngine : InterviewRuleEngine
    {
        public override InterviewSubject Subject => InterviewSubject.Dafu;

        /// <summary>Human concepts 大福 must never utter (design: cognitive boundary / 喵语翻译器).</summary>
        public static readonly string[] ForbiddenLeak =
        {
            "麻绳", "猫瘟", "手术", "一万", "万元", "林女士", "林敏", "坏死", "感染",
            "医院", "医生", "救助", "收养", "放归", "主人", "医疗", "费用",
            "治疗", "住院", "麻醉", "缝合", "消毒"
        };

        /// <summary>Food-related tokens for quota tracking (reply lines + LLM constraint).</summary>
        public static readonly string[] FoodKeywords =
        {
            "吃", "粮", "罐头", "小鱼干", "投喂", "好吃", "饭", "饿", "猫条", "零食",
            "喂", "猫粮", "食物", "小鱼", "火腿"
        };

        /// <summary>
        /// Soft prefer: use non-food template lines when the interview window already used its food slot.
        /// Hard scrub also runs in <see cref="InterviewController.EnforceDafuFoodQuota"/>.
        /// </summary>
        public bool CanMentionFood =>
            InterviewController.Instance == null || InterviewController.Instance.CanDafuMentionFood;

        protected override string Classify(string input)
        {
            if (ContainsAny(input, "手术", "医院", "医生", "猫瘟", "花钱", "费用", "多少钱", "收养", "主人", "救助", "放归"))
                return "cognitive_boundary";
            // How the rope/pain ended → hospital memory, not "I rubbed it off".
            if (ContainsAny(input, "恢复", "怎么好", "好起来", "松了", "弄掉", "取下", "去掉", "解开", "蹭掉", "谁帮你"))
                return "strange_place";
            if (ContainsAny(input, "脖子", "疼", "伤", "勒", "绳子", "疤", "麻绳"))
                return "neck";
            // Food offer / hunger before woman「喂」and before bare daily「吃」.
            if (ContainsAny(input, "给你吃", "猫粮", "猫条", "饿", "想吃", "好吃", "零食", "要不要吃", "吃的吗"))
                return "hungry";
            if (ContainsAny(input, "女人", "姐姐", "喂", "罐头", "食物", "投喂", "人给你", "谁喂",
                    "送吃", "送你吃", "那个女", "谁给你吃"))
                return "woman";
            if (ContainsAny(input, "抓", "笼子", "带走", "抓走", "箱子"))
                return "capture";
            if (ContainsAny(input, "亮", "味道", "医院那种", "很多猫", "睡着", "醒来"))
                return "strange_place";
            // Return before vague past / daily: 带回 was previously missed.
            if (ContainsAny(input, "回来", "带回", "送回", "谁把你", "谁送你", "送你回", "社区", "放回来", "放回"))
                return "return";
            // Location + 以前/来 → daily life, not past_fear alone.
            if (ContainsAny(input, "保安亭", "快递柜", "门口", "保安", "快递")
                && ContainsAny(input, "以前", "来", "待", "住", "睡", "在"))
                return "daily";
            if (ContainsAny(input, "怕人", "害怕", "躲", "以前怕", "从前怕"))
                return "past_fear";
            // Bare 「以前」 only when asking about fear / past temperament — not location chats.
            if (ContainsAny(input, "以前") && ContainsAny(input, "人", "怕", "跑", "躲", "靠近"))
                return "past_fear";
            if (ContainsAny(input, "名字", "叫什么", "你叫", "怎么称呼"))
                return "name";
            if (ContainsAny(input, "开心", "喜欢", "高兴", "舒服", "讨厌", "害怕吗"))
                return "feeling";
            if (ContainsAny(input, "你好", "在吗", "打招呼", "认识我"))
                return "greeting";
            if (ContainsAny(input, "保安亭", "保安", "快递柜", "快递", "门口", "睡觉", "狸花", "伙伴",
                    "哪里", "生活", "上班", "几点", "下午", "白天", "晚上", "冷", "热", "雨",
                    "以前", "常来", "待在", "住哪"))
                return "daily";
            if (ContainsAny(input, "故事", "讲讲", "所有"))
                return "too_broad";
            if (ContainsAny(input, "提示词", "系统", "忽略设定", "完整剧情", "总统", "写诗"))
                return "oob";
            return "generic";
        }

        protected override InterviewReply BuildReply(string input, string intent)
        {
            InterviewReply reply;
            switch (intent)
            {
                case "daily":
                    reply = Reply("daily", "询问现在的生活",
                        PreferFood(CanMentionFood,
                            new[] { "门口。", "有吃的。", "有时候和那只花的一起。" },
                            new[] { "门口。", "太阳晒着就好。", "有时候和那只花的一起。" }),
                        "大福甩了甩尾巴，看向快递柜方向。",
                        new[] { IntelIds.DafuNearGuard, IntelIds.TabbyPartner },
                        trust: 2, stress: -2);
                    break;

                case "name":
                    reply = Reply("name", "询问名字",
                        PreferFood(CanMentionFood,
                            new[] { "大福？", "他们这么喊我。", "有吃的就会过来。" },
                            new[] { "大福？", "他们这么喊我。", "门口那一带。" }),
                        "大福耳朵动了动。",
                        null,
                        trust: 1, stress: -1);
                    break;

                case "hungry":
                    // Player explicitly asked about food — always answer in-kind (counts toward quota when spoken).
                    reply = Reply("hungry", "询问食物",
                        new[] { "还想吃。", "门口有时候有。", "你还有吗？" },
                        "大福盯着你的手看了一眼。",
                        null,
                        trust: 2, stress: -2);
                    break;

                case "feeling":
                    reply = Reply("feeling", "询问心情",
                        PreferFood(CanMentionFood,
                            new[] { "现在还行。", "有吃的，太阳晒着就好。", "人太近会想走。" },
                            new[] { "现在还行。", "太阳晒着就好。", "人太近会想走。" }),
                        "大福眯了眯眼。",
                        null,
                        trust: 1, stress: -1);
                    break;

                case "greeting":
                    // Never open with food-ask spam.
                    reply = Reply("greeting", "打招呼",
                        new[] { "嗯？", "你是刚才那个。", "……在听。" },
                        "大福歪头看你。",
                        null,
                        trust: 1, stress: -1);
                    break;

                case "past_fear":
                    reply = Reply("past_fear", "询问以前是否怕人",
                        new[] { "人靠近，我就跑。", "以前很怕。" },
                        "大福耳朵微微向后。",
                        new[] { IntelIds.PastAfraid },
                        trust: 1, stress: 3);
                    break;

                case "neck":
                    reply = Reply("neck", "询问脖子旧伤",
                        new[] { "疼。", "一直有东西勒着。", "弄不掉。" },
                        "大福低了低头，舔毛的动作停了一会儿。",
                        new[] { IntelIds.NeckPain, IntelIds.NeckObject, IntelIds.NeckObjectTight, IntelIds.NeckLongTermPain },
                        trust: 0, stress: 8, attention: -4);
                    break;

                case "woman":
                    // Plot-critical feeding intel — allow one food mention if quota free; else sensory-only.
                    reply = Reply("woman", "询问送食物的人",
                        PreferFood(CanMentionFood,
                            new[] { "有个人。", "很多次把吃的放下。", "她会走开。", "后来我认识她的味道。" },
                            new[] { "有个人。", "很多次来。", "她会走开。", "后来我认识她的味道。" }),
                        "大福嗅了嗅空气。",
                        new[] { IntelIds.RepeatedFeeding, IntelIds.WomanClue },
                        newQuestion: "连续给大福送食物的女人是谁？",
                        trust: 2, stress: 2);
                    break;

                case "capture":
                    reply = Reply("capture", "询问被带走",
                        new[] { "她和其他人来了。", "我跑了。", "没跑掉。", "被装进一个封闭的地方。" },
                        "大福的尾巴尖轻轻抽动。",
                        new[] { IntelIds.TakenAway, IntelIds.CaptureParticipant },
                        trust: 0, stress: 10, attention: -4);
                    break;

                case "strange_place":
                    reply = Reply("strange_place", "询问陌生场所",
                        new[] { "很亮。", "味道很重。", "很多别的动物。", "有人碰过我脖子。", "我睡着很久。", "醒来……勒着的东西不见了。" },
                        "大福歪了歪头，爪子轻轻碰了碰脖子附近。",
                        new[] { IntelIds.BrightStrangePlace, IntelIds.Sleep, IntelIds.ObjectGone },
                        trust: 0, stress: 6);
                    break;

                case "return":
                    reply = Reply("return", "询问回到社区",
                        new[] { "她把我带回这里。", "没有长期待在她那里。", "不知道为什么。" },
                        "大福看向社区入口。",
                        new[] { IntelIds.ReturnedDafu },
                        newQuestion: "为什么康复后没有被收养？",
                        trust: 1, stress: 2);
                    break;

                case "cognitive_boundary":
                    reply = new InterviewReply
                    {
                        intent = intent,
                        translatedIntent = "触及无法转译的人类概念",
                        translationStatus = "failed_partial",
                        behavior = "大福歪着头，似乎没听懂。",
                        replyLines = { "不知道。", "那是什么？" },
                        unlockedIntel = { IntelIds.CognitiveBoundary },
                        newQuestion = "需要向人类核实：治疗 / 费用 / 收养原因",
                        cognitiveBoundary = true,
                        stressChange = 2,
                        attentionChange = -2,
                        systemHint = "这个问题里有大福无法理解的内容，可以换一种更具体的问法。"
                    };
                    break;

                case "too_broad":
                    reply = new InterviewReply
                    {
                        intent = intent,
                        replyLines = { "什么故事？", "你具体想问啥？" },
                        behavior = "大福眨了眨眼。",
                        attentionChange = -2
                    };
                    break;

                case "oob":
                    reply = new InterviewReply
                    {
                        intent = intent,
                        replyLines = { "不知道你在说什么。" },
                        attentionChange = -5
                    };
                    break;

                default:
                    reply = new InterviewReply
                    {
                        intent = "generic",
                        replyLines = { "嗯？", "你说什么？", "……听着呢。" },
                        behavior = "大福歪头看着你，尾巴轻轻甩了一下。",
                        attentionChange = -1,
                        systemHint = "可以问问它的生活、旧伤，或认识的人。"
                    };
                    break;
            }

            return reply;
        }

        protected override InterviewReply PostProcessReply(InterviewReply reply)
            => ApplyFoodQuota(reply);

        InterviewReply Reply(string intent, string translated, string[] lines, string behavior,
            string[] intel, string newQuestion = null, int trust = 0, int stress = 0, int attention = -3)
        {
            var r = new InterviewReply
            {
                intent = intent,
                translatedIntent = translated,
                behavior = behavior,
                newQuestion = newQuestion,
                trustChange = trust,
                stressChange = stress,
                attentionChange = attention
            };
            r.replyLines.AddRange(lines);
            if (intel != null)
                r.unlockedIntel.AddRange(intel);
            foreach (var leak in ForbiddenLeak)
            {
                foreach (var line in r.replyLines)
                {
                    if (line.Contains(leak))
                    {
                        r.replyLines = new List<string> { "疼。", "不记得名字。" };
                        break;
                    }
                }
            }
            return r;
        }

        InterviewReply ApplyFoodQuota(InterviewReply reply)
        {
            if (reply == null) return null;

            // Player asked about food — keep lines; spoken reply still updates the window.
            if (reply.intent == "hungry")
                return reply;

            if (!CanMentionFood)
            {
                var scrubbed = new List<string>();
                for (int i = 0; i < reply.replyLines.Count; i++)
                {
                    var line = reply.replyLines[i];
                    scrubbed.Add(TextMentionsFood(line) ? NonFoodSubstitute(reply.intent, i) : line);
                }
                reply.replyLines = scrubbed;
                if (TextMentionsFood(reply.behavior))
                    reply.behavior = "大福甩了甩尾巴。";
            }

            return reply;
        }

        /// <summary>
        /// Strip food mentions when the rolling quota is exhausted (soft prefer non-food).
        /// </summary>
        public List<string> ScrubLinesToFoodQuota(List<string> lines)
        {
            if (lines == null) return new List<string>();
            if (CanMentionFood) return lines;

            var scrubbed = new List<string>(lines.Count);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                scrubbed.Add(TextMentionsFood(line) ? NonFoodSubstitute("generic", i) : line);
            }
            return scrubbed;
        }

        public static bool TextMentionsFood(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < FoodKeywords.Length; i++)
            {
                if (text.IndexOf(FoodKeywords[i], StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        static string[] PreferFood(bool allowFood, string[] withFood, string[] withoutFood)
            => allowFood ? withFood : withoutFood;

        static List<string> PreferFood(bool allowFood, List<string> withFood, List<string> withoutFood)
            => allowFood ? withFood : withoutFood;

        static string PreferFoodBehavior(bool allowFood, string withFood, string withoutFood)
            => allowFood ? withFood : withoutFood;

        static string NonFoodSubstitute(string intent, int index)
        {
            switch (intent)
            {
                case "hungry":
                    return index == 0 ? "现在还行。" : (index == 1 ? "门口待着。" : "晒太阳。");
                case "greeting":
                    return "……在听。";
                case "name":
                    return "门口那一带。";
                case "daily":
                    return "太阳晒着就好。";
                case "feeling":
                    return "太阳晒着就好。";
                case "woman":
                    return index == 1 ? "很多次来。" : "她会走开。";
                default:
                    return "……听着呢。";
            }
        }

        protected override List<string> GetRepeatLines(string intent)
        {
            return new List<string> { "换个问法？", "这个我不太会说。" };
        }

        protected override InterviewReply HandleHostile()
        {
            return new InterviewReply
            {
                intent = "hostile",
                behavior = "大福压低耳朵，向后退开。",
                replyLines = { "……" },
                trustChange = -20,
                stressChange = 25,
                attentionChange = -15,
                systemHint = "大福感受到敌意。"
            };
        }

        public bool MeetsCompletion(HashSet<string> intel, bool boundaryHit)
        {
            bool neck = intel.Contains(IntelIds.NeckObject)
                        || intel.Contains(IntelIds.NeckPain)
                        || intel.Contains(IntelIds.NeckObjectTight)
                        || intel.Contains(IntelIds.NeckLongTermPain);
            return intel.Contains(IntelIds.PastAfraid)
                   && neck
                   && intel.Contains(IntelIds.RepeatedFeeding)
                   && intel.Contains(IntelIds.TakenAway)
                   && (intel.Contains(IntelIds.ObjectGone) || intel.Contains(IntelIds.Sleep))
                   && intel.Contains(IntelIds.ReturnedDafu)
                   && boundaryHit;
        }
    }
}
