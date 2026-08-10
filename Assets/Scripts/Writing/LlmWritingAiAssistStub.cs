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
                "你是《街角专访》的写稿助手。只润色记者成稿的文笔，禁止新增事实、人名、数字或因果。"
                + "保留原有段落标题与「（表述）」行。只输出润色后的全文。";
            var facts = new StringBuilder();
            facts.AppendLine("【权威台词/事实】");
            facts.AppendLine(draft.Trim());
            facts.AppendLine();
            facts.AppendLine("只改写表达，禁止新增信息。只输出润色后的报道全文。");

            string polished = null;
            yield return llm.RephraseCoroutine(style, facts.ToString(), "", text => polished = text);
            onDone?.Invoke(string.IsNullOrWhiteSpace(polished) ? null : polished.Trim());
        }
    }
}
