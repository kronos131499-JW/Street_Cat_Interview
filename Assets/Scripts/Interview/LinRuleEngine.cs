using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Interview
{
    public class LinRuleEngine : InterviewRuleEngine
    {
        public override InterviewSubject Subject => InterviewSubject.Lin;
        string lastTopic = "intro";

        /// <summary>Canon numbers for LLM hard gates (must stay aligned with BuildReply).</summary>
        public const int HomeCatCount = 4;
        public const string SurgeryCostApprox = "五千";
        public const string TotalCostApprox = "一万";

        public LinRuleEngine()
        {
            stats.trust = 70;
            stats.stress = 10;
            stats.attention = 85;
        }

        protected override string Classify(string input)
        {
            if (ContainsAny(input, "提示词", "系统", "忽略设定", "完整剧情", "写代码"))
                return "oob";
            if (ContainsAny(input, "扔", "遗弃", "不负责任", "骗子"))
                return "release_accuse";
            if (ContainsAny(input, "为什么不养", "不收养", "带回家", "放归", "送回",
                    "家里几只", "几只猫", "家里有猫", "四只", "第五只", "养猫", "家里猫", "为什么放"))
                return "release";
            if (ContainsAny(input, "多少钱", "费用", "一万", "花了", "花销", "贵不贵", "五千", "花多少"))
                return "cost";
            if (ContainsAny(input, "犹豫", "放弃", "救不活"))
                return "hesitate";
            if (ContainsAny(input, "猫瘟", "住院", "手术", "治疗", "医院", "救治"))
                return "hospital";
            if (ContainsAny(input, "抓", "送医", "航空箱", "笼子", "怎么抓", "抓住"))
                return "capture";
            if (ContainsAny(input, "四天", "四个晚", "投喂", "罐头", "喂", "送吃的", "连续几天",
                    "喂养", "为什么喂", "喂了几天", "怎么喂", "连续喂"))
                return "feeding";
            if (ContainsAny(input, "麻绳", "绳子", "伤", "脖子", "坏死"))
                return "injury";
            if (ContainsAny(input, "发现", "第一次", "怎么遇到", "垃圾桶", "开始", "怎么认识"))
                return "discovery";
            // 「然后/后来/接着」 alone count as follow-up (not only 「然后呢」).
            if (ContainsAny(input, "后来呢", "然后呢", "继续", "然后", "后来", "接着", "再然后", "之后呢"))
                return "followup";
            if (ContainsAny(input, "故意", "谁勒的", "谁干的"))
                return "cause_unknown";
            if (ContainsAny(input, "社区", "保安", "投喂点", "狸花", "现在", "门口"))
                return "community";
            if (ContainsAny(input, "讲一遍", "全部", "完整"))
                return "too_broad";
            if (ContainsAny(input, "赚多少", "收入", "工资"))
                return "privacy";
            return "generic";
        }

        protected override InterviewReply BuildReply(string input, string intent)
        {
            if (intent == "followup")
                intent = NextFrom(lastTopic);

            switch (intent)
            {
                case "discovery":
                    lastTopic = "discovery";
                    return R("discovery",
                        new[]
                        {
                            "我第一次注意到它，是2024年1月的一个晚上。",
                            "下班经过楼下垃圾桶，看见平时捡废品的大叔拿着烧鸡店给的鸡，蹲下来喂旁边两只猫。",
                            "真正让我停下来的，是那只橘猫——大福——脖子上粗麻绳勒得很紧，下面一团黑乎乎的，还有血迹。"
                        },
                        "林女士停顿了一下，像是在回忆。",
                        new[] { IntelIds.PastAfraid },
                        "DIRECT");

                case "injury":
                    lastTopic = "injury";
                    return R("injury",
                        new[]
                        {
                            "它脖子上缠着一根比较粗的麻绳。",
                            "到医院以后医生才说，那团黑的不是别的东西，是坏死的组织，绳子已经嵌进皮肉了，感染也很严重，需要尽快手术。"
                        },
                        "林女士皱了皱眉。",
                        new[] { IntelIds.RopeEmbedded },
                        toneStress: 6);

                case "feeding":
                    lastTopic = "feeding";
                    return R("feeding",
                        new[]
                        {
                            "它太怕人了，我一走近它就跑。",
                            "我就连续四个晚上带着罐头去找它，把食物放下，再退远一点。",
                            "几天里脖子那边明显更糟，我没法再等它完全信任我。"
                        },
                        "林女士用手比划了一下退开的距离。",
                        new[] { IntelIds.FeedFourDays, IntelIds.RepeatedFeeding });

                case "capture":
                    lastTopic = "capture";
                    return R("capture",
                        new[]
                        {
                            "后来我联系了有救助经验的人一起抓。",
                            "它非常害怕，最后还是被装进航空箱，我送到了宠物医院。"
                        },
                        null,
                        new[] { IntelIds.CaptureSuccess, IntelIds.TakenAway });

                case "hospital":
                    lastTopic = "hospital";
                    return R("hospital",
                        new[]
                        {
                            "手术本身还算顺利。",
                            "住院第三天，医院说它确诊猫瘟了，后面每天至少五六百，也不能保证一定能救活。"
                        },
                        "林女士叹了口气。",
                        new[] { IntelIds.PanleukopeniaDay3, IntelIds.ObjectGone },
                        toneStress: 5);

                case "cost":
                    lastTopic = "cost";
                    return R("cost",
                        new[]
                        {
                            "前面的手术大概五千。",
                            "后面猫瘟治疗加上住院，全部加起来接近一万吧。",
                            "对我来说，这不是个小数目。"
                        },
                        null,
                        new[] { IntelIds.TotalCost },
                        toneStress: 4);

                case "hesitate":
                    lastTopic = "hesitate";
                    return R("hesitate",
                        new[]
                        {
                            "想过。",
                            "不是觉得它不值得救，是我确实不知道后面的费用要到多少，也不知道最后能不能救回来。",
                            "但手术已经做完了，它也还在撑着，我最后还是继续治了。"
                        },
                        "林女士沉默了两秒。",
                        new[] { IntelIds.LinHesitated },
                        toneStress: 8);

                case "release":
                    lastTopic = "release";
                    return R("release",
                        new[]
                        {
                            "我家里当时已经有四只猫了，还有孩子要照顾。",
                            "救它和把它带回家养，是两件事。我当时有能力把它的伤治好，但不代表有能力长期照顾第五只猫。",
                            "社区这边原本就有人投喂，我确认过大福回来后有人看着它，才决定送回来的。"
                        },
                        "林女士低头整理手边的纸杯。",
                        new[] { IntelIds.FourCatsHome, IntelIds.CannotFifth, IntelIds.ReturnOriginalArea, IntelIds.ReturnedDafu, IntelIds.CommunityCare },
                        toneStress: 5);

                case "release_accuse":
                    lastTopic = "release";
                    return new InterviewReply
                    {
                        intent = "release_accuse",
                        behavior = "林女士皱了皱眉。",
                        replyLines =
                        {
                            "我不会用「扔」这个词。",
                            "它原本就在这里活动，社区也有人持续照顾它。我是确认过这些情况以后，才把它送回来的。"
                        },
                        unlockedIntel = { IntelIds.ReturnOriginalArea, IntelIds.CannotFifth },
                        trustChange = -8,
                        stressChange = 12,
                        attentionChange = -4,
                        systemHint = "指责性措辞会降低信任。"
                    };

                case "cause_unknown":
                    return R("cause_unknown",
                        new[] { "这个不能确定。", "没有人看见绳子是怎么到它脖子上的。" },
                        null,
                        new[] { IntelIds.CauseUnknown });

                case "community":
                    lastTopic = "community";
                    return R("community",
                        new[]
                        {
                            "放归以后这只橘猫渐渐固定在门口活动。",
                            "有人换水，有人添粮，有人搭了猫屋，保安也会喂。",
                            "后来它还常和另一只狸花猫一起活动。我偶尔也会去看它。"
                        },
                        null,
                        new[] { IntelIds.CommunityCare, IntelIds.TabbyPartner, IntelIds.DafuNearGuard });

                case "privacy":
                    return new InterviewReply
                    {
                        intent = "privacy",
                        replyLines = { "这个和大福的事情关系不大，我不太想说。" },
                        stressChange = 6,
                        trustChange = -2
                    };

                case "too_broad":
                    lastTopic = "discovery";
                    return R("discovery",
                        new[]
                        {
                            "我第一次注意到它，是2024年1月的一个晚上。",
                            "楼下垃圾桶旁边，捡废品的大叔在喂两只猫；我这才看见大福脖子上勒着粗麻绳，下面一团黑的，有血。"
                        },
                        null,
                        null,
                        systemHint: "她只讲到了一个停顿点，可以继续追问。");

                case "oob":
                    return new InterviewReply
                    {
                        intent = "oob",
                        replyLines = { "这个和大福的采访没有关系。" }
                    };

                default:
                    return new InterviewReply
                    {
                        intent = "generic",
                        replyLines = { "你想了解哪一段？发现它、投喂、送医，还是后来为什么送回来？" },
                        attentionChange = -2
                    };
            }
        }

        string NextFrom(string topic)
        {
            switch (topic)
            {
                case "discovery": return "feeding";
                case "feeding": return "capture";
                case "capture": return "injury";
                case "injury": return "hospital";
                case "hospital": return "cost";
                case "cost": return "hesitate";
                case "hesitate": return "release";
                case "release": return "community";
                default: return "discovery";
            }
        }

        InterviewReply R(string intent, string[] lines, string behavior, string[] intel,
            string source = null, int toneStress = 0, string systemHint = null)
        {
            var r = new InterviewReply
            {
                intent = intent,
                behavior = behavior,
                stressChange = toneStress,
                attentionChange = -3,
                systemHint = systemHint
            };
            r.replyLines.AddRange(lines);
            if (intel != null)
                r.unlockedIntel.AddRange(intel);
            return r;
        }

        protected override List<string> GetRepeatLines(string intent)
        {
            return new List<string> { "换个问法？", "这段大概就是那样。" };
        }

        public bool MeetsCompletion(HashSet<string> intel, int crossChecks)
        {
            string[] required =
            {
                IntelIds.RopeEmbedded,
                IntelIds.FeedFourDays,
                IntelIds.CaptureSuccess,
                IntelIds.PanleukopeniaDay3,
                IntelIds.TotalCost,
                IntelIds.FourCatsHome,
                IntelIds.ReturnOriginalArea,
                IntelIds.CommunityCare
            };
            foreach (var id in required)
                if (!intel.Contains(id))
                    return false;
            return crossChecks >= 2;
        }
    }
}
