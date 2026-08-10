using System;
using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;

namespace StreetCat.Writing
{
    /// <summary>
    /// Snapshot for writing / material-card assist (mirrors <c>InterviewHintContext</c>).
    /// </summary>
    public sealed class WritingAssistContext
    {
        public WritingDirection Direction;
        public IReadOnlyList<string> UnlockedMaterialIds;
        public IReadOnlyList<string> SelectedMaterialIds;
        public string FocusMaterialId;
        public int FocusParagraph;
    }

    /// <summary>
    /// Player-facing assist bundle: coach tip, suggested selection, draft prose, optional card wording.
    /// </summary>
    public sealed class WritingAssistBundle
    {
        public string CoachTip;
        public readonly List<string> SuggestedMaterialIds = new List<string>();
        /// <summary>Template / assembler draft from suggested (or current) selection.</summary>
        public string DraftArticle;
        /// <summary>How the focused unlocked card may read in the article (direction-aware).</summary>
        public string FocusedCardWording;
        /// <summary>"rule" or "rule+llm-ready" — never invents intel.</summary>
        public string ProviderNote = "rule";
        public bool CanAssembleWithSuggestion;
        public string AssembleError;
        /// <summary>True when draft was built from the player's current selection (not AI re-pick).</summary>
        public bool DraftFromPlayerSelection;
    }

    /// <summary>
    /// Pluggable writing assist. Default is rule/template-based (offline).
    /// Swap <see cref="WritingAiAssistService.Provider"/> for an LLM-backed editor assist later;
    /// shipping Play Mode must not require a live cloud LLM.
    /// Same pattern as <c>IInterviewHintProvider</c>.
    /// </summary>
    public interface IWritingAiAssist
    {
        WritingAssistBundle Suggest(WritingAssistContext ctx);
    }

    /// <summary>Facade so UI resolves assist without knowing the provider type.</summary>
    public static class WritingAiAssistService
    {
        /// <summary>
        /// Default: <see cref="LlmWritingAiAssistStub"/> (rule-based sync; optional LLM polish when keyed).
        /// Assign a custom provider for editor experiments; keep a rule fallback for shipping.
        /// </summary>
        public static IWritingAiAssist Provider { get; set; } = new LlmWritingAiAssistStub();

        public static WritingAssistBundle Suggest(WritingAssistContext ctx)
        {
            var p = Provider ?? new LlmWritingAiAssistStub();
            return p.Suggest(ctx) ?? new WritingAssistBundle();
        }

        public static WritingAssistContext BuildContext(
            WritingDirection dir,
            IReadOnlyList<string> selected,
            string focusMatId,
            int focusParagraph)
        {
            var ctx = new WritingAssistContext
            {
                Direction = dir,
                SelectedMaterialIds = selected,
                FocusMaterialId = focusMatId,
                FocusParagraph = focusParagraph
            };
            var gs = GameState.Instance;
            ctx.UnlockedMaterialIds = gs != null
                ? (IReadOnlyList<string>)gs.Data.unlockedMaterials
                : Array.Empty<string>();
            return ctx;
        }
    }
}
