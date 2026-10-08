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
            window.minSize = new Vector2(520f, 520f);
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
            EditorGUILayout.HelpBox(
                "右侧色板是羊皮纸上比较耐看的墨色。点一下填入这一行，再自己微调。透明度会保留。\n" +
                "当前用羊皮纸对话框时，改单段文字请用「文本样式编辑器」的自定义颜色。",
                MessageType.None);
            EditorGUI.BeginChangeCheck();
            var speakerName = ColorWithSwatches("角色姓名", data.speakerName);
            var dialogue = ColorWithSwatches("普通对话正文", data.dialogue);
            var narration = ColorWithSwatches("旁白正文", data.narration);
            var inner = ColorWithSwatches("内心独白", data.inner);
            var system = ColorWithSwatches("系统文字", data.system);
            var status = ColorWithSwatches("状态提示", data.status);
            var clickHint = ColorWithSwatches("继续提示", data.clickHint);
            var choice = ColorWithSwatches("选项文字", data.choice);
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

        static Color ColorWithSwatches(string label, Color value)
        {
            EditorGUILayout.BeginHorizontal();
            var next = EditorGUILayout.ColorField(new GUIContent(label), value, true, true, true,
                GUILayout.MinWidth(180f));
            InkSwatches.DrawMini(ref next);
            EditorGUILayout.EndHorizontal();
            return next;
        }

        static void DrawPreview(DialogueFontColorData data)
        {
            EditorGUILayout.LabelField("颜色预览", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(100f, 112f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.93f, 0.88f, 0.78f, 1f));
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
