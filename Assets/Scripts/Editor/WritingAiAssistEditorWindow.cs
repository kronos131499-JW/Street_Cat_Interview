using StreetCat.Data;
using StreetCat.UI;
using StreetCat.Writing;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// Dev assist: dump rule-based writing suggestions while Play Mode is on the corkboard.
    /// Offline by default. Optional LLM polish uses the same PlayerPrefs key as interview.
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
            EditorGUILayout.LabelField("写稿 / 素材卡 AI 辅助", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "运行时默认用规则模板（无 API）。\n"
                + "可选润色：StreetCat/LLM 粘贴 Key 后，Play 里点「AI 建议」可用润色。\n"
                + "接口与采访提示同模式：IWritingAiAssist ↔ IInterviewHintProvider。",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("刷新建议（需 Play + 已进写稿）", GUILayout.Height(28)))
                    DumpSuggestion();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("进入 Play，并用「街角专访/测试跳转/写稿桌 / 素材板」打开素材板后再刷新。", MessageType.Warning);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastDump, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void DumpSuggestion()
        {
            var ui = Object.FindObjectOfType<GameUI>();
            if (ui == null)
            {
                lastDump = "未找到 GameUI。";
                return;
            }

            var bundle = ui.DebugPeekWritingAssist();
            if (bundle == null)
            {
                lastDump = "Suggest 返回空。";
                return;
            }

            var dir = WritingDirection.GuardCatToday;
            if (StreetCat.Core.GameState.Instance != null)
                dir = (WritingDirection)Mathf.Max(0, StreetCat.Core.GameState.Instance.Data.writingDirection);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Provider: " + (bundle.ProviderNote ?? "rule"));
            sb.AppendLine("Direction: " + dir);
            sb.AppendLine("Coach: " + (bundle.CoachTip ?? ""));
            sb.AppendLine("Phrasing: " + (bundle.PhrasingTip ?? ""));
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
