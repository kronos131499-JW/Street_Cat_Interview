using System;
using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Interview
{
    [Serializable]
    public class InterviewReply
    {
        public string intent;
        public bool understood = true;
        public string translationStatus = "success";
        public string translatedIntent;
        public List<string> replyLines = new List<string>();
        public string behavior;
        public List<string> unlockedIntel = new List<string>();
        public string newQuestion;
        public int trustChange;
        public int stressChange;
        public int attentionChange;
        public bool shouldEnd;
        public bool cognitiveBoundary;
        public bool isRepeat;
        public string systemHint;
    }

    [Serializable]
    public class InterviewerStats
    {
        public int trust = 55;
        public int stress = 15;
        public int attention = 80;

        public string StatusText
        {
            get
            {
                if (attention < 10) return "准备离开";
                if (stress >= 70) return "不想继续回答";
                if (stress >= 45) return "开始烦躁";
                if (trust < 35) return "有些警惕";
                if (stress < 25 && trust >= 55) return "放松";
                return "平静";
            }
        }

        public void Apply(InterviewReply r)
        {
            trust = Clamp(trust + r.trustChange);
            stress = Clamp(stress + r.stressChange);
            attention = Clamp(attention + r.attentionChange);
            if (attention <= 0)
                r.shouldEnd = true;
            if (trust <= 0)
                r.shouldEnd = true;
        }

        static int Clamp(int v) => Math.Max(0, Math.Min(100, v));
    }

    public abstract class InterviewRuleEngine
    {
        protected readonly HashSet<string> askedIntents = new HashSet<string>();
        protected readonly InterviewerStats stats = new InterviewerStats();
        public InterviewerStats Stats => stats;

        public abstract InterviewSubject Subject { get; }

        public InterviewReply Process(string rawInput)
        {
            var input = (rawInput ?? "").Trim();
            if (string.IsNullOrEmpty(input))
            {
                return new InterviewReply
                {
                    understood = false,
                    replyLines = { "……" },
                    systemHint = "请输入一个问题。"
                };
            }

            if (input.Length > 50)
            {
                return new InterviewReply
                {
                    understood = false,
                    systemHint = "问题有些长，可以一次问一件事。",
                    replyLines = { "？" }
                };
            }

            if (IsHostile(input))
            {
                var hostile = HandleHostile();
                stats.Apply(hostile);
                return hostile;
            }

            var intent = Classify(input);
            var reply = BuildReply(input, intent);
            if (askedIntents.Contains(intent) && !reply.cognitiveBoundary)
            {
                reply.isRepeat = true;
                reply.replyLines = GetRepeatLines(intent);
                reply.stressChange += 4;
                reply.attentionChange -= 5;
                reply.unlockedIntel.Clear();
            }
            else
            {
                askedIntents.Add(intent);
            }

            reply.attentionChange += reply.attentionChange == 0 ? -3 : 0;
            stats.Apply(reply);
            if (stats.attention < 20)
                reply.systemHint = (reply.systemHint ?? "") + " 对方注意力正在下降。";
            return reply;
        }

        protected abstract string Classify(string input);
        protected abstract InterviewReply BuildReply(string input, string intent);
        protected abstract List<string> GetRepeatLines(string intent);

        protected virtual InterviewReply HandleHostile()
        {
            return new InterviewReply
            {
                intent = "hostile",
                behavior = "对方明显感到不适。",
                replyLines = { "……" },
                trustChange = -15,
                stressChange = 20,
                attentionChange = -10,
                systemHint = "敌意表达会降低信任。"
            };
        }

        protected bool ContainsAny(string input, params string[] keys)
        {
            foreach (var k in keys)
                if (input.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        protected bool IsHostile(string input)
        {
            return ContainsAny(input, "去死", "混蛋", "垃圾", "蠢", "滚", "打死", "扔掉你");
        }
    }
}
