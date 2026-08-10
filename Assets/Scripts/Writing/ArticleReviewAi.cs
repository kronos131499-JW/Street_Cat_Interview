using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using StreetCat.Data;
using StreetCat.Interview;
using UnityEngine;

namespace StreetCat.Writing
{
    /// <summary>
    /// Shen He article review via optional LLM. Stricter than rubber-stamp;
    /// rule-based fallback when no key / failure.
    /// </summary>
    public static class ArticleReviewAi
    {
        [Serializable]
        class ReviewDto
        {
            public bool pass = true;
            public int score = 80;
            public string branch = "A";
            public string review = "";
        }

        /// <summary>
        /// Review the submitted article (rule + optional LLM).
        /// Expansion/polish is desk-only via <see cref="ArticleDraftAi.ExpandCoroutine"/>;
        /// when <paramref name="skipExpand"/> is true (default for desk submit), never expands here.
        /// Updates review fields on assembler; does not rewrite body when skipping expand.
        /// </summary>
        public static IEnumerator ReviewCoroutine(
            ArticleAssembler assembler,
            WritingDirection dir,
            List<string> selected,
            Action onDone,
            bool skipExpand = true)
        {
            if (assembler == null)
            {
                onDone?.Invoke();
                yield break;
            }

            if (!skipExpand)
                yield return ArticleDraftAi.ExpandCoroutine(assembler, dir, selected, null);

            // Rule baseline, then optionally override with LLM feedback.
            assembler.ApplyRuleReview(dir, selected);

            var llm = LlmClient.Instance;
            if (llm == null || !llm.IsConfigured || string.IsNullOrWhiteSpace(assembler.Body))
            {
                onDone?.Invoke();
                yield break;
            }

            int chars = ArticleDraftAi.CountContentChars(assembler.Body);
            var style =
                "你是《街角专访》的主编沈禾，审核标准严格。"
                + "根据记者成稿、写作立意与已选素材给出审核。"
                + "只根据给定正文与素材评价，禁止新增新闻事实。"
                + "必须打回（pass=false）的情况包括但不限于："
                + "①明显逻辑断裂或关键过程跳戏；"
                + "②文笔过差、流水账、几乎没有展开；"
                + "③选材与立意严重不匹配；"
                + "④把猜测写成铁板事实；"
                + "⑤正文过短（有效字数明显不足 " + ArticleDraftAi.TargetMinChars + "）；"
                + "⑥四个叙事段落里有的形同虚设。"
                + "只有结构清楚、事实克制、选材撑得住立意、篇幅充实，才可通过。"
                + "通过分通常 70–92；问题明显时 35–65 并退回。"
                + "只输出一行 JSON（不要 markdown 代码块），字段："
                + "{\"pass\":bool,\"score\":0-100整数,\"branch\":\"A|B|C|D\",\"review\":\"沈禾口吻评语，多行用\\n\"}。"
                + "branch：A=通过；B=立意/选材不匹配；C=逻辑差/写太差/篇幅不足；D=把推测当事实。";

            var facts = new StringBuilder();
            facts.AppendLine("【权威台词/事实】");
            facts.AppendLine("立意：" + ArticleAssembler.TitleFor(dir));
            facts.AppendLine("成稿有效字数（约）：" + chars);
            facts.AppendLine("已选素材：");
            if (selected != null)
            {
                foreach (var id in selected)
                {
                    var m = MaterialCatalog.Get(id);
                    if (m == null) continue;
                    facts.AppendLine("- " + m.id + " " + m.title + "：" + m.body);
                }
            }
            facts.AppendLine();
            facts.AppendLine("【成稿正文】");
            facts.AppendLine(assembler.Body.Trim());
            facts.AppendLine();
            facts.AppendLine("严格审核。只输出 JSON。");

            string raw = null;
            yield return llm.RephraseCoroutine(style, facts.ToString(), "", text => raw = text);

            if (TryParseReview(raw, out var dto))
            {
                string branch = string.IsNullOrWhiteSpace(dto.branch)
                    ? (dto.pass ? "A" : "C")
                    : dto.branch.Trim().ToUpperInvariant();
                if (branch.Length > 1) branch = branch.Substring(0, 1);
                if ("ABCD".IndexOf(branch, StringComparison.Ordinal) < 0)
                    branch = dto.pass ? "A" : "C";

                // Enforce length floor even if model is soft.
                if (chars < ArticleDraftAi.TargetMinChars * 0.85f && branch == "A")
                {
                    branch = "C";
                    dto.pass = false;
                    if (dto.score >= 70) dto.score = 58;
                    if (string.IsNullOrWhiteSpace(dto.review))
                        dto.review = "篇幅不够。特稿不能写成像备忘录，回去把过程写开。";
                }

                bool pass = branch == "A" && dto.score >= 70;
                if (!pass && branch == "A")
                    branch = "C";

                string review = string.IsNullOrWhiteSpace(dto.review)
                    ? (pass
                        ? "审核结果——通过\n\n沈禾：看完了。可以发。"
                        : "审核结果——退回\n\n沈禾：这稿还得改。")
                    : dto.review.Trim().Replace("\\n", "\n");

                if (pass && review.IndexOf("通过", StringComparison.Ordinal) < 0)
                    review = "审核结果——通过\n\n" + review;
                if (!pass && review.IndexOf("退回", StringComparison.Ordinal) < 0)
                    review = "审核结果——退回\n\n" + review;

                assembler.ApplyReview(dto.score, branch, review);
            }

            onDone?.Invoke();
        }

        static bool TryParseReview(string raw, out ReviewDto dto)
        {
            dto = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim();
            if (s.StartsWith("```", StringComparison.Ordinal))
            {
                int firstNl = s.IndexOf('\n');
                int lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNl >= 0 && lastFence > firstNl)
                    s = s.Substring(firstNl + 1, lastFence - firstNl - 1).Trim();
            }

            int start = s.IndexOf('{');
            int end = s.LastIndexOf('}');
            if (start < 0 || end <= start) return false;
            s = s.Substring(start, end - start + 1);

            try
            {
                dto = JsonUtility.FromJson<ReviewDto>(s);
                if (dto == null) return false;
                dto.score = Mathf.Clamp(dto.score, 0, 100);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
