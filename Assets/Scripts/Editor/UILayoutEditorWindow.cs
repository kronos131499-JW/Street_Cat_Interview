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
        static string T(string zh, string en) => ToolLang.T(zh, en);

        [MenuItem("街角专访/通用 UI 布局编辑器", priority = 5)]
        [MenuItem("StreetCat/UI Layout Editor", priority = 5)]
        public static void Open()
        {
            var window = GetWindow<UILayoutEditorWindow>(T("通用 UI 布局", "UI Layout"));
            window.minSize = new Vector2(420f, 640f);
            window.Show();
        }

        [MenuItem("街角专访/切换通用 UI 编辑模式", priority = 6)]
        [MenuItem("StreetCat/Toggle UI Layout Edit Mode", priority = 6)]
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
                ? T("[UI Layout] 编辑模式 ON — 点选组件后自动锁定，可以拖它，按 L 解锁再选别的。",
                    "[UI Layout] Edit mode ON — clicking a component locks it; you can still drag it. Press L to unlock and pick another.")
                : T("[UI Layout] 编辑模式 OFF", "[UI Layout] Edit mode OFF"));
        }

        Vector2 _scroll;

        void OnGUI()
        {
            titleContent.text = T("通用 UI 布局", "UI Layout");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            ToolLang.DrawToggle();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(T("通用 UI · Game 视图编辑", "UI Layout · Edit in Game view"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(T(
                "1. Play 并进入想修改的真实游戏界面\n" +
                "2. 启用编辑模式\n" +
                "3. Game 视图点击青色框选择组件（优先点最深层控件）\n" +
                "   对话人名点文字本身（Name），拖动只挪名字\n" +
                "4. 拖框内移动；拖四角缩放；松手自动保存\n" +
                "5. 写入 Assets/Resources/UILayoutOverrides.asset\n" +
                "文本样式模式：点文字本身，单独改字体、字号、字距、粗体和颜色。\n\n" +
                "选中组件后自动锁定：可以继续拖它，点别的不会切换。按 L 解锁后再选别的。\n" +
                "回退上一步 = 撤销最近一次移动、缩放、删除或去掉布局。\n" +
                "Alt+点击选父级；方向键微调；Delete 删除；F7 隐藏面板。",
                "1. Enter Play Mode and open the real game screen you want to change\n" +
                "2. Enable edit mode\n" +
                "3. Click a cyan frame in the Game view to select it (the deepest control wins)\n" +
                "   For dialogue speaker names, click the text itself (Name) to move just the name\n" +
                "4. Drag inside the frame to move; drag a corner to resize; release to auto-save\n" +
                "5. Saved to Assets/Resources/UILayoutOverrides.asset\n" +
                "Text style mode: click a text to change its font, size, spacing, weight and color.\n\n" +
                "A new selection locks automatically: you can keep dragging it, and clicks elsewhere won't switch.\n" +
                "Press L to unlock and pick another component.\n" +
                "Undo = revert the last move, resize, delete or override removal.\n" +
                "Alt+click selects the parent; arrow keys nudge; Delete removes; F7 hides the panel."),
                MessageType.Info);

            EditorGUILayout.Space(8f);
            var enabled = UILayoutEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft(T("启用 Game 视图编辑模式", "Enable Game-view edit mode"), enabled);
            if (next != enabled)
                SetEditMode(next);

            if (GUILayout.Button(T("关闭所有编辑器", "Turn off every editor")))
                StreetCatEditorMenus.DisableAllPlayEditors();

            var textFocus = UILayoutEditMode.TextFocus;
            var textNext = EditorGUILayout.ToggleLeft(
                T("文本样式模式（点选文字本身）", "Text style mode (click the text itself)"),
                textFocus);
            if (textNext != textFocus)
            {
                UILayoutEditMode.TextFocus = textNext;
                if (textNext && !UILayoutEditMode.Enabled)
                    SetEditMode(true);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(T("选择", "Selection"), EditorStyles.boldLabel);
            var lockSel = UILayoutEditMode.LockSelection;
            var lockNext = EditorGUILayout.ToggleLeft(
                lockSel
                    ? T("已锁定当前组件（可以拖它，点别的不会切换）", "Locked (drag it freely; clicks elsewhere won't switch)")
                    : T("未锁定（点哪个就改哪个）", "Unlocked (click any component to edit it)"),
                lockSel);
            if (lockNext != lockSel)
                UILayoutEditMode.LockSelection = lockNext;
            UILayoutEditMode.SkipFullscreenCatchers = EditorGUILayout.ToggleLeft(
                T("忽略全屏遮罩 / Catcher", "Skip fullscreen catchers"),
                UILayoutEditMode.SkipFullscreenCatchers);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(T("拖动设置", "Drag"), EditorStyles.boldLabel);
            UILayoutEditMode.SnapEnabled = EditorGUILayout.Toggle(T("网格吸附", "Snap to grid"), UILayoutEditMode.SnapEnabled);
            using (new EditorGUI.DisabledScope(!UILayoutEditMode.SnapEnabled))
                UILayoutEditMode.GridSize = EditorGUILayout.Slider(T("网格大小", "Grid size"), UILayoutEditMode.GridSize, 1f, 100f);
            UILayoutEditMode.ShowAllFrames = EditorGUILayout.Toggle(T("显示全部组件框", "Show all frames"), UILayoutEditMode.ShowAllFrames);

            EditorGUILayout.Space(10f);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    UILayoutEditMode.Enabled
                        ? (UILayoutEditMode.LockSelection
                            ? T("Play · 编辑开启 · 已锁定当前组件，可以拖动它，点别的不会切换。",
                                "Play · Editing · Current component locked. Drag it; clicks elsewhere won't switch.")
                            : T("Play · 编辑已开启。请直接操作 Game 视图。",
                                "Play · Editing. Work directly in the Game view."))
                        : T("Play · 开启编辑模式后即可直接拖动。",
                            "Play · Enable edit mode to start dragging."),
                    MessageType.None);

                var controller = UILayoutEditController.Instance;
                var selLabel = controller != null ? controller.SelectedDisplayName : T("等待编辑控制器…", "Waiting for edit controller…");
                if (controller != null && controller.IsDirty)
                    selLabel = "* " + selLabel + T("  (未保存拖动中)", "  (unsaved drag)");
                EditorGUILayout.LabelField(T("当前选择", "Selected"), selLabel);

                using (new EditorGUI.DisabledScope(controller == null || !controller.CanUndoStep))
                {
                    var undoLabel = controller != null && controller.CanUndoStep
                        ? T("回退上一步", "Undo") + " (" + controller.UndoStepCount + ")"
                        : T("回退上一步", "Undo");
                    if (GUILayout.Button(undoLabel))
                        controller.UndoLastStep();
                }

                using (new EditorGUI.DisabledScope(controller == null || !controller.HasSelection))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(T("保存选中", "Save selection")))
                        controller.SaveSelection();
                    using (new EditorGUI.DisabledScope(controller == null || !controller.SelectionHasSavedOverride))
                    {
                        if (GUILayout.Button(T("还原选中", "Revert selection")))
                            controller.RevertSelection();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (GUILayout.Button(T("删除已保存布局条目", "Remove saved override")))
                        controller.RemoveSelectionOverride();

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                    if (GUILayout.Button(T("隐藏并持久删除组件", "Delete component (hide permanently)")))
                        controller.RequestDeleteSelection();
                    GUI.backgroundColor = oldColor;
                }
                using (new EditorGUI.DisabledScope(controller == null || !controller.CanRestoreLastDeleted))
                {
                    if (GUILayout.Button(T("恢复上一个删除", "Restore last deleted")))
                        controller.RestoreLastDeleted();
                }
                if (controller != null && GUILayout.Button(T("恢复全部已删除", "Restore all deleted")))
                    controller.RestoreAllDeleted();

                DrawTextStyle(controller);
                DrawStatusLine(controller);
            }
            else
            {
                EditorGUILayout.HelpBox(T("请先进入 Play Mode，再打开要调整的界面。",
                    "Enter Play Mode first, then open the screen you want to adjust."), MessageType.Warning);
                if (GUILayout.Button(T("进入 Play Mode", "Enter Play Mode")))
                    EditorApplication.isPlaying = true;
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button(T("创建 / 选中布局资源", "Create / select layout asset")))
            {
                var asset = UILayoutOverrides.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            var data = UILayoutOverrides.Asset;
            EditorGUILayout.LabelField(T("资源路径", "Asset path"), UILayoutOverrides.AssetDiskPath);
            EditorGUILayout.LabelField(T("已保存组件", "Saved components"), data != null ? data.entries.Count.ToString() : "0");
            var deletedCount = 0;
            if (data != null && data.entries != null)
                for (var i = 0; i < data.entries.Count; i++)
                    if (data.entries[i] != null && data.entries[i].deleted) deletedCount++;
            EditorGUILayout.LabelField(T("已删除组件", "Deleted components"), deletedCount.ToString());
            EditorGUILayout.EndScrollView();
        }

        static void DrawTextStyle(UILayoutEditController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(T("文本样式", "Text style"), EditorStyles.boldLabel);
            if (!UILayoutEditMode.TextFocus)
            {
                EditorGUILayout.HelpBox(T(
                    "勾选「文本样式模式」后，在 Game 视图点文字本身。按钮上的字不会再选成整个按钮。",
                    "Turn on \"Text style mode\", then click the text itself in the Game view. Button labels are picked as text, not as the whole button."),
                    MessageType.None);
            }
            if (controller == null || !controller.TryReadSelectedTextStyle(out var state))
            {
                EditorGUILayout.HelpBox(T("还没有选中文字。", "No text selected yet."), MessageType.None);
                return;
            }

            var options = FontCatalog.All;
            var names = new string[options.Length + 1];
            var ids = new string[options.Length + 1];
            names[0] = T("跟随全局设置", "Follow global setting");
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
            EditorGUILayout.LabelField(T("内容", "Content"), string.IsNullOrEmpty(sample) ? T("（空）", "(empty)") : sample);

            EditorGUI.BeginChangeCheck();
            fontIndex = EditorGUILayout.Popup(T("字体", "Font"), fontIndex, names);
            var fontChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            var customSize = EditorGUILayout.Toggle(T("自定义字号", "Custom size"), state.customSize);
            var sizeToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var shownSize = customSize ? state.fontSize : state.liveSize;
            var fontSize = EditorGUILayout.Slider(T("字号", "Size"), Mathf.Clamp(shownSize, 10f, 72f), 10f, 72f);
            var sizeDragged = EditorGUI.EndChangeCheck();
            if (sizeDragged) customSize = true;

            EditorGUI.BeginChangeCheck();
            var customSpacing = EditorGUILayout.Toggle(T("自定义字距", "Custom spacing"), state.customSpacing);
            var spacingToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var spacing = EditorGUILayout.Slider(T("字距", "Letter spacing"), state.letterSpacing, 0f, 12f);
            var spacingDragged = EditorGUI.EndChangeCheck();
            if (spacingDragged) customSpacing = true;

            var weightNames = ToolLang.English
                ? new[] { "Follow global", "Light", "Regular", "Medium", "Semibold", "Bold", "Extra bold" }
                : new[] { "跟随全局", "细", "常规", "中等", "半粗", "粗", "特粗" };
            var weightValues = new[] { 0, 300, 400, 500, 600, 700, 800 };
            var weightIndex = 0;
            for (var i = 0; i < weightValues.Length; i++)
            {
                if (weightValues[i] == state.weightMode) weightIndex = i;
            }
            EditorGUI.BeginChangeCheck();
            weightIndex = EditorGUILayout.Popup(T("字重", "Weight"), weightIndex, weightNames);
            var weightChanged = EditorGUI.EndChangeCheck();
            var weight = weightValues[Mathf.Clamp(weightIndex, 0, weightValues.Length - 1)];

            EditorGUI.BeginChangeCheck();
            var customColor = EditorGUILayout.Toggle(T("自定义颜色", "Custom color"), state.customColor);
            var colorToggleChanged = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            var color = EditorGUILayout.ColorField(T("颜色", "Color"), state.color);
            var colorChanged = EditorGUI.EndChangeCheck();
            if (InkSwatches.DrawRow(ref color))
            {
                customColor = true;
                colorChanged = true;
            }
            if (colorChanged) customColor = true;

            var changed = fontChanged || sizeToggleChanged || sizeDragged
                || spacingToggleChanged || spacingDragged
                || weightChanged || colorToggleChanged || colorChanged;

            if (GUILayout.Button(T("清除这个文字的单独样式", "Clear this text's custom style")))
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
                EditorGUILayout.HelpBox(T("尚未保存 — 拖动松手或点「保存选中」后这里会显示结果。",
                    "Nothing saved yet — release a drag or click \"Save selection\" to see the result here."), MessageType.None);
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
