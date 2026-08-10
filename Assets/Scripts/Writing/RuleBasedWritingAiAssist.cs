using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Data;
using StreetCat.Loc;

namespace StreetCat.Writing
{
    /// <summary>
    /// Offline writing assist from unlocked materials + article assemble rules.
    /// Does not invent facts or unlock cards — only ranks / templates what the player already has.
    /// Gate: paragraphs 01–04 each have ≥1 card (no forced M13/M07/…).
    /// </summary>
    public sealed class RuleBasedWritingAiAssist : IWritingAiAssist
    {
        const int TargetSelect = 8;
        const int MaxSelect = 10;

        static readonly string[] GuardPriority =
        {
            MaterialIds.M01, MaterialIds.M14, MaterialIds.M15,
            MaterialIds.M03, MaterialIds.M05, MaterialIds.M16, MaterialIds.M02,
            MaterialIds.M06, MaterialIds.M07, MaterialIds.M08, MaterialIds.M04,
            MaterialIds.M09, MaterialIds.M10,
            MaterialIds.M13, MaterialIds.M12, MaterialIds.M11
        };

        static readonly string[] RescuePriority =
        {
            MaterialIds.M05, MaterialIds.M03, MaterialIds.M02, MaterialIds.M16,
            MaterialIds.M06, MaterialIds.M07, MaterialIds.M08, MaterialIds.M09, MaterialIds.M10, MaterialIds.M04,
            MaterialIds.M12, MaterialIds.M11, MaterialIds.M13,
            MaterialIds.M14, MaterialIds.M01, MaterialIds.M15
        };

        public WritingAssistBundle Suggest(WritingAssistContext ctx)
        {
            var bundle = new WritingAssistBundle { ProviderNote = "rule" };
            if (ctx == null) return bundle;

            var unlocked = ToSet(ctx.UnlockedMaterialIds);
            BuildSuggestedSelection(ctx.Direction, unlocked, bundle.SuggestedMaterialIds);

            var assembler = new ArticleAssembler();
            var playerIds = FilterUnlocked(ctx.SelectedMaterialIds, unlocked);
            if (playerIds.Count > MaxSelect)
                playerIds = playerIds.GetRange(0, MaxSelect);

            List<string> useIds;
            if (assembler.CanAssemble(ctx.Direction, playerIds, out _))
            {
                useIds = playerIds;
                bundle.DraftFromPlayerSelection = true;
            }
            else
            {
                useIds = PreferFilledSelection(playerIds, bundle.SuggestedMaterialIds, unlocked);
                bundle.DraftFromPlayerSelection = false;
            }

            bundle.CanAssembleWithSuggestion = assembler.CanAssemble(ctx.Direction, useIds, out var err);
            bundle.AssembleError = err;
            if (bundle.CanAssembleWithSuggestion)
            {
                assembler.Assemble(ctx.Direction, useIds);
                bundle.DraftArticle = assembler.Body;
            }
            else
            {
                bundle.DraftArticle = BuildPartialDraft(ctx.Direction, useIds, unlocked);
            }

            bundle.FocusedCardWording = BuildFocusedWording(ctx.FocusMaterialId, ctx.Direction, unlocked);
            bundle.CoachTip = BuildCoachTip(ctx, bundle, playerIds);
            return bundle;
        }

        static List<string> FilterUnlocked(IReadOnlyList<string> ids, HashSet<string> unlocked)
        {
            var list = new List<string>();
            if (ids == null) return list;
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id) || !unlocked.Contains(id) || list.Contains(id)) continue;
                list.Add(id);
            }
            return list;
        }

        /// <summary>Cover paragraphs 01–04 first, then fill toward TargetSelect.</summary>
        static void BuildSuggestedSelection(WritingDirection dir, HashSet<string> unlocked, List<string> outIds)
        {
            outIds.Clear();
            var priority = dir == WritingDirection.RescueWithoutAdoption ? RescuePriority : GuardPriority;
            var assembler = new ArticleAssembler();

            foreach (var id in priority)
            {
                if (outIds.Count >= MaxSelect) break;
                if (!unlocked.Contains(id) || outIds.Contains(id)) continue;
                outIds.Add(id);
                if (assembler.CanAssemble(dir, outIds, out _) && outIds.Count >= 4)
                    break;
            }

            if (outIds.Count < TargetSelect)
            {
                foreach (var id in priority)
                {
                    if (outIds.Count >= TargetSelect) break;
                    if (!unlocked.Contains(id) || outIds.Contains(id)) continue;
                    outIds.Add(id);
                }
            }

            foreach (var m in MaterialCatalog.All)
            {
                if (outIds.Count >= TargetSelect) break;
                if (m == null || !unlocked.Contains(m.id) || outIds.Contains(m.id)) continue;
                outIds.Add(m.id);
            }

            while (outIds.Count > MaxSelect)
                outIds.RemoveAt(outIds.Count - 1);
        }

        static List<string> PreferFilledSelection(
            IReadOnlyList<string> selected,
            List<string> suggested,
            HashSet<string> unlocked)
        {
            var list = new List<string>();
            if (selected != null)
            {
                foreach (var id in selected)
                {
                    if (list.Count >= MaxSelect) break;
                    if (string.IsNullOrEmpty(id) || !unlocked.Contains(id) || list.Contains(id)) continue;
                    list.Add(id);
                }
            }
            if (suggested != null)
            {
                foreach (var id in suggested)
                {
                    if (list.Count >= MaxSelect) break;
                    if (string.IsNullOrEmpty(id) || !unlocked.Contains(id) || list.Contains(id)) continue;
                    list.Add(id);
                }
            }
            return list;
        }

        static string BuildCoachTip(WritingAssistContext ctx, WritingAssistBundle bundle, List<string> playerIds)
        {
            var assembler = new ArticleAssembler();
            if (assembler.CanAssemble(ctx.Direction, playerIds, out _))
            {
                return string.Format(
                    T("ui.writing.ai.tip_from_selection",
                        "已按你当前选中的 {0} 张素材生成草稿（四段均有覆盖）。成稿不强制指定某张卡。"),
                    playerIds?.Count ?? 0);
            }

            ArticleAssembler.CountParagraphCoverage(playerIds, out int p1, out int p2, out int p3, out int p4);
            int covered = (p1 > 0 ? 1 : 0) + (p2 > 0 ? 1 : 0) + (p3 > 0 ? 1 : 0) + (p4 > 0 ? 1 : 0);
            if (covered < 4)
            {
                return string.Format(
                    T("ui.writing.ai.tip_need_paras",
                        "成稿只需段落 01～04 各有一张素材即可（已覆盖 {0}/4）。下方建议选材可帮你补齐缺口。"),
                    covered);
            }

            if (!bundle.CanAssembleWithSuggestion)
            {
                return string.Format(
                    T("ui.writing.ai.tip_assemble_fail", "按现有素材还不能成稿：{0}"),
                    bundle.AssembleError ?? "");
            }

            if (ctx.Direction == WritingDirection.GuardCatToday)
                return T("ui.writing.ai.tip_guard_ok",
                    "立意偏日常：建议保留「今日在岗 / 社区照料」，治疗线交代清楚即可，别写成纯救助特写。");

            return T("ui.writing.ai.tip_rescue_ok",
                "立意偏救助：建议压住晒太阳篇幅，把投喂→送医→费用→未收养→放归串起来。");
        }

        static string BuildFocusedWording(string matId, WritingDirection dir, HashSet<string> unlocked)
        {
            if (string.IsNullOrEmpty(matId) || !unlocked.Contains(matId))
                return T("ui.writing.ai.focus_none", "点选一张已解锁素材卡，可查看成稿时的可能写法。");

            var m = MaterialCatalog.Get(matId);
            if (m == null) return "";

            var line = dir == WritingDirection.GuardCatToday ? m.textGuardCat : m.textRescue;
            return string.Format(
                T("ui.writing.ai.focus_fmt", "【{0} {1}】成稿可能写成：\n{2}"),
                m.id, m.title, line);
        }

        static string BuildPartialDraft(WritingDirection dir, List<string> ids, HashSet<string> unlocked)
        {
            var sb = new StringBuilder();
            sb.AppendLine(T("ui.writing.ai.partial_header", "（素材不足，以下为已选/建议素材的片段拼接，非正式成稿）"));
            sb.AppendLine();
            if (ids == null || ids.Count == 0)
            {
                sb.AppendLine(T("ui.writing.ai.partial_empty", "暂无可拼接的已解锁素材。"));
                return sb.ToString();
            }

            foreach (var id in ids)
            {
                if (!unlocked.Contains(id)) continue;
                var m = MaterialCatalog.Get(id);
                if (m == null) continue;
                var line = dir == WritingDirection.GuardCatToday ? m.textGuardCat : m.textRescue;
                sb.AppendLine("· " + line);
            }
            return sb.ToString();
        }

        static HashSet<string> ToSet(IReadOnlyList<string> ids)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (ids == null) return set;
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id)) set.Add(id);
            return set;
        }

        static string T(string key, string fallback) => UiLoc.T(key, fallback);
    }
}
