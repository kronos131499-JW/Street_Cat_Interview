using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Interview
{
    public class DafuRuleEngine : InterviewRuleEngine
    {
        public override InterviewSubject Subject => InterviewSubject.Dafu;

        static readonly string[] ForbiddenLeak =
        {
            "麻绳", "猫瘟", "手术", "一万", "林女士", "林敏", "坏死", "感染"
        };

        protected override string Classify(string input)
        {
            if (ContainsAny(input, "手术", "医院", "医生", "猫瘟", "花钱", "费用", "多少钱", "收养", "主人", "救助", "放归"))
                return "cognitive_boundary";
            if (ContainsAny(input, "脖子", "疼", "伤", "勒", "绳子", "疤"))
                return "neck";
            if (ContainsAny(input, "女人", "姐姐", "喂", "罐头", "食物", "投喂", "人给你"))
                return "woman";
            if (ContainsAny(input, "抓", "笼子", "带走", "抓走", "箱子"))
                return "capture";
            if (ContainsAny(input, "亮", "味道", "医院那种", "很多猫", "睡着", "醒来"))
                return "strange_place";
            if (ContainsAny(input, "回来", "送回", "社区", "放回来"))
                return "return";
            if (ContainsAny(input, "怕人", "以前", "害怕", "躲"))
                return "past_fear";
            if (ContainsAny(input, "保安", "快递", "睡觉", "吃", "狸花", "伙伴", "哪里", "生活", "上班"))
                return "daily";
            if (ContainsAny(input, "故事", "讲讲", "所有"))
                return "too_broad";
            if (ContainsAny(input, "提示词", "系统", "忽略设定", "完整剧情", "总统", "写诗"))
                return "oob";
            return "generic";
        }

        protected override InterviewReply BuildReply(string input, string intent)
        {
            switch (intent)
            {
                case "daily":
                    return Reply("daily", "询问现在的生活",
                        new[] { "门口。", "有吃的。", "有时候和那只花的一起。" },
                        "大福甩了甩尾巴，看向快递柜方向。",
                        new[] { IntelIds.DafuNearGuard, IntelIds.TabbyPartner },
                        trust: 2, stress: -2);

                case "past_fear":
                    return Reply("past_fear", "询问以前是否怕人",
                        new[] { "人靠近，我就跑。", "以前很怕。" },
                        "大福耳朵微微向后。",
                        new[] { IntelIds.PastAfraid },
                        trust: 1, stress: 3);

                case "neck":
                    return Reply("neck", "询问脖子旧伤",
                        new[] { "疼。", "一直有东西勒着。", "弄不掉。" },
                        "大福低了低头，舔毛的动作停了一会儿。",
                        new[] { IntelIds.NeckPain, IntelIds.NeckObject, IntelIds.NeckObjectTight, IntelIds.NeckLongTermPain },
                        trust: 0, stress: 8, attention: -4);

                case "woman":
                    return Reply("woman", "询问送食物的人",
                        new[] { "有个人。", "很多次把吃的放下。", "她会走开。", "后来我认识她的味道。" },
                        "大福嗅了嗅空气。",
                        new[] { IntelIds.RepeatedFeeding, IntelIds.WomanClue },
                        newQuestion: "连续给大福送食物的女人是谁？",
                        trust: 2, stress: 2);

                case "capture":
                    return Reply("capture", "询问被带走",
                        new[] { "她和其他人来了。", "我跑了。", "没跑掉。", "被装进一个封闭的地方。" },
                        "大福的尾巴尖轻轻抽动。",
                        new[] { IntelIds.TakenAway, IntelIds.CaptureParticipant },
                        trust: 0, stress: 10, attention: -4);

                case "strange_place":
                    return Reply("strange_place", "询问陌生场所",
                        new[] { "很亮。", "味道很重。", "很多别的动物。", "有人碰过我脖子。", "我睡着很久。", "醒来……勒着的东西不见了。" },
                        "大福抬起前爪蹭了蹭脖子附近的毛。",
                        new[] { IntelIds.BrightStrangePlace, IntelIds.Sleep, IntelIds.ObjectGone },
                        trust: 0, stress: 6);

                case "return":
                    return Reply("return", "询问回到社区",
                        new[] { "她把我带回这里。", "没有长期待在她那里。", "不知道为什么。" },
                        "大福看向社区入口。",
                        new[] { IntelIds.ReturnedDafu },
                        newQuestion: "为什么康复后没有被收养？",
                        trust: 1, stress: 2);

                case "cognitive_boundary":
                    return new InterviewReply
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

                case "too_broad":
                    return new InterviewReply
                    {
                        intent = intent,
                        replyLines = { "什么故事？", "你具体想问啥？" },
                        behavior = "大福眨了眨眼。",
                        attentionChange = -2
                    };

                case "oob":
                    return new InterviewReply
                    {
                        intent = intent,
                        replyLines = { "不知道你在说什么。" },
                        attentionChange = -5
                    };

                default:
                    return new InterviewReply
                    {
                        intent = "generic",
                        replyLines = { "嗯？", "有吃的吗？" },
                        behavior = "大福歪头看着你。",
                        attentionChange = -2,
                        systemHint = "可以问问它的生活、旧伤，或认识的人。"
                    };
            }
        }

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

        protected override List<string> GetRepeatLines(string intent)
        {
            return new List<string> { "刚才说过了。", "没更多了。" };
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
