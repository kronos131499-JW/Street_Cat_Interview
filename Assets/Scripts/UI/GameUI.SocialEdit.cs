#if UNITY_EDITOR
using UnityEngine;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        bool socialEditPanelVisible = true;
        bool socialEditPreviewApplied;
        static Rect _socialEditPanelScreenRect;

        static int _socialDragMode;
        static Vector2 _socialDragStartMouse;
        static float _socialDragW, _socialDragH, _socialDragAx, _socialDragAy;
        static int _socialHotControl;

        const int SocialDragNone = 0;
        const int SocialDragMove = 1;
        const int SocialDragBL = 2;
        const int SocialDragBR = 3;
        const int SocialDragTL = 4;
        const int SocialDragTR = 5;

        public static bool SocialEditPanelConsumesMouse()
        {
            if (!SocialEditMode.Enabled) return false;
            return _socialEditPanelScreenRect.width > 0f
                   && _socialEditPanelScreenRect.Contains(
                       new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        void TickSocialEditMode()
        {
            if (!SocialEditMode.Enabled)
            {
                socialEditPreviewApplied = false;
                _socialEditPanelScreenRect = Rect.zero;
                _socialHotControl = 0;
                _socialDragMode = SocialDragNone;
                return;
            }

            if (!socialEditPreviewApplied)
            {
                EnsureSocialEditPreview();
                socialEditPreviewApplied = true;
                RefreshSocialLayoutFromAsset();
            }
        }

        bool HandleSocialEditHotkey()
        {
            if (!SocialEditMode.Enabled) return false;
            if (Input.GetKeyDown(KeyCode.F8))
            {
                socialEditPanelVisible = !socialEditPanelVisible;
                return true;
            }
            return false;
        }

        void EnsureSocialEditPreview()
        {
            if (canvasRt != null && !socialBuilt)
                BuildSocialOverlay(canvasRt);
            if (socialRoot == null) return;
            if (socialRoot.activeSelf && !string.IsNullOrEmpty(socialSpriteKey))
            {
                RefreshSocialLayoutFromAsset();
                return;
            }

            DebugPreviewSocialPost("social_post_01_feed");
        }

        /// <summary>Editor / debug: force-show a social feed sprite for layout tuning.</summary>
        public void DebugPreviewSocialPost(string resourceKey, bool detail = false)
        {
            if (canvasRt != null && !socialBuilt)
                BuildSocialOverlay(canvasRt);
            if (socialRoot == null) return;
            SocialShowSprite(resourceKey, detail, instant: true);
            RefreshSocialLayoutFromAsset();
        }

        void DrawSocialEditImGui()
        {
            if (!SocialEditMode.Enabled || !Application.isPlaying) return;

            DrawSocialPhoneImGui();

            if (!socialEditPanelVisible) return;

            const float w = 300f;
            float h = Mathf.Min(Screen.height - 24f, 720f);
            var outer = new Rect(Screen.width - w - 12f, 12f, w, h);
            _socialEditPanelScreenRect = outer;
            GUI.Box(outer, "社交帖子布局 (F8 隐藏)");

            var inner = new Rect(outer.x + 8f, outer.y + 22f, outer.width - 16f, outer.height - 30f);
            GUILayout.BeginArea(inner);
            GUILayout.Label("Game 视图：拖青色框移动 / 四角缩放",
                new GUIStyle(GUI.skin.label) { wordWrap = true });

            var d = SocialLayout.EnsureAsset();
            if (d == null)
            {
                GUILayout.EndArea();
                return;
            }

            bool changed = false;
            GUILayout.Label("浏览尺寸");
            float nw = GUILayout.HorizontalSlider(d.width, 220f, 1400f);
            GUILayout.Label($"宽度 {nw:F0}");
            if (!Mathf.Approximately(nw, d.width)) { d.width = nw; changed = true; }

            float nh = GUILayout.HorizontalSlider(d.height, 360f, 2000f);
            GUILayout.Label($"高度 {nh:F0}");
            if (!Mathf.Approximately(nh, d.height)) { d.height = nh; changed = true; }

            float ax = GUILayout.HorizontalSlider(d.anchorX, 0.15f, 0.85f);
            GUILayout.Label($"水平 {ax:F2}");
            if (!Mathf.Approximately(ax, d.anchorX)) { d.anchorX = ax; changed = true; }

            float ay = GUILayout.HorizontalSlider(d.anchorY, 0.25f, 0.95f);
            GUILayout.Label($"垂直 {ay:F2}");
            if (!Mathf.Approximately(ay, d.anchorY)) { d.anchorY = ay; changed = true; }

            float ds = GUILayout.HorizontalSlider(d.detailScale, 0.85f, 1.35f);
            GUILayout.Label($"详情放大 {ds:F2}");
            if (!Mathf.Approximately(ds, d.detailScale)) { d.detailScale = ds; changed = true; }

            GUILayout.Space(4);
            GUILayout.Label(socialZoomOpen ? "正在调放大帖（点图外关闭）" : "放大帖尺寸");
            d.EnsureZoomDefaults();
            float zw = GUILayout.HorizontalSlider(d.zoomWidth, 220f, 1600f);
            GUILayout.Label($"放大宽度 {zw:F0}");
            if (!Mathf.Approximately(zw, d.zoomWidth)) { d.zoomWidth = zw; changed = true; }
            float zh = GUILayout.HorizontalSlider(d.zoomHeight, 360f, 2000f);
            GUILayout.Label($"放大高度 {zh:F0}");
            if (!Mathf.Approximately(zh, d.zoomHeight)) { d.zoomHeight = zh; changed = true; }
            float zax = GUILayout.HorizontalSlider(d.zoomAnchorX, 0.15f, 0.85f);
            GUILayout.Label($"放大水平 {zax:F2}");
            if (!Mathf.Approximately(zax, d.zoomAnchorX)) { d.zoomAnchorX = zax; changed = true; }
            float zay = GUILayout.HorizontalSlider(d.zoomAnchorY, 0.15f, 0.95f);
            GUILayout.Label($"放大垂直 {zay:F2}");
            if (!Mathf.Approximately(zay, d.zoomAnchorY)) { d.zoomAnchorY = zay; changed = true; }
            if (GUILayout.Button(socialZoomOpen ? "关闭放大" : "预览放大"))
            {
                if (socialZoomOpen || IsZoomableSocialPost(socialSpriteKey))
                    SetSocialZoom(!socialZoomOpen);
            }

            if (changed)
            {
                d.Clamp();
                RefreshSocialLayoutFromAsset();
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存 / Save"))
                SocialLayout.SaveCurrent();
            if (GUILayout.Button("默认 / Reset"))
            {
                SocialLayout.ResetToDefaults();
                RefreshSocialLayoutFromAsset();
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(SocialLayout.LastSaveMessage))
            {
                var statusStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
                statusStyle.normal.textColor = SocialLayout.LastSaveOk
                    ? new Color(0.35f, 0.95f, 0.45f)
                    : new Color(1f, 0.45f, 0.35f);
                GUILayout.Label((SocialLayout.LastSaveOk ? "✓ " : "✗ ") + SocialLayout.LastSaveMessage,
                    statusStyle);
            }
            else
            {
                GUILayout.Label("松手或点保存后显示写入结果",
                    new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            }

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("帖子1"))
                DebugPreviewSocialPost("social_post_01_feed");
            if (GUILayout.Button("帖子2"))
                DebugPreviewSocialPost("social_post_02_feed");
            if (GUILayout.Button("帖子3"))
                DebugPreviewSocialPost("social_post_03_feed");
            if (GUILayout.Button("详情"))
                DebugPreviewSocialPost("social_post_03_detail", detail: true);
            GUILayout.EndHorizontal();

            GUILayout.Label(
                $"{d.width:F0}×{d.height:F0}  @({d.anchorX:F2},{d.anchorY:F2})  detail×{d.detailScale:F2}",
                new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            GUILayout.EndArea();
        }

        void DrawSocialPhoneImGui()
        {
            if (SocialEditPanelConsumesMouse() && _socialHotControl == 0)
                return;
            if (socialPhoneRt == null || socialRoot == null || !socialRoot.activeSelf)
                return;

            var d = SocialLayout.EnsureAsset();
            if (d == null) return;

            var phoneRect = SocialPhoneToGuiRect(d);
            DrawSocialOutline(phoneRect);

            int controlId = GUIUtility.GetControlID("SocialPhoneImGui".GetHashCode(), FocusType.Passive);
            var e = Event.current;
            const float handle = 14f;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != 0) break;
                    _socialDragMode = PickSocialDragMode(e.mousePosition, phoneRect, handle);
                    if (_socialDragMode == SocialDragNone) break;
                    _socialDragStartMouse = e.mousePosition;
                    if (socialZoomOpen)
                        d.EnsureZoomDefaults();
                    _socialDragW = socialZoomOpen ? d.zoomWidth : d.width;
                    _socialDragH = socialZoomOpen ? d.zoomHeight : d.height;
                    _socialDragAx = socialZoomOpen ? d.zoomAnchorX : d.anchorX;
                    _socialDragAy = socialZoomOpen ? d.zoomAnchorY : d.anchorY;
                    _socialHotControl = controlId;
                    GUIUtility.hotControl = controlId;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId) break;
                    ApplySocialImGuiDrag(d, e.mousePosition - _socialDragStartMouse);
                    RefreshSocialLayoutFromAsset();
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId) break;
                    _socialHotControl = 0;
                    _socialDragMode = SocialDragNone;
                    GUIUtility.hotControl = 0;
                    d.Clamp();
                    SocialLayout.SaveCurrent();
                    RefreshSocialLayoutFromAsset();
                    e.Use();
                    break;

                case EventType.Repaint:
                    var label = _socialHotControl != 0 ? "拖动中…" : "手机框 · 拖中间移动 · 拖角缩放";
                    var style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                    GUI.color = new Color(0.2f, 0.85f, 0.95f, 1f);
                    GUI.Label(new Rect(phoneRect.x, phoneRect.y - 20f, phoneRect.width, 20f), label, style);
                    GUI.color = Color.white;
                    break;
            }
        }

        static Rect SocialPhoneToGuiRect(SocialLayoutData d)
        {
            var ui = Instance;
            if (ui != null && ui.socialPhoneRt != null && ui.socialRoot != null && ui.socialRoot.activeSelf)
            {
                var corners = new Vector3[4];
                ui.socialPhoneRt.GetWorldCorners(corners);
                // 0=bl, 1=tl, 2=tr, 3=br in world; convert to screen then GUI
                var cam = ui.canvasRt != null ? ui.canvasRt.GetComponentInParent<Canvas>()?.worldCamera : null;
                Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                float x = bl.x;
                float y = Screen.height - tr.y;
                float w = tr.x - bl.x;
                float h = tr.y - bl.y;
                return new Rect(x, y, w, h);
            }

            float sw = Screen.width;
            float sh = Screen.height;
            float sx = sw / 1920f;
            float sy = sh / 1080f;
            float pw = d.width * sx;
            float ph = d.height * sy;
            float cx = d.anchorX * sw;
            float cy = sh - d.anchorY * sh;
            return new Rect(cx - pw * 0.5f, cy - ph * 0.5f, pw, ph);
        }

        static void DrawSocialOutline(Rect r)
        {
            var c = new Color(0.15f, 0.9f, 1f, 0.95f);
            DrawSocialEdge(new Rect(r.x, r.y, r.width, 2f), c);
            DrawSocialEdge(new Rect(r.x, r.yMax - 2f, r.width, 2f), c);
            DrawSocialEdge(new Rect(r.x, r.y, 2f, r.height), c);
            DrawSocialEdge(new Rect(r.xMax - 2f, r.y, 2f, r.height), c);
            const float h = 12f;
            DrawSocialEdge(new Rect(r.x - 1f, r.y - 1f, h, h), c);
            DrawSocialEdge(new Rect(r.xMax - h + 1f, r.y - 1f, h, h), c);
            DrawSocialEdge(new Rect(r.x - 1f, r.yMax - h + 1f, h, h), c);
            DrawSocialEdge(new Rect(r.xMax - h + 1f, r.yMax - h + 1f, h, h), c);
        }

        static void DrawSocialEdge(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        static int PickSocialDragMode(Vector2 mouse, Rect r, float handle)
        {
            var bl = new Rect(r.x - 2f, r.yMax - handle, handle, handle);
            var br = new Rect(r.xMax - handle, r.yMax - handle, handle, handle);
            var tl = new Rect(r.x - 2f, r.y - 2f, handle, handle);
            var tr = new Rect(r.xMax - handle, r.y - 2f, handle, handle);
            if (bl.Contains(mouse)) return SocialDragBL;
            if (br.Contains(mouse)) return SocialDragBR;
            if (tl.Contains(mouse)) return SocialDragTL;
            if (tr.Contains(mouse)) return SocialDragTR;
            if (r.Contains(mouse)) return SocialDragMove;
            return SocialDragNone;
        }

        void ApplySocialImGuiDrag(SocialLayoutData d, Vector2 deltaGui)
        {
            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float scale = 1f;
            if (canvasRt != null)
            {
                var canvas = canvasRt.GetComponent<Canvas>();
                if (canvas != null && canvas.scaleFactor > 0.01f)
                    scale = canvas.scaleFactor;
            }

            float w = socialZoomOpen ? d.zoomWidth : d.width;
            float h = socialZoomOpen ? d.zoomHeight : d.height;
            float ax = socialZoomOpen ? d.zoomAnchorX : d.anchorX;
            float ay = socialZoomOpen ? d.zoomAnchorY : d.anchorY;
            float maxW = socialZoomOpen ? 1600f : 1400f;
            switch (_socialDragMode)
            {
                case SocialDragMove:
                    ax = Mathf.Clamp01(_socialDragAx + deltaGui.x / sw);
                    ay = Mathf.Clamp01(_socialDragAy - deltaGui.y / sh);
                    break;
                case SocialDragBR:
                    w = Mathf.Clamp(_socialDragW + deltaGui.x / scale, 220f, maxW);
                    h = Mathf.Clamp(_socialDragH + deltaGui.y / scale, 360f, 2000f);
                    break;
                case SocialDragBL:
                    w = Mathf.Clamp(_socialDragW - deltaGui.x / scale, 220f, maxW);
                    h = Mathf.Clamp(_socialDragH + deltaGui.y / scale, 360f, 2000f);
                    break;
                case SocialDragTR:
                    w = Mathf.Clamp(_socialDragW + deltaGui.x / scale, 220f, maxW);
                    h = Mathf.Clamp(_socialDragH - deltaGui.y / scale, 360f, 2000f);
                    break;
                case SocialDragTL:
                    w = Mathf.Clamp(_socialDragW - deltaGui.x / scale, 220f, maxW);
                    h = Mathf.Clamp(_socialDragH - deltaGui.y / scale, 360f, 2000f);
                    break;
            }
            if (socialZoomOpen)
            {
                d.zoomWidth = w;
                d.zoomHeight = h;
                d.zoomAnchorX = ax;
                d.zoomAnchorY = ay;
            }
            else
            {
                d.width = w;
                d.height = h;
                d.anchorX = ax;
                d.anchorY = ay;
            }
            d.Clamp();
        }
    }
}
#endif
