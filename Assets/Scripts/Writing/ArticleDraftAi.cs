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
    /// Expands a stub article into a fuller feature (≥1000 字 when possible).
    /// LLM when keyed; otherwise offline narrative expansion from selected cards only.
    /// </summary>
    public static class ArticleDraftAi
    {
        public const int TargetMinChars = 1000;

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

            // Prefer polishing the player's current body; only fall back to offline skeleton when thin.
            int currentChars = CountContentChars(assembler.Body);
            if (currentChars < TargetMinChars * 0.5f)
            {
                string skeleton = BuildOfflineFeature(dir, selected, assembler.Title, assembler.Body);
                if (CountContentChars(skeleton) > currentChars)
                    assembler.ReplaceBody(skeleton);
            }

            var llm = LlmClient.Instance;
            if (llm == null || !llm.IsConfigured)
            {
                // Offline path: ensure ≥ target when possible from materials.
                if (CountContentChars(assembler.Body) < TargetMinChars)
                {
                    string skeleton = BuildOfflineFeature(dir, selected, assembler.Title, assembler.Body);
                    if (CountContentChars(skeleton) > CountContentChars(assembler.Body))
                        assembler.ReplaceBody(skeleton);
                }
                else
                    assembler.ReplaceBody(StripRelatedVerificationBlocks(assembler.Body));
                onDone?.Invoke();
                yield break;
            }

            var title = ArticleAssembler.TitleFor(dir);

            var style =
                "你是《街角专访》的特稿写手。在保留记者当前草稿意图与可用事实的前提下，润色并扩写成一篇社区观察特稿全文。"
                + "要求：1）总字数（不计空白）必须达到 " + TargetMinChars + " 字以上，内容充实、有场景感与过渡；"
                + "2）只能使用给定素材与立意中的事实，禁止编造新的人名、数字、医院名、费用、因果；"
                + "3）保留清晰小标题结构；文风冷静克制，适合杂志；"
                + "4）不确定处写「无法确认/尚不清楚」，不要写成铁板事实；"
                + "5）优先润色/扩展【当前草稿】，不要无故推翻已有表述；"
                + "6）不要输出「相关核实」小节、资料核对清单或素材原文罗列；把事实写进叙述里即可；"
                + "7）只输出成稿全文，不要前言后语，不要 JSON。";

            var facts = new StringBuilder();
            facts.AppendLine("【权威台词/事实】");
            facts.AppendLine("立意标题：" + title);
            facts.AppendLine("已选素材（只能据此扩写）：");
            if (selected != null)
            {
                foreach (var id in selected)
                {
                    var m = MaterialCatalog.Get(id);
                    if (m == null) continue;
                    var line = dir == WritingDirection.GuardCatToday ? m.textGuardCat : m.textRescue;
                    facts.AppendLine("- " + m.id + " " + m.title);
                    facts.AppendLine("  事实要点：" + m.body);
                    facts.AppendLine("  可写句子：" + line);
                }
            }
            facts.AppendLine();
            facts.AppendLine("【当前草稿（请在此基础上润色并扩写至 " + TargetMinChars + " 字以上）】");
            facts.AppendLine(assembler.Body?.Trim() ?? "");
            facts.AppendLine();
            facts.AppendLine("请输出不少于 " + TargetMinChars + " 字的完整特稿正文。");

            string expanded = null;
            yield return llm.RephraseCoroutine(style, facts.ToString(), "", text => expanded = text);

            if (!string.IsNullOrWhiteSpace(expanded))
            {
                var cleaned = StripRelatedVerificationBlocks(expanded.Trim());
                if (CountContentChars(cleaned) >= CountContentChars(assembler.Body) * 0.8f)
                    assembler.ReplaceBody(cleaned);
            }
            else
            {
                assembler.ReplaceBody(StripRelatedVerificationBlocks(assembler.Body));
            }

            onDone?.Invoke();
        }

        /// <summary>Offline expansion: weave material bodies into fuller section prose.</summary>
        public static string BuildOfflineFeature(
            WritingDirection dir, List<string> selected, string title, string stubBody)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.IsNullOrEmpty(title)
                ? ArticleAssembler.TitleFor(dir)
                : title);
            sb.AppendLine();

            if (dir == WritingDirection.GuardCatToday)
            {
                AppendOfflineSection(sb, "【现在的大福】",
                    "若你傍晚路过槐安社区门口，很容易注意到一只橘猫。它并不急着讨好谁，只是按自己的节奏出现，把一块小小的公共空间当成固定工位。",
                    selected, dir, ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn);
                AppendOfflineSection(sb, "【过去】",
                    "要理解它今天的稳，就得回到它并不愿意细说、却又无法完全忘掉的那段日子。伤痛往往不是一句「它受伤了」能说完的。",
                    selected, dir, ArticleStage.B_PastInjury);
                AppendOfflineSection(sb, "【救助】",
                    "真正把故事推到医院门口的，不是戏剧冲突，而是有人连续出现、退开、再出现，直到抓捕与送医成为不得不做的一步。",
                    selected, dir, ArticleStage.C_RescueTreatment);
                AppendOfflineSection(sb, "【放归之后】",
                    "治好并不自动等于拥有。放归社区，意味着承认照料是一群人的接力，而不是一个人的承诺书。",
                    selected, dir, ArticleStage.D_Release, ArticleStage.E_AfterReturn);
            }
            else
            {
                AppendOfflineSection(sb, "【发现】",
                    "故事往往从一道伤口开始。谁先看见、看见时它有多怕人，决定了后面每一步能走多远。",
                    selected, dir, ArticleStage.B_PastInjury);
                AppendOfflineSection(sb, "【接近与治疗】",
                    "接近一只怕人的猫，靠的不是勇气口号，而是重复的退让与等待；进入医院之后，账本与结果同样残酷。",
                    selected, dir, ArticleStage.C_RescueTreatment);
                AppendOfflineSection(sb, "【为什么没有收养】",
                    "救治与收养不是同一张支票。空间、精力、家里已有的生命，都会把「我想留下它」压成更具体的限制。",
                    selected, dir, ArticleStage.D_Release);
                AppendOfflineSection(sb, "【回到社区】",
                    "当它重新出现在熟悉的门口，值班、晒太阳、结伴离开，读者看到的不只是可爱，还有一场救助留下的痕迹。",
                    selected, dir, ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn);
            }

            sb.AppendLine("【编者按】");
            sb.AppendLine("本稿只写已核实的观察与当事人陈述。绳子如何套上、是否存在故意伤害等无法确认之处，不作推断定罪。");
            sb.AppendLine();

            var result = sb.ToString();
            // If still thin, append stub lines as backup (dedupe-ish by length).
            if (CountContentChars(result) < 700 && !string.IsNullOrWhiteSpace(stubBody))
            {
                sb.AppendLine("【素材摘录】");
                sb.AppendLine(StripRelatedVerificationBlocks(stubBody.Trim()));
                result = sb.ToString();
            }
            return StripRelatedVerificationBlocks(result);
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

        static void AppendOfflineSection(
            StringBuilder sb, string heading, string lead,
            List<string> selected, WritingDirection dir, params ArticleStage[] stages)
        {
            var lines = new List<string>();
            if (selected != null)
            {
                foreach (var id in selected)
                {
                    var m = MaterialCatalog.Get(id);
                    if (m == null) continue;
                    bool match = false;
                    foreach (var st in stages)
                        if (m.stage == st) match = true;
                    if (!match && m.id == MaterialIds.M01)
                        foreach (var st in stages)
                            if (st == ArticleStage.A_PresentLife || st == ArticleStage.E_AfterReturn)
                                match = true;
                    if (!match) continue;
                    var written = dir == WritingDirection.GuardCatToday ? m.textGuardCat : m.textRescue;
                    lines.Add(written);
                    // Weave body detail into prose when it adds facts the direction line lacks —
                    // but never as a labeled 「相关核实」block in the成稿.
                    if (!string.IsNullOrWhiteSpace(m.body) && m.body != written
                        && written != null && !written.Contains(m.body))
                        lines.Add(m.body);
                }
            }
            if (lines.Count == 0) return;

            sb.AppendLine(heading);
            sb.AppendLine(lead);
            sb.AppendLine();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
                sb.AppendLine();
            }
        }
    }
}
