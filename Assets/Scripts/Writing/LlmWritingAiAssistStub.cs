using System.Collections;
using System.Text;
using StreetCat.Interview;
using UnityEngine;

namespace StreetCat.Writing
{
    /// <summary>
    /// Optional LLM-backed writing assist wrapper.
    /// Sync <see cref="Suggest"/> always uses <see cref="RuleBasedWritingAiAssist"/> (offline-safe).
    /// Call <see cref="PolishDraftCoroutine"/> only when <see cref="LlmClient"/> has an API key —
    /// never invents intel; polish is rewrite-only. No secrets in repo: use env / PlayerPrefs
    /// (<c>STREETCAT_LLM_API_KEY</c>) via StreetCat/LLM menus.
    /// </summary>
    public sealed class LlmWritingAiAssistStub : IWritingAiAssist
    {
        readonly RuleBasedWritingAiAssist rules = new RuleBasedWritingAiAssist();

        public WritingAssistBundle Suggest(WritingAssistContext ctx)
        {
            var bundle = rules.Suggest(ctx);
            bool llmReady = LlmClient.Instance != null && LlmClient.Instance.IsConfigured;
            bundle.ProviderNote = llmReady ? "rule+llm-ready" : "rule";
            return bundle;
        }

        /// <summary>
        /// Optional polish of an already-assembled draft. Yields null text on skip/failure
        /// (caller keeps the rule draft). Safe to call from Play Mode when a key is set.
        /// </summary>
        public static IEnumerator PolishDraftCoroutine(string draft, System.Action<string> onDone)
        {
            var llm = LlmClient.Instance;
            if (llm == null || !llm.IsConfigured || string.IsNullOrWhiteSpace(draft))
            {
                onDone?.Invoke(null);
                yield break;
            }

            var style =
                "你是《街角专访》的写稿助手。在不新增事实、人名、数字或因果的前提下，把特稿写得更充实："
                + "总字数（不计空白）尽量达到 " + ArticleDraftAi.TargetMinChars + " 字以上；"
                + "保留小标题；补场景过渡与叙述节奏，但每句都必须能从原稿事实推出。"
                + "只输出成稿全文。";
            var facts = new StringBuilder();
            facts.AppendLine("【权威台词/事实】");
            facts.AppendLine(draft.Trim());
            facts.AppendLine();
            facts.AppendLine("在事实边界内扩写至充实特稿。只输出全文。");

            string polished = null;
            yield return llm.RephraseCoroutine(style, facts.ToString(), "", text => polished = text);
            onDone?.Invoke(string.IsNullOrWhiteSpace(polished) ? null : polished.Trim());
        }
    }
}
