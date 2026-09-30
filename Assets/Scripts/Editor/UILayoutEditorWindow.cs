using StreetCat.Investigation;
using StreetCat.Loc;
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
            window.minSize = new Vector2(420f, 640f);
            window.Show();
        }

        [MenuItem("街角专访/切换通用 UI 编辑模式", priority = 6)]
        public static void ToggleEditMode()
        {
            SetEditMode(!UILayoutEditMode.Enabled);
        }

        [MenuItem("街角专访/文本样式编辑器", priority = 7)]
        [MenuItem("StreetCat/Text Style Editor", priority = 7)]
        public static void OpenTextStyle()
        {
            UILayoutEditMode.LockSelection = false;
            UILayoutEditMode.TextFocus = true;
            SetEditMode(true);
            Open();
        }

        static void SetEditMode(bool enabled)
        {
            UILayoutEditMode.Enabled = enabled;
            if (!enabled)
                UILayoutEditMode.TextFocus = false;
            if (enabled)
            {
                // Avoid two independent Game-view editors consuming the same pointer.
                TitleMenuEditMode.Enabled = false;
                PortraitEditMode.Enabled = false;
                SocialEditMode.Enabled = false;
                InvestigateHotspotEditMode.Enabled = false;
            }
            Debug.Log(enabled
                ? "[UI Layout] 编辑模式 ON — 点选后默认锁定，按 L 解锁才能拖。"
                : "[UI Layout] 编辑模式 OFF");
        }

        Vector2 _scroll;

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("通用 UI · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(
                "1. Play 并进入想修改的真实游戏界面\n" +
                "2. 启用编辑模式\n" +
                "3. Game 视图点击青色框选择组件（优先点最深层控件）\n" +
                "   对话人名点文字本身（Name），拖动只挪名字\n" +
                "4. 拖框内移动；拖四角缩放；松手自动保存\n" +
                "5. 写入 Assets/Resources/UILayoutOverrides.asset\n" +
                "文本样式模式：点文字本身，单独改字体、字号、字距、粗体和颜色。\n\n" +
                "选中组件默认锁定。按 L 解锁后才能拖动；再点别的会重新锁定。\n" +
                "回退上一步 = 撤销最近一次移动、缩放、删除或去掉布局。\n" +
                "Alt+点击选父级；方向键微调；Delete 删除；F7 隐藏面板。",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            var enabled = UILayoutEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式 / Enable edit mode", enabled);
            if (next != enabled)
                SetEditMode(next);

            if (GUILayout.Button("关闭所有编辑器 / Turn off every editor"))
                StreetCatEditorMenus.DisableAllPlayEditors();

            var textFocus = UILayoutEditMode.TextFocus;
            var textNext = EditorGUILayout.ToggleLeft(
                "文本样式模式 / Edit text font & size（点选文字本身）",
                textFocus);
            if (textNext != textFocus)
            {
                UILayoutEditMode.TextFocus = textNext;
                if (textNext && !UILayoutEditMode.Enabled)
                    SetEditMode(true);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("选择 / Selection", EditorStyles.boldLabel);
            var lockSel = UILayoutEditMode.LockSelection;
            var lockNext = EditorGUILayout.ToggleLeft(
                lockSel
                    ? "已锁定 / Locked  (按 L 解锁后才能拖动)"
                    : "已解锁 / Unlocked  (可以拖动)",
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

                using (new EditorGUI.DisabledScope(controller == null || !controller.CanUndoStep))
                {
                    var undoLabel = controller != null && controller.CanUndoStep
                        ? "回退上一步 / Undo (" + controller.UndoStepCount + ")"
                        : "回退上一步 / Undo";
                    if (GUILayout.Button(undoLabel))
                        controller.UndoLastStep();
                }

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

                DrawTextStyle(controller);
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
            EditorGUILayout.EndScrollView();
        }

        static void DrawTextStyle(UILayoutEditController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("文本样式 / Text style", EditorStyles.boldLabel);
            if (!UILayoutEditMode.TextFocus)
            {
                EditorGUILayout.HelpBox("勾选「文本样式模式」后，在 Game 视图点文字本身。按钮上的字不会再选成整个按钮。", MessageType.None);
            }
            if (controller == null || !controller.TryReadSelectedTextStyle(out var state))
            {
                EditorGUILayout.HelpBox("还没有选中文字。", MessageType.None);
                return;
            }

            var options = FontCatalog.All;
            var names = new string[options.Length + 1];
            var ids = new string[options.Length + 1];
            names[0] = "跟随全局设置";
            ids[0] = "";
            var fontIndex = 0;
            for (var i = 0; i < options.Length; i++)
            {
                names[i + 1] = options[i].DisplayName;
                ids[i + 1] = options[i].Id;
                if (options[i].Id == state.fontId) fontIndex = i + 1;
            }

            var sample = state.sample ?? "";
            sample = sample.Replace("\n", " ");
            if (sample.Length > 48) sample = sample.Substring(0, 48) + "…";
            EditorGUILayout.LabelField("内容", string.IsNullOrEmpty(sample) ? "（空）" : sample);

            EditorGUI.BeginChangeCheck();
            fontIndex = EditorGUILayout.Popup("字体", fontIndex, names);
            var fontChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            var customSize = EditorGUILayout.Toggle("自定义字号", state.customSize);
            var sizeToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var shownSize = customSize ? state.fontSize : state.liveSize;
            var fontSize = EditorGUILayout.Slider("字号", Mathf.Clamp(shownSize, 10f, 72f), 10f, 72f);
            var sizeDragged = EditorGUI.EndChangeCheck();
            if (sizeDragged) customSize = true;

            EditorGUI.BeginChangeCheck();
            var customSpacing = EditorGUILayout.Toggle("自定义字距", state.customSpacing);
            var spacingToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var spacing = EditorGUILayout.Slider("字距", state.letterSpacing, 0f, 12f);
            var spacingDragged = EditorGUI.EndChangeCheck();
            if (spacingDragged) customSpacing = true;

            var weightNames = new[] { "跟随全局", "细", "常规", "中等", "半粗", "粗", "特粗" };
            var weightValues = new[] { 0, 300, 400, 500, 600, 700, 800 };
            var weightIndex = 0;
            for (var i = 0; i < weightValues.Length; i++)
            {
                if (weightValues[i] == state.weightMode) weightIndex = i;
            }
            EditorGUI.BeginChangeCheck();
            weightIndex = EditorGUILayout.Popup("字重", weightIndex, weightNames);
            var weightChanged = EditorGUI.EndChangeCheck();
            var weight = weightValues[Mathf.Clamp(weightIndex, 0, weightValues.Length - 1)];

            EditorGUI.BeginChangeCheck();
            var customColor = EditorGUILayout.Toggle("自定义颜色", state.customColor);
            var colorToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var color = EditorGUILayout.ColorField("颜色", state.color);
            var colorChanged = EditorGUI.EndChangeCheck();
            if (colorChanged) customColor = true;

            var changed = fontChanged || sizeToggleChanged || sizeDragged
                || spacingToggleChanged || spacingDragged
                || weightChanged || colorToggleChanged || colorChanged;

            if (GUILayout.Button("清除这个文字的单独样式"))
            {
                state.fontId = "";
                state.customSize = false;
                state.customSpacing = false;
                state.weightMode = 0;
                state.customColor = false;
                controller.WriteSelectedTextStyle(state);
                return;
            }

            if (!changed) return;
            state.fontId = ids[Mathf.Clamp(fontIndex, 0, ids.Length - 1)];
            state.customSize = customSize;
            state.fontSize = fontSize;
            state.customSpacing = customSpacing;
            state.letterSpacing = spacing;
            state.weightMode = weight;
            state.customColor = customColor;
            state.color = color;
            controller.WriteSelectedTextStyle(state);
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

        void OnInspectorUpdate()
        {
            // Repainting while a slider or popup is hot throws away the click.
            if (GUIUtility.hotControl != 0) return;
            if (mouseOverWindow == this) return;
            Repaint();
        }
    }
}
