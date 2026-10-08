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
    /// Expands a stub article into a fuller feature (≥1000 字 / ≥600 English words when possible).
    /// LLM when keyed; otherwise offline narrative built from selected cards only.
    /// Chinese prose here is the source; English comes from Resources/Loc/hardtext_en.json.
    /// </summary>
    public static class ArticleDraftAi
    {
        public const int TargetMinChars = 1000;
        public const int TargetMinWordsEn = 600;

        /// <summary>Length goal in the unit of the current language (字 or words).</summary>
        public static int TargetLength => GameSettings.IsEnglish ? TargetMinWordsEn : TargetMinChars;

        public static int CountContentChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int n = 0;
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (c == '【' || c == '】' || c == '《' || c == '》') continue;
                n++;
            }
            return n;
        }

        /// <summary>English: words (each CJK char counts as one). Chinese: non-blank chars.</summary>
        public static int CountLength(string text)
        {
            if (!GameSettings.IsEnglish) return CountContentChars(text);
            if (string.IsNullOrEmpty(text)) return 0;
            int n = 0;
            bool inWord = false;
            foreach (var c in text)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    n++;
                    inWord = false;
                }
                else if (char.IsLetterOrDigit(c))
                {
                    if (!inWord) n++;
                    inWord = true;
                }
                else if (c != '\'' && c != '’' && c != '-')
                    inWord = false;
            }
            return n;
        }

        /// <summary>
        /// Overwrite <see cref="ArticleAssembler.Body"/> with a longer draft when possible.
        /// Does not invent intel beyond selected material bodies / direction templates.
        /// </summary>
        public static IEnumerator ExpandCoroutine(
            ArticleAssembler assembler,
            WritingDirection dir,
            List<string> selected,
            Action onDone)
        {
            if (assembler == null)
            {
                onDone?.Invoke();
                yield break;
            }

            // Prefer polishing the player's current body; only fall back to the offline draft when thin.
            if (CountLength(assembler.Body) < TargetLength * 0.5f)
            {
                string offline = BuildOfflineFeature(dir, selected);
                if (CountLength(offline) > CountLength(assembler.Body))
                    assembler.ReplaceBody(offline);
            }

            var llm = LlmClient.Instance;
            if (llm == null || !llm.IsConfigured)
            {
                assembler.ReplaceBody(StripRelatedVerificationBlocks(assembler.Body));
                onDone?.Invoke();
                yield break;
            }

            bool en = GameSettings.IsEnglish;
            string style = en ? ExpandStyleEn() : ExpandStyleZh();
            string facts = BuildExpandFacts(en, dir, selected, assembler.Body);

            string expanded = null;
            yield return llm.RephraseCoroutine(style, facts, "", text => expanded = text);

            if (!string.IsNullOrWhiteSpace(expanded))
            {
                var cleaned = StripRelatedVerificationBlocks(expanded.Trim());
                if (CountLength(cleaned) >= CountLength(assembler.Body) * 0.8f)
                    assembler.ReplaceBody(cleaned);
            }
            else
            {
                assembler.ReplaceBody(StripRelatedVerificationBlocks(assembler.Body));
            }

            onDone?.Invoke();
        }

        static string ExpandStyleZh() =>
            "你是《街角专访》的特稿写手。在保留记者当前草稿意图与可用事实的前提下，润色并扩写成一篇社区观察特稿全文。"
            + "要求：1）总字数（不计空白）必须达到 " + TargetMinChars + " 字以上，内容充实、有场景感与过渡；"
            + "2）只能使用给定素材与立意中的事实，禁止编造新的人名、数字、医院名、费用、因果；"
            + "3）保留清晰小标题结构（用【】包裹小标题）；文风冷静克制，适合杂志；"
            + "4）不确定处写「无法确认/尚不清楚」，不要写成铁板事实；"
            + "5）优先润色/扩展【当前草稿】，不要无故推翻已有表述；"
            + "6）不要输出「相关核实」小节、资料核对清单或素材原文罗列；把事实写进叙述里即可；"
            + "7）只输出成稿全文，不要前言后语，不要 JSON。";

        static string ExpandStyleEn() =>
            "You are a feature writer for the magazine Here & Now. Keeping the reporter's intent and the available facts, "
            + "polish and expand the current draft into a complete community feature story. Rules: "
            + "1) At least " + TargetMinWordsEn + " words, with scene-setting and smooth transitions; "
            + "2) Use only facts from the given materials and angle. Never invent names, numbers, hospitals, costs, or causes; "
            + "3) Keep short section headings, each on its own line; calm, restrained magazine prose; "
            + "4) Where something is uncertain, say it cannot be confirmed instead of stating it as fact; "
            + "5) Build on the current draft rather than discarding it; "
            + "6) No fact-check sections, source lists, or pasted card text—weave facts into the narrative; "
            + "7) Write in natural English only. Output the article text only: no preface, no notes, no JSON.";

        static string BuildExpandFacts(bool en, WritingDirection dir, List<string> selected, string body)
        {
            var facts = new StringBuilder();
            facts.AppendLine(en ? "[Authoritative facts]" : "【权威台词/事实】");
            facts.AppendLine((en ? "Headline: " : "立意标题：") + ArticleAssembler.TitleFor(dir));
            facts.AppendLine(en ? "Selected materials (expand only from these):" : "已选素材（只能据此扩写）：");
            foreach (var m in Cards(selected))
            {
                facts.AppendLine("- " + m.id + " " + m.LocalizedTitle);
                facts.AppendLine((en ? "  Fact: " : "  事实要点：") + m.LocalizedBody);
                facts.AppendLine((en ? "  Usable sentence: " : "  可写句子：") + m.LocalizedLine(dir));
            }
            facts.AppendLine();
            facts.AppendLine(en
                ? "[Current draft — polish and expand to at least " + TargetMinWordsEn + " words]"
                : "【当前草稿（请在此基础上润色并扩写至 " + TargetMinChars + " 字以上）】");
            facts.AppendLine(body?.Trim() ?? "");
            facts.AppendLine();
            facts.AppendLine(en
                ? "Output the complete feature, at least " + TargetMinWordsEn + " words, in English."
                : "请输出不少于 " + TargetMinChars + " 字的完整特稿正文。");
            return facts.ToString();
        }

        // ---------- Offline feature ----------

        static readonly string[] GuardOrder =
        {
            MaterialIds.M01, MaterialIds.M14,
            MaterialIds.M02, MaterialIds.M03, MaterialIds.M05, MaterialIds.M16,
            MaterialIds.M06, MaterialIds.M07, MaterialIds.M04, MaterialIds.M08, MaterialIds.M09, MaterialIds.M10,
            MaterialIds.M11, MaterialIds.M12, MaterialIds.M13,
            MaterialIds.M15
        };

        static readonly string[] RescueOrder =
        {
            MaterialIds.M03, MaterialIds.M05, MaterialIds.M16, MaterialIds.M02,
            MaterialIds.M06, MaterialIds.M07, MaterialIds.M04, MaterialIds.M08, MaterialIds.M09, MaterialIds.M10,
            MaterialIds.M11, MaterialIds.M12, MaterialIds.M13,
            MaterialIds.M01, MaterialIds.M14, MaterialIds.M15
        };

        /// <summary>
        /// Offline feature: title, lede, four headed sections, closing. Each selected card is used once,
        /// in narrative order; connective sentences only appear when the cards they rely on are selected.
        /// </summary>
        public static string BuildOfflineFeature(WritingDirection dir, List<string> selected)
        {
            var picked = new HashSet<string>(StringComparer.Ordinal);
            if (selected != null)
                foreach (var id in selected)
                    if (!string.IsNullOrEmpty(id) && MaterialCatalog.Get(id) != null)
                        picked.Add(id);

            var sb = new StringBuilder();
            sb.AppendLine(ArticleAssembler.TitleFor(dir));
            sb.AppendLine();

            bool has(string id) => picked.Contains(id);
            bool guard = dir == WritingDirection.GuardCatToday;
            var order = guard ? GuardOrder : RescueOrder;

            if (guard)
            {
                bool anyPresent = AnyStage(picked, ArticleStage.A_PresentLife);
                AppendParagraph(sb, has(MaterialIds.M01)
                    ? "槐安社区的居民说起大福，常会用到一个词：上班。它没有工牌，也没有排班表，却总能让人在差不多的时间、差不多的地方找到它。"
                    : "在槐安社区，大福是不少居民都认得的一只猫。");

                AppendSection(sb, "今天的大福", null, dir, order, picked,
                    anyPresent
                        ? new[] { ArticleStage.A_PresentLife }
                        : new[] { ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn });
                AppendSection(sb, "从前", "如今的从容，并不是它一直就有的。", dir, order, picked,
                    ArticleStage.B_PastInjury);
                AppendSection(sb, "救助",
                    has(MaterialIds.M06)
                        ? "转机来自一个愿意一次次出现、又一次次退开的人。"
                        : "后来，它被送进了医院。",
                    dir, order, picked, ArticleStage.C_RescueTreatment);
                AppendSection(sb, "回到社区",
                    has(MaterialIds.M13)
                        ? "伤好之后，大福没有被带进谁的家门。"
                        : "伤好了，接下来的问题是：它该去哪儿。",
                    dir, order, picked,
                    anyPresent
                        ? new[] { ArticleStage.D_Release, ArticleStage.E_AfterReturn }
                        : new[] { ArticleStage.D_Release });

                AppendParagraph(sb, has(MaterialIds.M14)
                    ? "所以，「大福今天也在上班」并不只是一句玩笑。它的「工位」是很多人一起守着的：有人添粮，有人换水，有人修补猫窝。"
                    : "下次傍晚路过槐安社区，不妨留意一下，它今天是不是也在上班。");
            }
            else
            {
                AppendParagraph(sb,
                    "救一只猫，常常从看见一道伤口开始。可真正难回答的问题，往往出现在它被救下之后。");

                AppendSection(sb, "一道伤口", null, dir, order, picked, ArticleStage.B_PastInjury);
                AppendSection(sb, "接近与治疗",
                    has(MaterialIds.M06)
                        ? "要救一只怕人的猫，第一步不是抓，而是等。"
                        : "接下来，是送医与治疗。",
                    dir, order, picked, ArticleStage.C_RescueTreatment);
                AppendSection(sb, "为什么没有收养",
                    "很多人会问：既然已经救了，为什么不干脆把它带回家？",
                    dir, order, picked, ArticleStage.D_Release);
                AppendSection(sb, "回到社区",
                    has(MaterialIds.M13)
                        ? "如今，大福又回到了熟悉的地方。"
                        : "如今的大福，过着另一种日子。",
                    dir, order, picked, ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn);

                AppendParagraph(sb, has(MaterialIds.M12)
                    ? "救治与收养被分成了两件事。这不是推卸，而是一个普通人在自己的能力边界里，所能给出的最诚实的答案。"
                    : "一只猫被救下之后的日子，并不只属于救它的那一个人。");
            }

            return sb.ToString().TrimEnd() + "\n";
        }

        static IEnumerable<MaterialCard> Cards(List<string> selected)
        {
            if (selected == null) yield break;
            foreach (var id in selected)
            {
                var m = MaterialCatalog.Get(id);
                if (m != null) yield return m;
            }
        }

        static bool AnyStage(HashSet<string> picked, ArticleStage stage)
        {
            foreach (var id in picked)
            {
                var m = MaterialCatalog.Get(id);
                if (m != null && m.stage == stage) return true;
            }
            return false;
        }

        static string Heading(string zh) =>
            GameSettings.IsEnglish ? HardTextLoc.T(zh) : "【" + zh + "】";

        static void AppendParagraph(StringBuilder sb, string zh)
        {
            sb.AppendLine(HardTextLoc.T(zh));
            sb.AppendLine();
        }

        /// <summary>Heading + lead + card sentences, at most three sentences per paragraph. Skipped when no card matches.</summary>
        static void AppendSection(
            StringBuilder sb, string headingZh, string leadZh, WritingDirection dir,
            string[] order, HashSet<string> picked, params ArticleStage[] stages)
        {
            var sentences = new List<string>();
            foreach (var id in order)
            {
                if (!picked.Contains(id)) continue;
                var m = MaterialCatalog.Get(id);
                if (m == null || Array.IndexOf(stages, m.stage) < 0) continue;
                var line = m.LocalizedLine(dir);
                if (!string.IsNullOrWhiteSpace(line)) sentences.Add(line.Trim());
            }
            if (sentences.Count == 0) return;
            if (!string.IsNullOrEmpty(leadZh))
                sentences.Insert(0, HardTextLoc.T(leadZh));

            string sep = GameSettings.IsEnglish ? " " : "";
            sb.AppendLine(Heading(headingZh));
            for (int i = 0; i < sentences.Count; i += 3)
            {
                int n = Math.Min(3, sentences.Count - i);
                sb.AppendLine(string.Join(sep, sentences.GetRange(i, n)));
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Remove 「相关核实」lines / sections from assembled feature body (offline templates or LLM).
        /// </summary>
        public static string StripRelatedVerificationBlocks(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            var src = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = src.Split('\n');
            var outSb = new StringBuilder(src.Length);
            bool skippingSection = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("【相关核实】", StringComparison.Ordinal)
                    || trimmed.StartsWith("相关核实", StringComparison.Ordinal))
                {
                    // Drop a dedicated heading section until the next 【…】 heading or blank+heading.
                    if (trimmed.StartsWith("【相关核实】", StringComparison.Ordinal))
                    {
                        skippingSection = true;
                        continue;
                    }
                    // Inline "相关核实：…" fact dumps — drop the whole line.
                    continue;
                }
                if (skippingSection)
                {
                    if (trimmed.StartsWith("【", StringComparison.Ordinal) && trimmed.Contains("】"))
                        skippingSection = false;
                    else
                        continue;
                }
                if (outSb.Length > 0) outSb.Append('\n');
                outSb.Append(line);
            }
            return outSb.ToString().TrimEnd() + (text.EndsWith("\n") || text.EndsWith("\r\n") ? "\n" : "");
        }
    }
}
