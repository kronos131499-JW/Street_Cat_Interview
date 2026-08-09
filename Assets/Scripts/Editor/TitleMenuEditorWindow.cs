using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class TitleMenuEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/主菜单布局编辑器")]
        public static void Open()
        {
            var win = GetWindow<TitleMenuEditorWindow>("主菜单布局");
            win.minSize = new Vector2(340, 320);
            win.Show();
        }

        [MenuItem("街角专访/切换主菜单编辑模式")]
        public static void ToggleEditMode()
        {
            TitleMenuEditMode.Enabled = !TitleMenuEditMode.Enabled;
            Debug.Log(TitleMenuEditMode.Enabled
                ? "[主菜单] 编辑模式 ON — Play 到标题后，在 Game 视图拖拽青色框；松手自动保存。"
                : "[主菜单] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("主菜单 · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "1. 勾选下方「启用编辑模式」\n" +
                "2. Play → 进入标题界面\n" +
                "3. Game 视图拖拽青色半透明框：中间移动，四角缩放\n" +
                "4. 松手写入 Resources/TitleMenuLayout.asset\n" +
                "编辑开启时菜单按钮点击会被屏蔽，避免误进游戏。",
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = TitleMenuEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式", edit);
            if (next != edit)
                TitleMenuEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("创建 / 选中 Layout 资源"))
            {
                var asset = TitleMenuLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button("从默认值重置资源"))
            {
                if (EditorUtility.DisplayDialog("重置主菜单布局",
                        "用代码默认坐标覆盖 TitleMenuLayout.asset？", "重置", "取消"))
                {
                    var asset = TitleMenuLayout.EnsureAsset();
                    asset.entries.Clear();
                    foreach (var kv in TitleMenuLayout.Defaults)
                        asset.entries.Add(new TitleRectEntry { id = kv.Key, rect = kv.Value });
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                    TitleMenuLayout.InvalidateCache();
                }
            }

            EditorGUILayout.Space(8);
            var data = TitleMenuLayout.Asset;
            if (data == null)
            {
                EditorGUILayout.HelpBox("尚未创建 Layout。点上面的按钮创建。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("当前坐标", EditorStyles.boldLabel);
            foreach (var e in data.entries)
            {
                if (e == null) continue;
                var name = TitleMenuLayout.DisplayNames.TryGetValue(e.id, out var n) ? n : e.id;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, GUILayout.Width(90));
                EditorGUILayout.LabelField($"({e.rect.x:F2}, {e.rect.y:F2}, {e.rect.z:F2}, {e.rect.w:F2})");
                EditorGUILayout.EndHorizontal();
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    TitleMenuEditMode.Enabled
                        ? "Play 中 · 编辑已开。在标题界面 Game 视图拖拽。"
                        : "Play 中 · 请勾选上方「启用编辑模式」。",
                    MessageType.None);
            }
        }

        void OnInspectorUpdate() => Repaint();
    }
}
