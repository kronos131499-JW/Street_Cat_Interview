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
            window.minSize = new Vector2(400f, 460f);
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
                ? "[UI Layout] 编辑模式 ON — Game 视图点击选择；拖中间移动，拖四角缩放；松手自动保存。L=锁定选中。"
                : "[UI Layout] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("通用 UI · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(
                "1. Play 并进入想修改的真实游戏界面\n" +
                "2. 启用编辑模式\n" +
                "3. Game 视图点击青色框选择组件（优先点最深层控件）\n" +
                "4. 拖框内移动；拖四角缩放；松手自动保存\n" +
                "5. 写入 Assets/Resources/UILayoutOverrides.asset\n\n" +
                "L / 下方勾选 = 锁定选中（点击不再改选）\n" +
                "Alt+点击选父级；方向键微调；Delete 删除；F7 隐藏面板。",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            var enabled = UILayoutEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式 / Enable edit mode", enabled);
            if (next != enabled)
                SetEditMode(next);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("选择 / Selection", EditorStyles.boldLabel);
            var lockSel = UILayoutEditMode.LockSelection;
            var lockNext = EditorGUILayout.ToggleLeft(
                lockSel
                    ? "锁定选中 / Lock selection  (ON — 点击不会改选，按 L 解锁)"
                    : "锁定选中 / Lock selection  (OFF — 按 L 开关)",
                lockSel);
            if (lockNext != lockSel)
                UILayoutEditMode.LockSelection = lockNext;
            UILayoutEditMode.SkipFullscreenCatchers = EditorGUILayout.ToggleLeft(
                "忽略全屏遮罩/Catcher / Skip fullscreen catchers",
                UILayoutEditMode.SkipFullscreenCatchers);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("拖动设置 / Drag", EditorStyles.boldLabel);
            UILayoutEditMode.SnapEnabled = EditorGUILayout.Toggle("网格吸附 / Snap", UILayoutEditMode.SnapEnabled);
            using (new EditorGUI.DisabledScope(!UILayoutEditMode.SnapEnabled))
                UILayoutEditMode.GridSize = EditorGUILayout.Slider("网格大小 / Grid", UILayoutEditMode.GridSize, 1f, 100f);
            UILayoutEditMode.ShowAllFrames = EditorGUILayout.Toggle("显示全部组件框 / Show all frames", UILayoutEditMode.ShowAllFrames);

            EditorGUILayout.Space(10f);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    UILayoutEditMode.Enabled
                        ? (UILayoutEditMode.LockSelection
                            ? "Play · 编辑开启 · 选中已锁定 — 仅可拖当前组件。"
                            : "Play · 编辑已开启。请直接操作 Game 视图。")
                        : "Play · 开启编辑模式后即可直接拖动。",
                    MessageType.None);

                var controller = UILayoutEditController.Instance;
                var selLabel = controller != null ? controller.SelectedDisplayName : "等待编辑控制器…";
                if (controller != null && controller.IsDirty)
                    selLabel = "* " + selLabel + "  (未保存拖动中)";
                EditorGUILayout.LabelField("当前选择", selLabel);

                using (new EditorGUI.DisabledScope(controller == null || !controller.HasSelection))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("保存选中 / Save"))
                        controller.SaveSelection();
                    using (new EditorGUI.DisabledScope(controller == null || !controller.SelectionHasSavedOverride))
                    {
                        if (GUILayout.Button("还原选中 / Revert"))
                            controller.RevertSelection();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (GUILayout.Button("删除已保存布局条目 / Remove saved override"))
                        controller.RemoveSelectionOverride();

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                    if (GUILayout.Button("隐藏并持久删除组件 / Delete component"))
                        controller.RequestDeleteSelection();
                    GUI.backgroundColor = oldColor;
                }
                using (new EditorGUI.DisabledScope(controller == null || !controller.CanRestoreLastDeleted))
                {
                    if (GUILayout.Button("恢复上一个删除 / Restore last deleted"))
                        controller.RestoreLastDeleted();
                }
                if (controller != null && GUILayout.Button("恢复全部已删除 / Restore all deleted"))
                    controller.RestoreAllDeleted();

                DrawStatusLine(controller);
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
            EditorGUILayout.LabelField("资源路径", UILayoutOverrides.AssetDiskPath);
            EditorGUILayout.LabelField("已保存组件", data != null ? data.entries.Count.ToString() : "0");
            var deletedCount = 0;
            if (data != null && data.entries != null)
                for (var i = 0; i < data.entries.Count; i++)
                    if (data.entries[i] != null && data.entries[i].deleted) deletedCount++;
            EditorGUILayout.LabelField("已删除组件", deletedCount.ToString());
        }

        static void DrawStatusLine(UILayoutEditController controller)
        {
            var msg = controller != null ? controller.StatusLine : UILayoutOverrides.LastOperationMessage;
            if (string.IsNullOrEmpty(msg))
            {
                EditorGUILayout.HelpBox("尚未保存 — 拖动松手或点「保存选中」后这里会显示结果。", MessageType.None);
                return;
            }
            var ok = controller != null ? controller.StatusOk : UILayoutOverrides.LastOperationOk;
            var prev = GUI.color;
            GUI.color = ok ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f);
            EditorGUILayout.HelpBox((ok ? "✓ " : "✗ ") + msg, ok ? MessageType.Info : MessageType.Error);
            GUI.color = prev;
        }

        void OnInspectorUpdate() => Repaint();
    }
}
