using StreetCat.Investigation;
using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>Controller window for the Play Mode, Game-view UI layout editor.</summary>
    public sealed class UILayoutEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/通用 UI 布局编辑器", priority = 5)]
        [MenuItem("StreetCat/UI Layout Editor", priority = 5)]
        public static void Open()
        {
            var window = GetWindow<UILayoutEditorWindow>("通用 UI 布局");
            window.minSize = new Vector2(390f, 390f);
            window.Show();
        }

        [MenuItem("街角专访/切换通用 UI 编辑模式", priority = 6)]
        public static void ToggleEditMode()
        {
            SetEditMode(!UILayoutEditMode.Enabled);
        }

        static void SetEditMode(bool enabled)
        {
            UILayoutEditMode.Enabled = enabled;
            if (enabled)
            {
                // Avoid two independent Game-view editors consuming the same pointer.
                TitleMenuEditMode.Enabled = false;
                PortraitEditMode.Enabled = false;
                SocialEditMode.Enabled = false;
                InvestigateHotspotEditMode.Enabled = false;
            }
            Debug.Log(enabled
                ? "[UI Layout] 编辑模式 ON — 在 Game 视图点击组件，拖中间移动，拖四角缩放；松手自动保存。"
                : "[UI Layout] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("通用 UI · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(
                "1. Play 并进入想修改的真实游戏界面\n" +
                "2. 启用编辑模式\n" +
                "3. Game 视图点击青色框选择组件\n" +
                "4. 拖框内移动；拖四角缩放\n" +
                "5. 松手自动保存到 Resources/UILayoutOverrides.asset\n\n" +
                "Alt+点击选父级；方向键微调；Delete 删除；F7 隐藏面板。",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            var enabled = UILayoutEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式", enabled);
            if (next != enabled)
                SetEditMode(next);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("拖动设置", EditorStyles.boldLabel);
            UILayoutEditMode.SnapEnabled = EditorGUILayout.Toggle("网格吸附", UILayoutEditMode.SnapEnabled);
            using (new EditorGUI.DisabledScope(!UILayoutEditMode.SnapEnabled))
                UILayoutEditMode.GridSize = EditorGUILayout.Slider("网格大小", UILayoutEditMode.GridSize, 1f, 100f);
            UILayoutEditMode.ShowAllFrames = EditorGUILayout.Toggle("显示全部组件框", UILayoutEditMode.ShowAllFrames);

            EditorGUILayout.Space(10f);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    UILayoutEditMode.Enabled
                        ? "Play 中 · 编辑已开启。请直接操作 Game 视图。"
                        : "Play 中 · 开启编辑模式后即可直接拖动。",
                    MessageType.None);

                var controller = UILayoutEditController.Instance;
                EditorGUILayout.LabelField("当前选择", controller != null
                    ? controller.SelectedDisplayName
                    : "等待编辑控制器…");
                using (new EditorGUI.DisabledScope(controller == null || !controller.HasSelection))
                {
                    if (GUILayout.Button("保存当前组件"))
                        controller.SaveSelection();

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                    if (GUILayout.Button("删除当前组件（可恢复）"))
                        controller.RequestDeleteSelection();
                    GUI.backgroundColor = oldColor;

                    if (GUILayout.Button("删除当前组件的已保存布局"))
                        controller.RemoveSelectionOverride();
                }
                using (new EditorGUI.DisabledScope(controller == null || !controller.CanRestoreLastDeleted))
                {
                    if (GUILayout.Button("恢复上一个删除的组件"))
                        controller.RestoreLastDeleted();
                }
                if (controller != null && GUILayout.Button("恢复全部已删除组件"))
                    controller.RestoreAllDeleted();
            }
            else
            {
                EditorGUILayout.HelpBox("请先进入 Play Mode，再打开要调整的界面。", MessageType.Warning);
                if (GUILayout.Button("进入 Play Mode"))
                    EditorApplication.isPlaying = true;
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("创建 / 选中布局资源"))
            {
                var asset = UILayoutOverrides.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            var data = UILayoutOverrides.Asset;
            EditorGUILayout.LabelField("已保存组件", data != null ? data.entries.Count.ToString() : "0");
            var deletedCount = 0;
            if (data != null && data.entries != null)
                for (var i = 0; i < data.entries.Count; i++)
                    if (data.entries[i] != null && data.entries[i].deleted) deletedCount++;
            EditorGUILayout.LabelField("已删除组件", deletedCount.ToString());
        }

        void OnInspectorUpdate() => Repaint();
    }
}
