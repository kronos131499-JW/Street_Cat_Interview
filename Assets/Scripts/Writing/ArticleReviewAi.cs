using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Loc;
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

            bool en = GameSettings.IsEnglish;
            int length = ArticleDraftAi.CountLength(assembler.Body);
            var style = en ? ReviewStyleEn() : ReviewStyleZh();

            var facts = new StringBuilder();
            facts.AppendLine(en ? "[Authoritative facts]" : "【权威台词/事实】");
            facts.AppendLine((en ? "Angle: " : "立意：") + ArticleAssembler.TitleFor(dir));
            facts.AppendLine(en
                ? "Approximate word count: " + length
                : "成稿有效字数（约）：" + length);
            facts.AppendLine(en ? "Selected materials:" : "已选素材：");
            if (selected != null)
            {
                foreach (var id in selected)
                {
                    var m = MaterialCatalog.Get(id);
                    if (m == null) continue;
                    facts.AppendLine("- " + m.id + " " + m.LocalizedTitle + (en ? ": " : "：") + m.LocalizedBody);
                }
            }
            facts.AppendLine();
            facts.AppendLine(en ? "[Article]" : "【成稿正文】");
            facts.AppendLine(assembler.Body.Trim());
            facts.AppendLine();
            facts.AppendLine(en ? "Review strictly. Output JSON only." : "严格审核。只输出 JSON。");

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

                bool pass = branch == "A" && dto.score >= 70;
                if (!pass && branch == "A")
                    branch = "C";

                string passMark = UiLoc.T("ui.writing.review.pass", "审核结果——通过");
                string failMark = UiLoc.T("ui.writing.review.fail", "审核结果——退回");
                string review = string.IsNullOrWhiteSpace(dto.review)
                    ? (pass
                        ? passMark + "\n\n" + UiLoc.T("ui.writing.review.pass_1", "沈禾：看完了。可以发。")
                        : failMark + "\n\n" + UiLoc.T("ui.writing.review.fail_short", "沈禾：这稿还得改。"))
                    : AddressReporter(dto.review.Trim().Replace("\\n", "\n"));

                if (pass && review.IndexOf(passMark, StringComparison.Ordinal) < 0
                    && review.IndexOf("通过", StringComparison.Ordinal) < 0)
                    review = passMark + "\n\n" + review;
                if (!pass && review.IndexOf(failMark, StringComparison.Ordinal) < 0
                    && review.IndexOf("退回", StringComparison.Ordinal) < 0)
                    review = failMark + "\n\n" + review;

                assembler.ApplyReview(dto.score, branch, review);
            }

            onDone?.Invoke();
        }

        static string ReviewStyleEn() =>
            "You are Shen He, editor-in-chief of Here & Now, and a strict reviewer. "
            + "You are speaking to your reporter, Ling. Address her as Ling. "
            + "Ms. Lin is the resident in the story who rescued the cat, not the person you are reviewing. Never call Ling Ms. Lin. "
            + "Review the reporter's article against its angle and the selected materials. "
            + "Judge only the given text and materials; never add new facts. "
            + "You must reject (pass=false) in cases including: "
            + "1) obvious logical gaps or skipped key events; "
            + "2) very weak writing, a flat list of facts, almost no development; "
            + "3) materials that badly mismatch the angle; "
            + "4) speculation stated as fact; "
            + "5) far too short (clearly under " + ArticleDraftAi.TargetMinWordsEn + " words); "
            + "6) one of the four narrative sections is effectively empty. "
            + "Pass only when the structure is clear, the facts are restrained, the materials support the angle, and the piece is substantial. "
            + "Passing scores are usually 70-92; clear problems score 35-65 and are rejected. "
            + "Output a single line of JSON (no markdown fences) with fields: "
            + "{\"pass\":bool,\"score\":integer 0-100,\"branch\":\"A|B|C|D\",\"review\":\"feedback in Shen He's voice, in English, use \\n for line breaks\"}. "
            + "branch: A=pass; B=angle/material mismatch; C=poor logic, weak writing, or too short; D=speculation as fact.";

        static string ReviewStyleZh() =>
                "你是《街角专访》的主编沈禾，审核标准严格。"
                + "你在对记者小凌说话，称呼她小凌。林女士是稿件里的救助者，不是你面前的记者，不要把小凌叫成林女士。"
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

        /// <summary>
        /// Models sometimes open the note by addressing Ling as Ms. Lin.
        /// Only the direct address is rewritten; mentions of Ms. Lin in the story stay.
        /// </summary>
        static string AddressReporter(string review)
        {
            if (string.IsNullOrEmpty(review)) return review;
            review = System.Text.RegularExpressions.Regex.Replace(
                review, @"(?m)(^|\n)(\s*)Ms\.?\s+Lin(\s*[,:])", "$1$2Ling$3");
            review = System.Text.RegularExpressions.Regex.Replace(
                review, @"(?m)(^|\n)(\s*)林女士(\s*[，,：:])", "$1$2小凌$3");
            return review;
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
