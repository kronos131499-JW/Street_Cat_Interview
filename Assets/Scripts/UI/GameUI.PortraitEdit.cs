#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        bool portraitEditPanelVisible = true;
        bool portraitEditPreviewApplied;
        Vector2 _portraitEditScroll;
        string _portraitEditPreviewKey = "ch_shenhe_default";
        bool _advanceCatcherRaycastBeforeEdit = true;
        bool _advanceCatcherActiveBeforeEdit = true;
        bool _portraitEditInputCaptured;
        static Rect _portraitEditPanelScreenRect;

        // IMGUI slot drag state (screen-space, reliable in Game view)
        static int _slotDragMode;
        static Vector2 _slotDragStartMouse;
        static float _slotDragL, _slotDragR, _slotDragB, _slotDragT;
        static int _slotHotControl;

        const int SlotDragNone = 0;
        const int SlotDragMove = 1;
        const int SlotDragBL = 2;
        const int SlotDragBR = 3;
        const int SlotDragTL = 4;
        const int SlotDragTR = 5;

        public static bool PortraitSlotIsDragging => _slotHotControl != 0;

        public static bool PortraitEditPanelConsumesMouse()
        {
            if (!PortraitEditMode.Enabled) return false;
            return _portraitEditPanelScreenRect.width > 0f
                   && _portraitEditPanelScreenRect.Contains(
                       new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        void BuildPortraitEditOverlay(Transform parent)
        {
            // Slot editing is IMGUI-only; no UIView frame needed.
            _ = parent;
        }

        void TickPortraitEditMode()
        {
            if (!PortraitEditMode.Enabled)
            {
                portraitEditPreviewApplied = false;
                _portraitEditPanelScreenRect = Rect.zero;
                _slotHotControl = 0;
                _slotDragMode = SlotDragNone;
                _portraitEditInputCaptured = false;
                RestorePortraitEditInputBlock();
                SetAdvanceEnabled(canClickAdvance, waitingForChoice);
                return;
            }

            BlockInputForPortraitEdit();

            if (!_portraitEditInputCaptured)
            {
                if (advanceCatcher != null)
                {
                    _advanceCatcherRaycastBeforeEdit = advanceCatcher.raycastTarget;
                    _advanceCatcherActiveBeforeEdit = advanceCatcher.gameObject.activeSelf;
                }
                _portraitEditInputCaptured = true;
            }

            if (!portraitEditPreviewApplied)
            {
                EnsurePortraitEditPreview();
                portraitEditPreviewApplied = true;
                RefreshPortraitLayoutFromAsset();
            }
        }

        void LateUpdatePortraitEdit()
        {
            // IMGUI handles drawn in OnGUI.
        }

        void BlockInputForPortraitEdit()
        {
            if (advanceCatcher != null)
            {
                advanceCatcher.raycastTarget = false;
                advanceCatcher.gameObject.SetActive(false);
                var btn = advanceCatcher.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
            if (dialogueClick != null)
            {
                dialogueClick.interactable = false;
                var dlgImg = dialogueClick.GetComponent<Image>();
                if (dlgImg != null) dlgImg.raycastTarget = false;
            }
            if (hideDialogueBtn != null)
                hideDialogueBtn.interactable = false;
        }

        void RestorePortraitEditInputBlock()
        {
            if (advanceCatcher != null)
            {
                advanceCatcher.raycastTarget = _advanceCatcherRaycastBeforeEdit;
                advanceCatcher.gameObject.SetActive(_advanceCatcherActiveBeforeEdit);
            }
            if (dialogueClick != null)
            {
                var dlgImg = dialogueClick.GetComponent<Image>();
                if (dlgImg != null) dlgImg.raycastTarget = true;
            }
            if (hideDialogueBtn != null)
                hideDialogueBtn.interactable = true;
        }

        void EnsurePortraitEditPreview()
        {
            if (mode == Mode.Title || mode == Mode.Interview) return;
            if (portraitImage == null) return;
            if (portraitImage.enabled && portraitImage.sprite != null) return;
            SetPortrait(_portraitEditPreviewKey);
        }

        public void RefreshPortraitLayoutFromAsset()
        {
            if (portraitImage != null && portraitImage.sprite != null)
                LayoutPortraitRect(portraitImage.sprite);
        }

        bool HandlePortraitEditHotkey()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                portraitEditPanelVisible = !portraitEditPanelVisible;
                return true;
            }
            return false;
        }

        void DrawPortraitEditImGui()
        {
            if (!PortraitEditMode.Enabled || !Application.isPlaying) return;

            DrawPortraitSlotImGui();

            if (!portraitEditPanelVisible) return;

            const float w = 320f;
            float maxH = Screen.height - 24f;
            float h = Mathf.Min(maxH, 720f);
            var outer = new Rect(12f, 12f, w, h);
            _portraitEditPanelScreenRect = outer;
            GUI.Box(outer, "立绘布局 (F10 隐藏)");

            var inner = new Rect(outer.x + 8f, outer.y + 22f, outer.width - 16f, outer.height - 30f);
            _portraitEditScroll = GUI.BeginScrollView(inner, _portraitEditScroll,
                new Rect(0f, 0f, inner.width - 22f, 680f));

            GUILayout.BeginArea(new Rect(0f, 0f, inner.width - 24f, 680f));
            GUILayout.Label("Game 视图：拖青色框移动/缩放", new GUIStyle(GUI.skin.label) { wordWrap = true });

            var d = PortraitLayout.EnsureAsset();
            if (d == null)
            {
                GUILayout.EndArea();
                GUI.EndScrollView();
                return;
            }

            if (EditorGUILayoutSliders(d))
                RefreshPortraitLayoutFromAsset();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
                PortraitLayout.SaveCurrent();
            if (GUILayout.Button("恢复默认"))
            {
                PortraitLayout.ResetToThemeDefaults();
                RefreshPortraitLayoutFromAsset();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("预览角色", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            string[] keys = { "ch_shenhe_default", "ch_dafu_default", "ch_lin_default", "ch_guard_default", "ch_xiaoling_default" };
            string[] labels = { "沈禾", "大福", "林女士", "保安", "小凌" };
            GUILayout.BeginHorizontal();
            for (int i = 0; i < keys.Length; i++)
            {
                if (GUILayout.Button(labels[i], GUILayout.Width(56f)))
                {
                    _portraitEditPreviewKey = keys[i];
                    SetPortrait(keys[i]);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                $"区域 ({d.slotLeft:F2},{d.slotBottom:F2})–({d.slotRight:F2},{d.slotTop:F2})\n"
                + $"缩放 {d.heightScale:F2}  偏右 {d.centerBias:F2}  上移 {d.offsetY:F3}",
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label("顶/底边界：数值越小越靠下；垂直微调可负值下移",
                new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });

            GUILayout.EndArea();
            GUI.EndScrollView();
        }

        void DrawPortraitSlotImGui()
        {
            if (PortraitEditPanelConsumesMouse() && _slotHotControl == 0)
                return;

            var d = PortraitLayout.EnsureAsset();
            if (d == null) return;

            var slotRect = AnchorsToGuiRect(d.slotLeft, d.slotBottom, d.slotRight, d.slotTop);
            DrawSlotOutline(slotRect);

            int controlId = GUIUtility.GetControlID("PortraitSlotImGui".GetHashCode(), FocusType.Passive);
            var e = Event.current;
            const float handle = 14f;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != 0) break;
                    _slotDragMode = PickSlotDragMode(e.mousePosition, slotRect, handle);
                    if (_slotDragMode == SlotDragNone) break;
                    _slotDragStartMouse = e.mousePosition;
                    _slotDragL = d.slotLeft;
                    _slotDragR = d.slotRight;
                    _slotDragB = d.slotBottom;
                    _slotDragT = d.slotTop;
                    _slotHotControl = controlId;
                    GUIUtility.hotControl = controlId;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId) break;
                    ApplySlotImGuiDrag(d, e.mousePosition - _slotDragStartMouse);
                    RefreshPortraitLayoutFromAsset();
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId) break;
                    _slotHotControl = 0;
                    _slotDragMode = SlotDragNone;
                    GUIUtility.hotControl = 0;
                    d.Clamp();
                    PortraitLayout.SaveCurrent();
                    RefreshPortraitLayoutFromAsset();
                    e.Use();
                    break;

                case EventType.Repaint:
                    var label = _slotHotControl != 0 ? "拖动中…" : "立绘区域 · 拖中间移动 · 拖角缩放";
                    var style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                    GUI.color = new Color(0.2f, 0.75f, 1f, 1f);
                    GUI.Label(new Rect(slotRect.x, slotRect.y - 20f, slotRect.width, 20f), label, style);
                    GUI.color = Color.white;
                    break;
            }
        }

        static Rect AnchorsToGuiRect(float left, float bottom, float right, float top)
        {
            float sw = Screen.width;
            float sh = Screen.height;
            return new Rect(
                left * sw,
                sh - top * sh,
                (right - left) * sw,
                (top - bottom) * sh);
        }

        static void DrawSlotOutline(Rect r)
        {
            var fill = new Color(0.2f, 0.75f, 1f, 0.12f);
            var line = new Color(0.2f, 0.75f, 1f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
            DrawGuiRectBorder(r, line, 2f);
        }

        static void DrawGuiRectBorder(Rect r, Color c, float thickness)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static int PickSlotDragMode(Vector2 mouse, Rect slotRect, float handle)
        {
            var bl = new Rect(slotRect.xMin, slotRect.yMax - handle, handle, handle);
            var br = new Rect(slotRect.xMax - handle, slotRect.yMax - handle, handle, handle);
            var tl = new Rect(slotRect.xMin, slotRect.yMin, handle, handle);
            var tr = new Rect(slotRect.xMax - handle, slotRect.yMin, handle, handle);
            if (bl.Contains(mouse)) return SlotDragBL;
            if (br.Contains(mouse)) return SlotDragBR;
            if (tl.Contains(mouse)) return SlotDragTL;
            if (tr.Contains(mouse)) return SlotDragTR;
            if (slotRect.Contains(mouse)) return SlotDragMove;
            return SlotDragNone;
        }

        static void ApplySlotImGuiDrag(PortraitLayoutData d, Vector2 deltaGui)
        {
            float dx = deltaGui.x / Screen.width;
            float dy = -deltaGui.y / Screen.height; // GUI Y down, anchor Y up

            float l = _slotDragL, r = _slotDragR, b = _slotDragB, t = _slotDragT;
            switch (_slotDragMode)
            {
                case SlotDragMove:
                    l += dx; r += dx; b += dy; t += dy;
                    break;
                case SlotDragBL:
                    l += dx; b += dy;
                    break;
                case SlotDragBR:
                    r += dx; b += dy;
                    break;
                case SlotDragTL:
                    l += dx; t += dy;
                    break;
                case SlotDragTR:
                    r += dx; t += dy;
                    break;
            }

            if (_slotDragMode == SlotDragMove)
                KeepSlotOnScreen(ref l, ref r, ref b, ref t);

            d.slotLeft = l;
            d.slotRight = r;
            d.slotBottom = b;
            d.slotTop = t;
            d.Clamp();
        }

        /// <summary>
        /// Moving the whole slot into the screen edge used to clamp only the top,
        /// which shrank the slot and left the figure where it was.
        /// </summary>
        static void KeepSlotOnScreen(ref float l, ref float r, ref float b, ref float t)
        {
            float w = r - l;
            float h = t - b;
            if (t > 1f)
            {
                t = 1f;
                b = t - h;
            }
            float minBottom = VnTheme.PortraitSlotBottomMin;
            if (b < minBottom)
            {
                b = minBottom;
                t = Mathf.Min(1f, b + h);
            }
            if (r > 1f)
            {
                r = 1f;
                l = r - w;
            }
            if (l < 0f)
            {
                l = 0f;
                r = Mathf.Min(1f, l + w);
            }
        }

        bool EditorGUILayoutSliders(PortraitLayoutData d)
        {
            bool changed = false;
            float minBottom = VnTheme.PortraitSlotBottomMin;
            float minTop = minBottom + PortraitLayout.MinSlotHeight;

            changed |= ApplySlider(ref d.slotLeft, GUILayout.HorizontalSlider(d.slotLeft, 0.35f, 0.95f));
            GUILayout.Label("左边界 " + d.slotLeft.ToString("F2"));
            changed |= ApplySlider(ref d.slotRight, GUILayout.HorizontalSlider(d.slotRight, 0.45f, 1f));
            GUILayout.Label("右边界 " + d.slotRight.ToString("F2"));
            changed |= ApplySlider(ref d.slotTop, GUILayout.HorizontalSlider(d.slotTop, minTop, 1f));
            GUILayout.Label("顶边界 " + d.slotTop.ToString("F2") + "（↓变小）");
            changed |= ApplySlider(ref d.slotBottom, GUILayout.HorizontalSlider(d.slotBottom, minBottom, 0.85f));
            GUILayout.Label("底边界 " + d.slotBottom.ToString("F2"));
            changed |= ApplySlider(ref d.heightScale, GUILayout.HorizontalSlider(d.heightScale, 0.45f, 1.8f));
            GUILayout.Label("缩放 " + d.heightScale.ToString("F2"));
            changed |= ApplySlider(ref d.centerBias, GUILayout.HorizontalSlider(d.centerBias, 0f, 1f));
            GUILayout.Label("水平位置 " + d.centerBias.ToString("F2"));
            changed |= ApplySlider(ref d.offsetY, GUILayout.HorizontalSlider(d.offsetY, -0.35f, 0.35f));
            GUILayout.Label("垂直微调 " + d.offsetY.ToString("F3") + "（负=下移）");

            if (changed)
                d.Clamp();
            return changed;
        }

        static bool ApplySlider(ref float field, float sliderValue)
        {
            if (Mathf.Approximately(field, sliderValue)) return false;
            field = sliderValue;
            return true;
        }
    }
}
#endif
