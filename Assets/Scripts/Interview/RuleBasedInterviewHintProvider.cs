using System;
using System.Collections.Generic;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Notebook;

namespace StreetCat.Interview
{
    /// <summary>
    /// Contextual free-interview hints from trust / pressure / focus, last reply,
    /// remaining notebook topics, and asked intents/questions. No network.
    /// Asked chips are replaced with fresh alternatives.
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
            void TryAdd(string q, string chipIntent = null)
            {
                if (chips.Count >= MaxChips) return;
                if (string.IsNullOrWhiteSpace(q)) return;
                q = q.Trim();
                if (chips.Contains(q)) return;
                if (IsAlreadyAsked(ctx, q, chipIntent)) return;
                chips.Add(q);
            }

            // 1) Immediate follow-up from last reply / story beat.
            var follow = FollowUpChip(ctx);
            if (!string.IsNullOrEmpty(follow))
                TryAdd(follow, IntentForFollowUp(ctx));

            // 2) Soft rapport when meters are tense (once).
            if (ctx.Stats != null && (ctx.Stats.stress >= 45 || ctx.Stats.trust < 40))
                TryAdd(SoftRapportChip(ctx.Subject), "soft");

            // 3) Sensory rephrase after cognitive boundary (Dafu).
            if (ctx.LastReply != null && ctx.LastReply.cognitiveBoundary
                && ctx.Subject == InterviewSubject.Dafu)
            {
                TryAdd(T("ui.interview.chip.dafu_sensory_pain", "勒着你的东西是什么感觉？你还记得吗？"), "neck");
                TryAdd(T("ui.interview.chip.dafu_sensory_place", "那个很亮、味道很重的地方，后来怎样了？"), "strange_place");
            }

            // 4) Notebook-driven incomplete topics (subject-filtered), preferring Open.
            var nb = ReporterNotebook.Instance;
            if (nb != null)
            {
                foreach (var q in nb.GetContextualAskQuestions(ctx.Subject, ctx.AskedIntents, MaxChips + 4))
                    TryAdd(q, GuessIntent(ctx.Subject, q));
            }

            // 5) Evidence-aware nudges from owned intel gaps.
            TryAdd(EvidenceChip(ctx), null);

            // 6) Expanded fallbacks — skip any whose intent/text was already asked.
            foreach (var pair in FallbackChips(ctx.Subject))
                TryAdd(pair.Item1, pair.Item2);

            // 7) Last-resort openers (non-sticky intents) so the row never goes empty mid-interview.
            if (chips.Count == 0)
            {
                if (ctx.Subject == InterviewSubject.Lin)
                {
                    TryAdd(T("ui.interview.chip.lin_open1", "关于大福，您还记得哪个细节最清楚？"), "generic");
                    TryAdd(T("ui.interview.chip.lin_open2", "如果重新来过，您还会做同样的选择吗？"), "generic");
                }
                else
                {
                    TryAdd(T("ui.interview.chip.dafu_open1", "你现在最想做什么？"), "generic");
                    TryAdd(T("ui.interview.chip.dafu_open2", "还有什么想让我知道的吗？"), "generic");
                }
            }

            while (chips.Count > MaxChips)
                chips.RemoveAt(chips.Count - 1);
        }

        static bool IsAlreadyAsked(InterviewHintContext ctx, string candidate, string chipIntent)
        {
            if (LooksLikeLastQuestion(ctx.LastPlayerQuestion, candidate))
                return true;

            var norm = InterviewRuleEngine.NormalizeQuestion(candidate);
            if (!string.IsNullOrEmpty(norm) && ctx.AskedQuestions != null)
            {
                foreach (var a in ctx.AskedQuestions)
                {
                    if (string.IsNullOrEmpty(a)) continue;
                    if (string.Equals(a, norm, StringComparison.Ordinal)) return true;
                    if (a.Length >= 6 && norm.IndexOf(a, StringComparison.Ordinal) >= 0) return true;
                    if (norm.Length >= 6 && a.IndexOf(norm, StringComparison.Ordinal) >= 0) return true;
                }
            }

            var intent = chipIntent;
            if (string.IsNullOrEmpty(intent))
                intent = GuessIntent(ctx.Subject, candidate);
            if (!string.IsNullOrEmpty(intent) && !IsNonSticky(intent) && HasAsked(ctx.AskedIntents, intent))
                return true;

            return false;
        }

        static bool IsNonSticky(string intent)
        {
            return intent == "generic" || intent == "greeting" || intent == "followup"
                   || intent == "too_broad" || intent == "oob" || intent == "hostile" || intent == "soft";
        }

        static string FollowUpChip(InterviewHintContext ctx)
        {
            if (ctx.LastReply == null) return null;
            var intent = ctx.LastReply.intent ?? "";

            if (ctx.Subject == InterviewSubject.Lin)
            {
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

        static string IntentForFollowUp(InterviewHintContext ctx)
        {
            if (ctx?.LastReply == null) return null;
            var intent = ctx.LastReply.intent ?? "";
            if (ctx.Subject == InterviewSubject.Lin)
            {
                switch (intent)
                {
                    case "discovery":
                    case "too_broad": return "feeding";
                    case "feeding": return "capture";
                    case "capture": return "injury";
                    case "injury": return "hospital";
                    case "hospital": return "cost";
                    case "cost": return "hesitate";
                    case "hesitate": return "release";
                    case "release":
                    case "release_accuse": return "community";
                }
            }
            else
            {
                switch (intent)
                {
                    case "daily": return "past_fear";
                    case "past_fear": return "neck";
                    case "neck": return "woman";
                    case "woman": return "capture";
                    case "capture":
                    case "strange_place": return "return";
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

        static IEnumerable<(string, string)> FallbackChips(InterviewSubject subject)
        {
            if (subject == InterviewSubject.Lin)
            {
                yield return (T("ui.interview.chip.lin_fb1", "您是怎么注意到大福的？"), "discovery");
                yield return (T("ui.interview.chip.lin_fb2", "为什么连续几天给它送吃的？"), "feeding");
                yield return (T("ui.interview.chip.lin_fb4", "送到医院以后怎么样？"), "hospital");
                yield return (T("ui.interview.chip.lin_fb5", "治疗大概花了多少？"), "cost");
                yield return (T("ui.interview.chip.lin_fb6", "您有没有犹豫过？"), "hesitate");
                yield return (T("ui.interview.chip.lin_fb3", "为什么又把它送回社区？"), "release");
                yield return (T("ui.interview.chip.lin_fb7", "家里还有别的猫要照顾吗？"), "home");
                yield return (T("ui.interview.chip.lin_fb8", "放归以后社区有人照看吗？"), "community");
            }
            else
            {
                yield return (T("ui.interview.chip.dafu_fb1", "你平时一般什么时候会来这里？"), "daily");
                yield return (T("ui.interview.chip.dafu_fb4", "你以前也会来保安亭这边吗？"), "past_fear");
                yield return (T("ui.interview.chip.dafu_fb2", "你脖子以前是不是受过伤？"), "neck");
                yield return (T("ui.interview.chip.dafu_fb3", "有没有人经常来找你？"), "woman");
                yield return (T("ui.interview.chip.dafu_fb5", "后来有人把你装进笼子带走了吗？"), "capture");
                yield return (T("ui.interview.chip.dafu_fb6", "那个很亮的地方，你还记得吗？"), "strange_place");
                yield return (T("ui.interview.chip.dafu_fb7", "是谁把你带回这里的？"), "return");
                yield return (T("ui.interview.chip.dafu_fb8", "今天有吃的吗？"), "hungry");
            }
        }

        /// <summary>Lightweight intent guess so chip text can be filtered against AskedIntents.</summary>
        public static string GuessIntent(InterviewSubject subject, string q)
        {
            if (string.IsNullOrEmpty(q)) return null;
            if (subject == InterviewSubject.Lin)
            {
                if (Contains(q, "犹豫")) return "hesitate";
                if (Contains(q, "花了", "费用", "多少")) return "cost";
                if (Contains(q, "医院", "手术", "医生", "恢复")) return "hospital";
                if (Contains(q, "伤", "脖子", "麻绳")) return "injury";
                if (Contains(q, "笼子", "抓", "送去", "送医")) return "capture";
                if (Contains(q, "连续", "投喂", "罐头", "几天", "送吃")) return "feeding";
                if (Contains(q, "送回", "放归", "收养", "为什么又")) return "release";
                if (Contains(q, "社区", "照看")) return "community";
                if (Contains(q, "家里", "四只", "孩子")) return "home";
                if (Contains(q, "注意", "发现", "第一次")) return "discovery";
            }
            else
            {
                if (Contains(q, "饿", "吃的吗", "猫粮")) return "hungry";
                if (Contains(q, "带回", "送回", "谁把你")) return "return";
                if (Contains(q, "亮", "味道", "醒来")) return "strange_place";
                if (Contains(q, "笼子", "带走", "抓走")) return "capture";
                if (Contains(q, "女人", "送吃", "喂", "找你")) return "woman";
                if (Contains(q, "脖子", "伤", "勒", "疼")) return "neck";
                if (Contains(q, "怕人", "以前", "保安亭")) return "past_fear";
                if (Contains(q, "几点", "平时", "生活", "快递")) return "daily";
            }
            return null;
        }

        static bool Contains(string q, params string[] keys)
        {
            foreach (var k in keys)
                if (q.IndexOf(k, StringComparison.Ordinal) >= 0)
                    return true;
            return false;
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
            var a = InterviewRuleEngine.NormalizeQuestion(last);
            var b = InterviewRuleEngine.NormalizeQuestion(candidate);
            if (a == b) return true;
            if (a.Length >= 6 && b.IndexOf(a, StringComparison.Ordinal) >= 0) return true;
            if (b.Length >= 6 && a.IndexOf(b, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        static string T(string key, string fallback) => UiLoc.T(key, fallback);
    }
}
