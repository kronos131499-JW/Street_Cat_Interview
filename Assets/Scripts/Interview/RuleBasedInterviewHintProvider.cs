using System;
using System.Collections.Generic;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Notebook;

namespace StreetCat.Interview
{
    /// <summary>
    /// Contextual free-interview hints from trust / pressure / focus, last reply,
    /// remaining notebook topics, and asked intents. No network.
    /// </summary>
    public sealed class RuleBasedInterviewHintProvider : IInterviewHintProvider
    {
        const int MaxChips = 3;

        public InterviewHintBundle GetHints(InterviewHintContext ctx)
        {
            var bundle = new InterviewHintBundle();
            if (ctx == null || ctx.Subject == InterviewSubject.None)
                return bundle;

            bundle.CoachTip = ResolveCoachTip(ctx);
            BuildAskChips(ctx, bundle.AskChips);
            return bundle;
        }

        static string ResolveCoachTip(InterviewHintContext ctx)
        {
            var reply = ctx.LastReply;
            var st = ctx.Stats;

            if (reply != null)
            {
                if (!reply.understood && string.IsNullOrEmpty(ctx.LastPlayerQuestion))
                    return T("ui.interview.hint.empty", "请输入一个问题。");
                if (!reply.understood && (ctx.LastPlayerQuestion?.Length ?? 0) > 50)
                    return T("ui.interview.hint.too_long", "问题有些长，可以一次问一件事。");
                if (string.Equals(reply.intent, "hostile", StringComparison.Ordinal))
                    return ctx.Subject == InterviewSubject.Dafu
                        ? T("ui.interview.hint.hostile_dafu", "大福感受到敌意。语气柔和一些会更安全。")
                        : T("ui.interview.hint.hostile_lin", "指责性措辞会降低信任。试着把问题说得更中性。");
                if (reply.cognitiveBoundary)
                    return T("ui.interview.hint.boundary", "触及了它听不懂的人类概念。改问疼、气味、笼子或醒来后的感受。");
                if (reply.isRepeat)
                    return T("ui.interview.hint.repeat", "这个方向刚问过。换一个还没填满的笔记主题试试。");
                if (string.Equals(reply.intent, "too_broad", StringComparison.Ordinal))
                    return T("ui.interview.hint.too_broad", "范围太大了。点选下方芯片，或一次只追问一个细节。");
                if (string.Equals(reply.intent, "generic", StringComparison.Ordinal)
                    || string.Equals(reply.intent, "oob", StringComparison.Ordinal))
                {
                    return ctx.Subject == InterviewSubject.Dafu
                        ? T("ui.interview.hint.generic_dafu", "可以问问它的生活、旧伤，或认识的人。")
                        : T("ui.interview.hint.generic_lin", "可以顺着发现、投喂、送医、放归这几段追问。");
                }
                if (!string.IsNullOrEmpty(reply.newQuestion))
                    return T("ui.interview.hint.gap", "记下这个缺口：之后也许要向别人核实。");
            }

            if (st != null)
            {
                if (st.attention < 20)
                    return T("ui.interview.hint.focus_low", "对方注意力正在下降。挑最关键的问题，或先结束。");
                if (st.stress >= 55)
                    return T("ui.interview.hint.pressure_high", "压力偏高。先聊轻松的日常，少用指责或审讯式问法。");
                if (st.trust < 35)
                    return T("ui.interview.hint.trust_low", "信任偏低。先建立一点共鸣，再碰敏感话题。");
            }

            if (ctx.CanComplete)
                return T("ui.interview.hint.can_end", "关键情报差不多齐了。可以继续挖细节，或结束采访。");

            if (ctx.IsOpening)
            {
                return ctx.Subject == InterviewSubject.Dafu
                    ? T("ui.interview.hint.open_dafu", "试试从现在的生活、脖子上的伤，或认识的人问起。")
                    : T("ui.interview.hint.open_lin", "可以从第一次发现大福、连续投喂，或为什么送回社区问起。");
            }

            var topicTip = TipFromIncompleteTopics(ctx);
            if (!string.IsNullOrEmpty(topicTip))
                return topicTip;

            return T("ui.interview.hint.default", "点选下方提问方向，或按笔记里的缺口自由追问。");
        }

        static string TipFromIncompleteTopics(InterviewHintContext ctx)
        {
            var nb = ReporterNotebook.Instance;
            if (nb == null) return null;

            NotebookTopic best = null;
            foreach (var t in nb.VisibleTopics())
            {
                if (t == null || t.status == TopicStatus.Complete) continue;
                if (best == null || (int)t.status > (int)best.status)
                    best = t;
            }
            if (best == null) return null;

            string title = best.title ?? best.id;
            if (best.status == TopicStatus.Open)
                return string.Format(
                    T("ui.interview.hint.topic_open", "笔记「{0}」还有疑问——下方芯片是可追问的方向。"),
                    title);
            return string.Format(
                T("ui.interview.hint.topic_new", "新线索「{0}」还没问透。点选芯片填入后再改写发送。"),
                title);
        }

        static void BuildAskChips(InterviewHintContext ctx, List<string> chips)
        {
            void TryAdd(string q)
            {
                if (chips.Count >= MaxChips) return;
                if (string.IsNullOrWhiteSpace(q)) return;
                q = q.Trim();
                if (LooksLikeLastQuestion(ctx.LastPlayerQuestion, q)) return;
                if (chips.Contains(q)) return;
                chips.Add(q);
            }

            // 1) Immediate follow-up from last reply / Lin story beat.
            TryAdd(FollowUpChip(ctx));

            // 2) Soft rapport when meters are tense.
            if (ctx.Stats != null && (ctx.Stats.stress >= 45 || ctx.Stats.trust < 40))
                TryAdd(SoftRapportChip(ctx.Subject));

            // 3) Sensory rephrase after cognitive boundary (Dafu).
            if (ctx.LastReply != null && ctx.LastReply.cognitiveBoundary
                && ctx.Subject == InterviewSubject.Dafu)
            {
                TryAdd(T("ui.interview.chip.dafu_sensory_pain", "勒着你的东西是什么感觉？你还记得吗？"));
                TryAdd(T("ui.interview.chip.dafu_sensory_place", "那个很亮、味道很重的地方，后来怎样了？"));
            }

            // 4) Notebook-driven incomplete topics (subject-filtered), preferring Open.
            var nb = ReporterNotebook.Instance;
            if (nb != null)
            {
                foreach (var q in nb.GetContextualAskQuestions(ctx.Subject, ctx.AskedIntents, MaxChips + 2))
                    TryAdd(q);
            }

            // 5) Evidence-aware nudges from owned intel gaps.
            TryAdd(EvidenceChip(ctx));

            // 6) Static fallbacks.
            if (chips.Count < 2)
            {
                foreach (var q in FallbackChips(ctx.Subject))
                    TryAdd(q);
            }

            while (chips.Count > MaxChips)
                chips.RemoveAt(chips.Count - 1);
        }

        static string FollowUpChip(InterviewHintContext ctx)
        {
            if (ctx.LastReply == null) return null;
            var intent = ctx.LastReply.intent ?? "";

            if (ctx.Subject == InterviewSubject.Lin)
            {
                // Story-beat chain: after a beat, suggest the natural next ask.
                switch (intent)
                {
                    case "discovery":
                    case "too_broad":
                        return T("ui.interview.chip.lin_next_feed", "然后呢？您连续几天做了什么？");
                    case "feeding":
                        return T("ui.interview.chip.lin_next_capture", "伤势恶化以后，您是怎么把它送去医院的？");
                    case "capture":
                        return T("ui.interview.chip.lin_next_injury", "医院怎么说它脖子上的伤？");
                    case "injury":
                        return T("ui.interview.chip.lin_next_hospital", "手术以后，大福恢复得怎么样？");
                    case "hospital":
                        return T("ui.interview.chip.lin_next_cost", "这一趟治疗大概花了多少？");
                    case "cost":
                        return T("ui.interview.chip.lin_next_hesitate", "面对费用的时候，您有没有犹豫过？");
                    case "hesitate":
                        return T("ui.interview.chip.lin_next_release", "决定把大福送回社区之前，您考虑了哪些情况？");
                    case "release":
                    case "release_accuse":
                        return T("ui.interview.chip.lin_next_community", "放归以后，社区有人继续照看它吗？");
                }
            }
            else
            {
                switch (intent)
                {
                    case "daily":
                        return T("ui.interview.chip.dafu_next_past", "你以前也会来保安亭这边吗？");
                    case "past_fear":
                        return T("ui.interview.chip.dafu_next_neck", "你脖子以前是不是受过伤？");
                    case "neck":
                        return T("ui.interview.chip.dafu_next_woman", "有没有人经常给你送吃的？");
                    case "woman":
                        return T("ui.interview.chip.dafu_next_capture", "后来有人把你装进笼子带走了吗？");
                    case "capture":
                    case "strange_place":
                        return T("ui.interview.chip.dafu_next_return", "是谁把你带回这里的？");
                }
            }

            return null;
        }

        static string SoftRapportChip(InterviewSubject subject)
        {
            if (subject == InterviewSubject.Lin)
                return T("ui.interview.chip.lin_soft", "您愿意的话，可以从第一次注意到大福开始讲。");
            return T("ui.interview.chip.dafu_soft", "你现在还好吗？今天有吃的吗？");
        }

        static string EvidenceChip(InterviewHintContext ctx)
        {
            var gs = StreetCat.Core.GameState.Instance;
            if (gs == null) return null;
            var asked = ctx.AskedIntents;

            if (ctx.Subject == InterviewSubject.Dafu)
            {
                if (gs.HasIntel(StreetCat.Data.IntelIds.NeckPain)
                    && !HasAsked(asked, "neck"))
                    return T("ui.interview.chip.dafu_ev_neck", "你还记得脖子一直疼的时候吗？");
                if (gs.HasIntel(StreetCat.Data.IntelIds.WomanClue)
                    && !HasAsked(asked, "woman"))
                    return T("ui.interview.chip.dafu_ev_woman", "那个给你送吃的女人，后来又来过吗？");
            }
            else
            {
                if (gs.HasIntel(StreetCat.Data.IntelIds.CognitiveBoundary)
                    && !HasAsked(asked, "hospital") && !HasAsked(asked, "cost"))
                    return T("ui.interview.chip.lin_ev_hospital", "送到医院以后，医生怎么说它的伤？");
                if (gs.HasIntel(StreetCat.Data.IntelIds.ReturnedDafu)
                    && !HasAsked(asked, "release"))
                    return T("ui.interview.chip.lin_ev_release", "为什么康复后没有继续收养它？");
            }
            return null;
        }

        static IEnumerable<string> FallbackChips(InterviewSubject subject)
        {
            if (subject == InterviewSubject.Lin)
            {
                yield return T("ui.interview.chip.lin_fb1", "您是怎么注意到大福的？");
                yield return T("ui.interview.chip.lin_fb2", "为什么连续几天给它送吃的？");
                yield return T("ui.interview.chip.lin_fb3", "为什么又把它送回社区？");
            }
            else
            {
                yield return T("ui.interview.chip.dafu_fb1", "你平时一般什么时候会来这里？");
                yield return T("ui.interview.chip.dafu_fb2", "你脖子以前是不是受过伤？");
                yield return T("ui.interview.chip.dafu_fb3", "有没有人经常来找你？");
            }
        }

        static bool HasAsked(IReadOnlyCollection<string> asked, string intent)
        {
            if (asked == null || string.IsNullOrEmpty(intent)) return false;
            foreach (var a in asked)
                if (string.Equals(a, intent, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static bool LooksLikeLastQuestion(string last, string candidate)
        {
            if (string.IsNullOrEmpty(last) || string.IsNullOrEmpty(candidate)) return false;
            var a = last.Trim();
            var b = candidate.Trim();
            if (a == b) return true;
            if (a.Length >= 6 && b.IndexOf(a, StringComparison.Ordinal) >= 0) return true;
            if (b.Length >= 6 && a.IndexOf(b, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        static string T(string key, string fallback) => UiLoc.T(key, fallback);
    }
}
