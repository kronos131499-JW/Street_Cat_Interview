using System;
using System.Text;
using StreetCat.Data;

namespace StreetCat.Interview
{
    /// <summary>
    /// Snapshot used to pick the interview subject's portrait expression.
    /// </summary>
    public sealed class InterviewPortraitContext
    {
        public InterviewSubject Subject;
        public InterviewerStats Stats;
        public InterviewReply Reply;
        public string PlayerQuestion;
        /// <summary>Joined reply lines (rule or final LLM) for tone heuristics.</summary>
        public string ReplyText;
    }

    /// <summary>
    /// Pluggable free-interview portrait expression picker.
    /// Default is rule-based (no network). Swap
    /// <see cref="InterviewPortraitService.Provider"/> for an LLM-backed picker later;
    /// Play Mode must not require a live cloud LLM.
    /// Returns a Chinese / English expression tag understood by <c>VnArt.Resolve*Expression</c>
    /// (e.g. 不满 / 回忆 / 放松), or null / empty for the subject's default standing.
    /// </summary>
    public interface IInterviewPortraitPicker
    {
        string PickExpression(InterviewPortraitContext ctx);
    }

    /// <summary>Facade so UI resolves expressions without knowing the provider type.</summary>
    public static class InterviewPortraitService
    {
        /// <summary>
        /// Default: <see cref="RuleBasedInterviewPortraitPicker"/>.
        /// Assign <see cref="LlmInterviewPortraitPickerStub"/> (or a hybrid) in editor / future online assist; keep a rule fallback.
        /// </summary>
        public static IInterviewPortraitPicker Provider { get; set; } = new RuleBasedInterviewPortraitPicker();

        public static string PickExpression(InterviewPortraitContext ctx)
        {
            var p = Provider ?? new RuleBasedInterviewPortraitPicker();
            return p.PickExpression(ctx);
        }

        public static InterviewPortraitContext BuildContext(InterviewController ic)
        {
            var ctx = new InterviewPortraitContext();
            if (ic == null)
                return ctx;
            ctx.Subject = ic.Subject;
            ctx.Stats = ic.Stats;
            ctx.Reply = ic.LastReply;
            ctx.PlayerQuestion = ic.LastPlayerQuestion;
            ctx.ReplyText = JoinReplyText(ic.LastReply);
            return ctx;
        }

        /// <summary>Build from an explicit reply (e.g. deferred LLM finish with updated lines).</summary>
        public static InterviewPortraitContext BuildContext(
            InterviewSubject subject,
            InterviewerStats stats,
            InterviewReply reply,
            string playerQuestion)
        {
            return new InterviewPortraitContext
            {
                Subject = subject,
                Stats = stats,
                Reply = reply,
                PlayerQuestion = playerQuestion,
                ReplyText = JoinReplyText(reply)
            };
        }

        static string JoinReplyText(InterviewReply reply)
        {
            if (reply?.replyLines == null || reply.replyLines.Count == 0)
                return reply?.behavior ?? "";
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(reply.behavior))
                sb.Append(reply.behavior).Append('\n');
            foreach (var line in reply.replyLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    sb.Append(line.Trim()).Append('\n');
            }
            return sb.ToString();
        }
    }
}
