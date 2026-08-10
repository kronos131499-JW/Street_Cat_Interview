using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Writing;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// Dev-only dump of rule-based writing suggestions (no in-game corkboard button).
    /// </summary>
    public sealed class WritingAiAssistEditorWindow : EditorWindow
    {
        Vector2 scroll;
        string lastDump = "";

        [MenuItem("街角专访/写稿 AI 辅助")]
        public static void Open()
        {
            var w = GetWindow<WritingAiAssistEditorWindow>("写稿 AI 辅助");
            w.minSize = new Vector2(420, 360);
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("写稿 / 素材卡 AI 辅助（仅编辑器）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "素材卡界面已不显示「AI 建议」。此窗口仅供开发调试规则模板。\n"
                + "接口：IWritingAiAssist（默认 RuleBased / Llm stub）。",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("刷新建议（需 Play）", GUILayout.Height(28)))
                    DumpSuggestion();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("进入 Play 后再刷新。", MessageType.Warning);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastDump, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void DumpSuggestion()
        {
            var gs = GameState.Instance;
            var dir = WritingDirection.GuardCatToday;
            IReadOnlyList<string> selected = System.Array.Empty<string>();
            if (gs != null)
            {
                dir = (WritingDirection)Mathf.Max(0, gs.Data.writingDirection);
                if (gs.Data.selectedMaterials != null && gs.Data.selectedMaterials.Count > 0)
                    selected = gs.Data.selectedMaterials;
            }

            var bundle = WritingAiAssistService.Suggest(
                WritingAiAssistService.BuildContext(dir, selected, null, 0));
            if (bundle == null)
            {
                lastDump = "Suggest 返回空。";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Provider: " + (bundle.ProviderNote ?? "rule"));
            sb.AppendLine("Direction: " + dir);
            sb.AppendLine("Coach: " + (bundle.CoachTip ?? ""));
            sb.AppendLine("Suggested: " + string.Join(", ", bundle.SuggestedMaterialIds));
            sb.AppendLine("CanAssemble: " + bundle.CanAssembleWithSuggestion
                          + (string.IsNullOrEmpty(bundle.AssembleError) ? "" : " (" + bundle.AssembleError + ")"));
            sb.AppendLine();
            sb.AppendLine("--- Draft ---");
            sb.AppendLine(bundle.DraftArticle ?? "");
            lastDump = sb.ToString();
            Repaint();
        }
    }
}
