using System.Collections;
using UnityEngine;
using StreetCat.Data;

namespace StreetCat.Interview
{
    /// <summary>
    /// Optional LLM-backed portrait picker wrapper.
    /// Sync <see cref="PickExpression"/> always uses <see cref="RuleBasedInterviewPortraitPicker"/>
    /// (offline-safe). Call <see cref="RefineExpressionCoroutine"/> only when
    /// <see cref="LlmClient"/> has an API key — never required for Play Mode.
    /// </summary>
    public sealed class LlmInterviewPortraitPickerStub : IInterviewPortraitPicker
    {
        readonly RuleBasedInterviewPortraitPicker rules = new RuleBasedInterviewPortraitPicker();

        public string PickExpression(InterviewPortraitContext ctx) => rules.PickExpression(ctx);

        /// <summary>
        /// Optional refine of a rule-picked expression. Yields null on skip/failure
        /// (caller keeps the rule tag). Safe to call from Play Mode when a key is set.
        /// </summary>
        public static IEnumerator RefineExpressionCoroutine(
            InterviewPortraitContext ctx,
            string ruleExpression,
            System.Action<string> onDone)
        {
            var llm = LlmClient.Instance;
            if (llm == null || !llm.IsConfigured || ctx == null
                || ctx.Subject == InterviewSubject.None)
            {
                onDone?.Invoke(null);
                yield break;
            }

            var who = ctx.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            var allowed = ctx.Subject == InterviewSubject.Dafu
                ? "常态,警觉,不满,回忆,好奇,放松"
                : "常态,压力,坚定,疲惫,防备,回忆";
            var style =
                "你是《街角专访》的立绘导演。根据提问与回答，只输出一个表情标签。"
                + "可选：" + allowed + "。不要解释，不要引号。";
            var facts =
                "受访者：" + who
                + "\n提问：" + (ctx.PlayerQuestion ?? "")
                + "\n回答：\n" + (ctx.ReplyText ?? "")
                + "\n规则建议：" + (ruleExpression ?? "常态")
                + "\n只输出一个标签。";

            string refined = null;
            yield return llm.RephraseCoroutine(style, facts, ctx.PlayerQuestion ?? "",
                text => refined = text);

            var tag = NormalizeTag(ctx.Subject, refined);
            onDone?.Invoke(tag);
        }

        static string NormalizeTag(InterviewSubject subject, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var t = raw.Trim().Trim('"', '\'', '「', '」', '。', '.', '！', '!');
            // Take first token / line only.
            int nl = t.IndexOf('\n');
            if (nl >= 0) t = t.Substring(0, nl).Trim();
            int comma = t.IndexOf('，');
            if (comma < 0) comma = t.IndexOf(',');
            if (comma >= 0) t = t.Substring(0, comma).Trim();

            if (subject == InterviewSubject.Dafu)
            {
                if (Contains(t, "不满", "annoyed")) return "不满";
                if (Contains(t, "警", "wary")) return "警觉";
                if (Contains(t, "回忆", "recall")) return "回忆";
                if (Contains(t, "好奇", "curious")) return "好奇";
                if (Contains(t, "放松", "relax")) return "放松";
                if (Contains(t, "常态", "default")) return null;
                return null;
            }

            if (Contains(t, "压力", "pressure")) return "压力";
            if (Contains(t, "坚定", "firm")) return "坚定";
            if (Contains(t, "疲惫", "tired")) return "疲惫";
            if (Contains(t, "防备", "guarded")) return "防备";
            if (Contains(t, "回忆", "recall")) return "回忆";
            if (Contains(t, "常态", "default")) return null;
            return null;
        }

        static bool Contains(string s, params string[] keys)
        {
            foreach (var k in keys)
                if (s.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
