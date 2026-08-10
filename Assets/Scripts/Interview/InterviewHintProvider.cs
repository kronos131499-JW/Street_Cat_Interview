using System;
using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Interview
{
    /// <summary>
    /// Snapshot used to pick coach tips + ask chips for free interview.
    /// </summary>
    public sealed class InterviewHintContext
    {
        public InterviewSubject Subject;
        public InterviewerStats Stats;
        public InterviewReply LastReply;
        public string LastPlayerQuestion;
        public IReadOnlyCollection<string> AskedIntents;
        /// <summary>Normalized questions already sent — chips matching these are replaced.</summary>
        public IReadOnlyCollection<string> AskedQuestions;
        public bool CanComplete;
        public bool IsOpening;
    }

    /// <summary>Player-facing hint bundle: short coach line + fillable question chips.</summary>
    public sealed class InterviewHintBundle
    {
        public string CoachTip;
        public readonly List<string> AskChips = new List<string>();
    }

    /// <summary>
    /// Pluggable interview hint source. Default is rule-based (no network).
    /// Swap <see cref="InterviewHintService.Provider"/> for an LLM-backed editor/dev assist later;
    /// Play Mode must not require a live cloud LLM.
    /// </summary>
    public interface IInterviewHintProvider
    {
        InterviewHintBundle GetHints(InterviewHintContext ctx);
    }

    /// <summary>Facade so UI / controllers resolve hints without knowing the provider type.</summary>
    public static class InterviewHintService
    {
        /// <summary>
        /// Default: <see cref="RuleBasedInterviewHintProvider"/>.
        /// Assign an LLM (or hybrid) provider in editor tools / future online assist; keep a rule fallback.
        /// </summary>
        public static IInterviewHintProvider Provider { get; set; } = new RuleBasedInterviewHintProvider();

        public static InterviewHintBundle GetHints(InterviewHintContext ctx)
        {
            var p = Provider ?? new RuleBasedInterviewHintProvider();
            return p.GetHints(ctx) ?? new InterviewHintBundle();
        }

        public static InterviewHintContext BuildContext(InterviewController ic)
        {
            var ctx = new InterviewHintContext();
            if (ic == null)
                return ctx;
            ctx.Subject = ic.Subject;
            ctx.Stats = ic.Stats;
            ctx.LastReply = ic.LastReply;
            ctx.LastPlayerQuestion = ic.LastPlayerQuestion;
            ctx.AskedIntents = ic.AskedIntents;
            ctx.AskedQuestions = ic.AskedQuestions;
            ctx.CanComplete = ic.CanComplete();
            ctx.IsOpening = string.IsNullOrEmpty(ic.LastPlayerQuestion) && ic.LastReply == null;
            return ctx;
        }
    }
}
