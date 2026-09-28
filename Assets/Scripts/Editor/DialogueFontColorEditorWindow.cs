using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public sealed class DialogueFontColorEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/对话字体颜色编辑器", priority = 7)]
        [MenuItem("StreetCat/Dialogue Font Color Editor", priority = 7)]
        public static void Open()
        {
            var window = GetWindow<DialogueFontColorEditorWindow>("对话字体颜色");
            window.minSize = new Vector2(380f, 430f);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("对话字体颜色", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "点击颜色块打开颜色选择器。Play Mode 中会即时预览，修改自动保存。透明度也会保留。",
                MessageType.Info);

            var data = DialogueFontColors.EnsureAsset();
            if (data == null)
            {
                EditorGUILayout.HelpBox("无法创建 DialogueFontColors.asset。", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            var speakerName = EditorGUILayout.ColorField(new GUIContent("角色姓名"), data.speakerName, true, true, true);
            var dialogue = EditorGUILayout.ColorField(new GUIContent("普通对话正文"), data.dialogue, true, true, true);
            var narration = EditorGUILayout.ColorField(new GUIContent("旁白正文"), data.narration, true, true, true);
            var inner = EditorGUILayout.ColorField(new GUIContent("内心独白"), data.inner, true, true, true);
            var system = EditorGUILayout.ColorField(new GUIContent("系统文字"), data.system, true, true, true);
            var status = EditorGUILayout.ColorField(new GUIContent("状态提示"), data.status, true, true, true);
            var clickHint = EditorGUILayout.ColorField(new GUIContent("继续提示"), data.clickHint, true, true, true);
            var choice = EditorGUILayout.ColorField(new GUIContent("选项文字"), data.choice, true, true, true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Change Dialogue Font Colors");
                data.speakerName = speakerName;
                data.dialogue = dialogue;
                data.narration = narration;
                data.inner = inner;
                data.system = system;
                data.status = status;
                data.clickHint = clickHint;
                data.choice = choice;
                DialogueFontColors.Save(data);
            }

            EditorGUILayout.Space(10f);
            DrawPreview(data);
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("恢复主题默认颜色"))
            {
                if (EditorUtility.DisplayDialog("恢复默认颜色", "确定恢复全部对话字体颜色？", "恢复", "取消"))
                    DialogueFontColors.ResetToDefaults();
            }
            if (GUILayout.Button("选中颜色资源"))
            {
                Selection.activeObject = data;
                EditorGUIUtility.PingObject(data);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "Play 中：颜色修改会立即应用到当前对话。"
                : "进入 Play Mode 并打开对话界面可查看即时效果。", MessageType.None);
        }

        static void DrawPreview(DialogueFontColorData data)
        {
            EditorGUILayout.LabelField("颜色预览", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(100f, 112f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.045f, 0.05f, 0.065f, 1f));
            DrawText(rect, 8f, 6f, "角色姓名", data.speakerName);
            DrawText(rect, 8f, 28f, "这是一段普通对话文字。", data.dialogue);
            DrawText(rect, 8f, 50f, "这是一段旁白文字。", data.narration);
            DrawText(rect, 8f, 72f, "（这是一段内心独白。）", data.inner);
            DrawText(rect, 8f, 94f, "请选择一个回答", data.choice);
        }

        static void DrawText(Rect parent, float x, float y, string text, Color color)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = color;
            GUI.Label(new Rect(parent.x + x, parent.y + y, parent.width - 16f, 20f), text, style);
        }
    }
}
