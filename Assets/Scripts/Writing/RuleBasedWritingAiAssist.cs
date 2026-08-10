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
    /// </summary>
    public sealed class RuleBasedWritingAiAssist : IWritingAiAssist
    {
        const int TargetSelect = 9;
        const int MinSelect = 8;
        const int MaxSelect = 10;

        static readonly string[] GuardPriority =
        {
            MaterialIds.M13, MaterialIds.M07, MaterialIds.M08,
            MaterialIds.M01, MaterialIds.M14, MaterialIds.M15,
            MaterialIds.M03, MaterialIds.M05, MaterialIds.M16, MaterialIds.M02,
            MaterialIds.M06, MaterialIds.M04, MaterialIds.M12, MaterialIds.M11,
            MaterialIds.M09, MaterialIds.M10
        };

        static readonly string[] RescuePriority =
        {
            MaterialIds.M13, MaterialIds.M07, MaterialIds.M08,
            MaterialIds.M06, MaterialIds.M05, MaterialIds.M09, MaterialIds.M10,
            MaterialIds.M12, MaterialIds.M11, MaterialIds.M04,
            MaterialIds.M03, MaterialIds.M16, MaterialIds.M02,
            MaterialIds.M14, MaterialIds.M01, MaterialIds.M15
        };

        public WritingAssistBundle Suggest(WritingAssistContext ctx)
        {
            var bundle = new WritingAssistBundle { ProviderNote = "rule" };
            if (ctx == null) return bundle;

            var unlocked = ToSet(ctx.UnlockedMaterialIds);
            BuildSuggestedSelection(ctx.Direction, unlocked, bundle.SuggestedMaterialIds);

            var assembler = new ArticleAssembler();
            var useIds = bundle.SuggestedMaterialIds.Count >= MinSelect
                ? bundle.SuggestedMaterialIds
                : PreferFilledSelection(ctx.SelectedMaterialIds, bundle.SuggestedMaterialIds, unlocked);

            bundle.CanAssembleWithSuggestion = assembler.CanAssemble(ctx.Direction, useIds, out var err);
            bundle.AssembleError = err;
            if (bundle.CanAssembleWithSuggestion)
            {
                assembler.Assemble(ctx.Direction, useIds, 1, 1);
                bundle.DraftArticle = assembler.Body;
            }
            else
            {
                bundle.DraftArticle = BuildPartialDraft(ctx.Direction, useIds, unlocked);
            }

            bundle.FocusedCardWording = BuildFocusedWording(ctx.FocusMaterialId, ctx.Direction, unlocked);
            bundle.SuggestedPhrasingA = 1;
            bundle.SuggestedPhrasingB = 1;
            bundle.PhrasingTip = T("ui.writing.ai.phrasing_tip",
                "关键表述建议选「无法确认」与「送回社区」——把推测写成事实会被退回。");
            bundle.CoachTip = BuildCoachTip(ctx, unlocked, bundle);
            return bundle;
        }

        static void BuildSuggestedSelection(WritingDirection dir, HashSet<string> unlocked, List<string> outIds)
        {
            outIds.Clear();
            var priority = dir == WritingDirection.RescueWithoutAdoption ? RescuePriority : GuardPriority;
            foreach (var id in priority)
            {
                if (outIds.Count >= TargetSelect) break;
                if (!unlocked.Contains(id)) continue;
                if (outIds.Contains(id)) continue;
                outIds.Add(id);
            }

            // If still short, append any remaining unlocked cards (catalog order).
            if (outIds.Count < MinSelect)
            {
                foreach (var m in MaterialCatalog.All)
                {
                    if (outIds.Count >= MinSelect) break;
                    if (m == null || !unlocked.Contains(m.id) || outIds.Contains(m.id)) continue;
                    outIds.Add(m.id);
                }
            }

            while (outIds.Count > MaxSelect)
                outIds.RemoveAt(outIds.Count - 1);
        }

        static List<string> PreferFilledSelection(
            IReadOnlyList<string> selected,
            List<string> suggested,
            HashSet<string> unlocked)
        {
            if (selected != null && selected.Count >= MinSelect)
                return new List<string>(selected);
            var list = new List<string>(suggested);
            if (selected == null) return list;
            foreach (var id in selected)
            {
                if (list.Count >= MaxSelect) break;
                if (string.IsNullOrEmpty(id) || !unlocked.Contains(id) || list.Contains(id)) continue;
                list.Add(id);
            }
            return list;
        }

        static string BuildCoachTip(WritingAssistContext ctx, HashSet<string> unlocked, WritingAssistBundle bundle)
        {
            if (unlocked.Count < MinSelect)
            {
                return string.Format(
                    T("ui.writing.ai.tip_need_unlock", "已解锁 {0} 张，成稿至少需要 8 张。建议返回采访补齐治疗/放归相关情报。"),
                    unlocked.Count);
            }

            if (!unlocked.Contains(MaterialIds.M13))
                return T("ui.writing.ai.tip_need_m13", "还没有「回到槐安社区」。放归事实没核实前，很难过审。");

            if (!unlocked.Contains(MaterialIds.M07) || !unlocked.Contains(MaterialIds.M08))
                return T("ui.writing.ai.tip_need_treatment",
                    "治疗过程还不完整（抓捕送医 / 术后猫瘟）。建议优先补访林女士，再按建议选材。");

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
