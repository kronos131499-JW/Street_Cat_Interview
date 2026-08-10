using System;
using StreetCat.Data;

namespace StreetCat.Interview
{
    /// <summary>
    /// Offline portrait expression from intent, meters, player question, and reply tone.
    /// Tags match <c>VnArt.ResolveDafuExpression</c> / <c>ResolveLinExpression</c>.
    /// </summary>
    public sealed class RuleBasedInterviewPortraitPicker : IInterviewPortraitPicker
    {
        public string PickExpression(InterviewPortraitContext ctx)
        {
            if (ctx == null || ctx.Subject == InterviewSubject.None)
                return null;

            return ctx.Subject == InterviewSubject.Lin
                ? PickLin(ctx)
                : PickDafu(ctx);
        }

        // ── Dafu: default / wary / annoyed / recall / curious / relaxed ──

        static string PickDafu(InterviewPortraitContext ctx)
        {
            var reply = ctx.Reply;
            var intent = reply?.intent ?? "";
            var st = ctx.Stats;
            var q = ctx.PlayerQuestion ?? "";
            var text = ctx.ReplyText ?? "";

            // Explicit tone from reply flags / hostile intent.
            if (string.Equals(intent, "hostile", StringComparison.Ordinal)
                || LooksHostileText(text))
                return "不满";

            if ((reply != null && reply.cognitiveBoundary)
                || string.Equals(intent, "cognitive_boundary", StringComparison.Ordinal))
                return "好奇";

            // Meter pressure before soft intents — high stress / low trust reads on the face.
            if (st != null)
            {
                if (st.stress >= 70)
                    return "不满";
                if (st.stress >= 55 || st.trust < 35)
                    return "警觉";
            }

            if (IsRecallIntentDafu(intent) || LooksRecallText(text, q))
                return "回忆";

            if (IsSoftRapport(intent, q) || string.Equals(intent, "feeling", StringComparison.Ordinal)
                || string.Equals(intent, "greeting", StringComparison.Ordinal)
                || string.Equals(intent, "hungry", StringComparison.Ordinal)
                || string.Equals(intent, "daily", StringComparison.Ordinal))
            {
                if (st == null || (st.stress < 40 && st.trust >= 40))
                    return "放松";
            }

            if (string.Equals(intent, "too_broad", StringComparison.Ordinal)
                || string.Equals(intent, "oob", StringComparison.Ordinal)
                || string.Equals(intent, "generic", StringComparison.Ordinal)
                || LooksCuriousText(text))
                return "好奇";

            if (st != null && st.stress < 25 && st.trust >= 55)
                return "放松";

            return null; // default standing
        }

        // ── Lin: default / pressure / firm / tired / guarded / recall ──

        static string PickLin(InterviewPortraitContext ctx)
        {
            var reply = ctx.Reply;
            var intent = reply?.intent ?? "";
            var st = ctx.Stats;
            var q = ctx.PlayerQuestion ?? "";
            var text = ctx.ReplyText ?? "";

            if (string.Equals(intent, "hostile", StringComparison.Ordinal)
                || string.Equals(intent, "release_accuse", StringComparison.Ordinal)
                || string.Equals(intent, "privacy", StringComparison.Ordinal)
                || LooksHostileText(text))
                return "防备";

            if (st != null)
            {
                if (st.stress >= 70)
                    return "疲惫";
                if (st.stress >= 55)
                    return "压力";
                if (st.trust < 35)
                    return "防备";
            }

            // Soft / rapport questions keep default standing when meters are calm.
            if (IsSoftRapport(intent, q) && (st == null || (st.stress < 40 && st.trust >= 40)))
                return null;

            if (string.Equals(intent, "release", StringComparison.Ordinal)
                || string.Equals(intent, "cause_unknown", StringComparison.Ordinal)
                || LooksFirmText(text))
                return "坚定";

            if (IsRecallIntentLin(intent) || LooksRecallText(text, q))
                return "回忆";

            if (string.Equals(intent, "hesitate", StringComparison.Ordinal)
                || string.Equals(intent, "cost", StringComparison.Ordinal))
                return st != null && st.stress >= 40 ? "疲惫" : "压力";

            if (string.Equals(intent, "community", StringComparison.Ordinal)
                || string.Equals(intent, "discovery", StringComparison.Ordinal)
                || string.Equals(intent, "feeding", StringComparison.Ordinal))
            {
                if (st == null || (st.stress < 40 && st.trust >= 40))
                    return null;
            }

            if (st != null && st.stress < 25 && st.trust >= 55)
                return null;

            return null;
        }

        static bool IsRecallIntentDafu(string intent)
        {
            switch (intent)
            {
                case "past_fear":
                case "neck":
                case "woman":
                case "capture":
                case "strange_place":
                case "return":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsRecallIntentLin(string intent)
        {
            switch (intent)
            {
                case "discovery":
                case "feeding":
                case "capture":
                case "injury":
                case "hospital":
                case "hesitate":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsSoftRapport(string intent, string question)
        {
            if (string.Equals(intent, "soft", StringComparison.Ordinal)
                || string.Equals(intent, "followup", StringComparison.Ordinal))
                return true;
            if (string.IsNullOrEmpty(question)) return false;
            return Contains(question,
                "还好吗", "今天有吃", "愿意的话", "谢谢", "辛苦",
                "慢慢说", "不着急", "没关系");
        }

        static bool LooksHostileText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return Contains(text, "后退", "压低耳朵", "皱了皱眉", "不适", "不想说");
        }

        static bool LooksRecallText(string text, string question)
        {
            if (!string.IsNullOrEmpty(text) && Contains(text,
                "以前", "记得", "第一次", "后来", "那天", "那时候", "回忆"))
                return true;
            if (!string.IsNullOrEmpty(question) && Contains(question,
                "以前", "还记得", "第一次", "那天", "那时候"))
                return true;
            return false;
        }

        static bool LooksCuriousText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return Contains(text, "歪头", "歪着头", "嗯？", "什么？", "那是什么", "眨了眨眼");
        }

        static bool LooksFirmText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return Contains(text, "不会用", "两件事", "确认过", "不能确定", "不太想说");
        }

        static bool Contains(string s, params string[] keys)
        {
            foreach (var k in keys)
                if (s.IndexOf(k, StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }
    }
}
