#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>Game-view selection, move, resize, and save controller for arbitrary runtime UI.</summary>
    public sealed class UILayoutEditController : MonoBehaviour
    {
        public static UILayoutEditController Instance { get; private set; }

        const int Move = 1;
        const int ResizeBL = 2;
        const int ResizeBR = 3;
        const int ResizeTL = 4;
        const int ResizeTR = 5;
        const float CornerPixels = 22f;
        const float FullscreenAreaRatio = 0.82f;

        readonly List<RectTransform> _rects = new List<RectTransform>(512);
        readonly Vector3[] _corners = new Vector3[4];
        Canvas _targetCanvas;
        RectTransform _selected;
        GameObject _captureRoot;
        UILayoutPointerSurface _surface;
        bool _panelVisible = true;
        float _nextRefresh;
        int _dragMode;
        Vector2 _dragStartLocal;
        Vector2 _dragStartPosition;
        Vector2 _dragStartSize;
        Camera _dragCamera;
        string _lastDeletedPath;
        RectTransform _lastDeletedTarget;
        Canvas _lastDeletedCanvas;
        bool _dirty;
        string _statusLine = "";
        bool _statusOk = true;

        public bool HasSelection => _selected != null;
        public bool IsDirty => _dirty;
        public bool CanRestoreLastDeleted => !string.IsNullOrEmpty(_lastDeletedPath);
        public string LastDeletedDisplayName => string.IsNullOrEmpty(_lastDeletedPath) ? "无" : _lastDeletedPath;
        public bool SelectionControlledByLayout => IsControlledByLayout(_selected);
        public string SelectedDisplayName => _selected != null
            ? UILayoutOverrides.GetPath(_targetCanvas, _selected)
            : "未选择 / None";
        public string StatusLine => !string.IsNullOrEmpty(_statusLine)
            ? _statusLine
            : UILayoutOverrides.LastOperationMessage;
        public bool StatusOk => string.IsNullOrEmpty(_statusLine)
            ? UILayoutOverrides.LastOperationOk
            : _statusOk;
        public bool SelectionHasSavedOverride
        {
            get
            {
                if (_selected == null || _targetCanvas == null) return false;
                var data = UILayoutOverrides.Asset;
                if (data == null) return false;
                var entry = data.Find(UILayoutOverrides.GetPath(_targetCanvas, _selected));
                return entry != null && !entry.deleted;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindObjectOfType<UILayoutEditController>() != null) return;
            var host = new GameObject("UILayoutEditController");
            host.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(host);
            host.AddComponent<UILayoutEditController>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var enabled = UILayoutEditMode.Enabled && Application.isPlaying;
            EnsureCaptureSurface(enabled);
            if (!enabled) return;

            if (Input.GetKeyDown(KeyCode.F7))
                _panelVisible = !_panelVisible;

            // L = toggle selection lock (ignore when Ctrl/Alt held to avoid OS shortcuts).
            if (Input.GetKeyDown(KeyCode.L) &&
                !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
                !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
            {
                UILayoutEditMode.LockSelection = !UILayoutEditMode.LockSelection;
                SetStatus(true, UILayoutEditMode.LockSelection
                    ? "锁定选中 ON / Lock ON — 点击不会改选"
                    : "锁定选中 OFF / Lock OFF — 可重新点选");
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.15f;
                RefreshTargets();
            }

            if (_selected == null) return;
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                RequestDeleteSelection();
                return;
            }
            var step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 10f : 1f;
            var delta = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) delta.x -= step;
            if (Input.GetKeyDown(KeyCode.RightArrow)) delta.x += step;
            if (Input.GetKeyDown(KeyCode.DownArrow)) delta.y -= step;
            if (Input.GetKeyDown(KeyCode.UpArrow)) delta.y += step;
            if (delta != Vector2.zero)
            {
                _selected.anchoredPosition += delta;
                _dirty = true;
                SaveSelection();
            }
        }

        void EnsureCaptureSurface(bool enabled)
        {
            if (_captureRoot == null)
            {
                _captureRoot = new GameObject("UILayoutEditCapture", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                _captureRoot.hideFlags = HideFlags.HideInHierarchy;
                _captureRoot.transform.SetParent(transform, false);
                var canvas = _captureRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue - 2;

                var capture = new GameObject("Capture", typeof(RectTransform), typeof(Image),
                    typeof(UILayoutPointerSurface));
                capture.transform.SetParent(_captureRoot.transform, false);
                var rect = capture.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = capture.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.001f);
                image.raycastTarget = true;
                _surface = capture.GetComponent<UILayoutPointerSurface>();
                _surface.Owner = this;
            }
            if (_captureRoot.activeSelf != enabled)
                _captureRoot.SetActive(enabled);
        }

        void RefreshTargets()
        {
            if (_selected != null && (!_selected.gameObject.activeInHierarchy ||
                _targetCanvas == null || !_selected.IsChildOf(_targetCanvas.transform)))
            {
                _selected = null;
                _dirty = false;
            }

            if (_targetCanvas != null && _targetCanvas.gameObject.activeInHierarchy)
                PopulateTargets(_targetCanvas);
            else
            {
                _targetCanvas = FindBestCanvas(Input.mousePosition);
                PopulateTargets(_targetCanvas);
            }
        }

        void PopulateTargets(Canvas canvas)
        {
            _rects.Clear();
            if (canvas == null) return;
            canvas.GetComponentsInChildren(false, _rects);
            for (var i = _rects.Count - 1; i >= 0; i--)
            {
                var target = _rects[i];
                if (target == null || target == canvas.transform || target.rect.width < 2f || target.rect.height < 2f)
                    _rects.RemoveAt(i);
            }
        }

        Canvas FindBestCanvas(Vector2 screenPoint)
        {
            var canvases = FindObjectsOfType<Canvas>();
            Canvas best = null;
            var bestOrder = int.MinValue;
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas || canvas.gameObject == _captureRoot) continue;
                if (!canvas.pixelRect.Contains(screenPoint)) continue;
                var order = canvas.sortingOrder;
                if (best == null || order > bestOrder)
                {
                    best = canvas;
                    bestOrder = order;
                }
            }
            return best;
        }

        public void PointerDown(PointerEventData eventData)
        {
            if (!UILayoutEditMode.Enabled) return;

            // Selection lock: keep current pick; only drag if click hits it (or a corner handle).
            if (UILayoutEditMode.LockSelection && _selected != null)
            {
                var onSelected = ContainsScreenPoint(_selected, eventData.position);
                var mode = PickDragMode(_selected, eventData.position);
                // Outside body and not near a corner → ignore (do not re-select).
                if (!onSelected && mode == Move)
                {
                    _dragMode = 0;
                    return;
                }
                BeginDragOnSelected(eventData);
                return;
            }

            var canvas = FindBestCanvas(eventData.position);
            if (canvas != _targetCanvas)
            {
                _targetCanvas = canvas;
                PopulateTargets(canvas);
            }
            var picked = PickBest(eventData.position);
            if (picked == null) return;
            picked = ResolveEditableTarget(picked, true);

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                var parent = picked.parent as RectTransform;
                if (parent != null && parent != _targetCanvas.transform)
                    picked = ResolveEditableTarget(parent, false);
            }

            if (_selected != picked)
                _dirty = false;
            _selected = picked;
            BeginDragOnSelected(eventData);
            UnityEditor.Selection.activeGameObject = picked.gameObject;
        }

        void BeginDragOnSelected(PointerEventData eventData)
        {
            if (_selected == null) return;
            _dragMode = PickDragMode(_selected, eventData.position);
            _dragCamera = eventData.pressEventCamera;
            var parentRect = _selected.parent as RectTransform;
            if (parentRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, _dragCamera, out _dragStartLocal))
            {
                _dragMode = 0;
                return;
            }
            _dragStartPosition = _selected.anchoredPosition;
            _dragStartSize = _selected.rect.size;
        }

        RectTransform ResolveEditableTarget(RectTransform picked, bool releaseFromLayout)
        {
            if (picked == null) return null;

            // Text/icon children visually fill a button and are often the smallest hit.
            // Editing the child would move only the decoration, so select its owning control.
            for (var current = picked; current != null && current != _targetCanvas.transform;
                 current = current.parent as RectTransform)
            {
                if (current.GetComponent<Selectable>() != null)
                {
                    picked = current;
                    break;
                }
            }

            // The dialogue HUD is intentionally a single aligned three-button toolbar.
            // Selecting any of its buttons moves the toolbar as a unit and preserves
            // equal sizing/spacing instead of detaching an individual child.
            var directParent = picked.parent as RectTransform;
            if (directParent != null && directParent.name == "HudActions" &&
                directParent.GetComponent<HorizontalLayoutGroup>() != null)
                return directParent;

            // A LayoutGroup rewrites direct children's positions every layout pass.
            // Detach the selected control through LayoutElement.ignoreLayout so it can be
            // moved independently. This flag is persisted with the layout override.
            if (releaseFromLayout && IsControlledByLayout(picked))
            {
                var layoutElement = picked.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = picked.gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                Canvas.ForceUpdateCanvases();
            }
            return picked;
        }

        public void PointerDrag(PointerEventData eventData)
        {
            if (_selected == null || _dragMode == 0) return;
            var parentRect = _selected.parent as RectTransform;
            if (parentRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, _dragCamera, out var current)) return;
            var delta = current - _dragStartLocal;
            _dirty = true;

            if (_dragMode == Move)
            {
                var next = _dragStartPosition + delta;
                if (UILayoutEditMode.SnapEnabled) next = Snap(next);
                _selected.anchoredPosition = next;
                return;
            }

            var left = _dragMode == ResizeBL || _dragMode == ResizeTL;
            var right = _dragMode == ResizeBR || _dragMode == ResizeTR;
            var bottom = _dragMode == ResizeBL || _dragMode == ResizeBR;
            var top = _dragMode == ResizeTL || _dragMode == ResizeTR;
            var size = _dragStartSize;
            var position = _dragStartPosition;
            if (right)
            {
                size.x = Mathf.Max(8f, _dragStartSize.x + delta.x);
                position.x = _dragStartPosition.x + (size.x - _dragStartSize.x) * _selected.pivot.x;
            }
            else if (left)
            {
                size.x = Mathf.Max(8f, _dragStartSize.x - delta.x);
                position.x = _dragStartPosition.x + (_dragStartSize.x - size.x) * (1f - _selected.pivot.x);
            }
            if (top)
            {
                size.y = Mathf.Max(8f, _dragStartSize.y + delta.y);
                position.y = _dragStartPosition.y + (size.y - _dragStartSize.y) * _selected.pivot.y;
            }
            else if (bottom)
            {
                size.y = Mathf.Max(8f, _dragStartSize.y - delta.y);
                position.y = _dragStartPosition.y + (_dragStartSize.y - size.y) * (1f - _selected.pivot.y);
            }
            if (UILayoutEditMode.SnapEnabled) size = Snap(size);
            _selected.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _selected.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            _selected.anchoredPosition = position;
        }

        public void PointerUp(PointerEventData eventData)
        {
            if (_selected != null && _dragMode != 0) SaveSelection();
            _dragMode = 0;
        }

        public bool SaveSelection()
        {
            if (_selected == null || _targetCanvas == null)
            {
                SetStatus(false, "保存失败 — 无选中 / Save failed — nothing selected");
                return false;
            }
            var ok = UILayoutOverrides.Save(_targetCanvas, _selected);
            _dirty = !ok;
            _statusLine = UILayoutOverrides.LastOperationMessage;
            _statusOk = ok;
            return ok;
        }

        public bool RevertSelection()
        {
            if (_selected == null || _targetCanvas == null)
            {
                SetStatus(false, "还原失败 — 无选中");
                return false;
            }
            var path = UILayoutOverrides.GetPath(_targetCanvas, _selected);
            var data = UILayoutOverrides.Asset;
            var entry = data != null ? data.Find(path) : null;
            if (entry == null || entry.deleted)
            {
                SetStatus(false, "还原失败 — 无已保存条目 / no saved override");
                return false;
            }
            UILayoutOverrides.Apply(_selected, entry);
            _dirty = false;
            SetStatus(true, "已还原到已保存布局 / reverted " + path);
            return true;
        }

        public void RemoveSelectionOverride()
        {
            if (_selected == null || _targetCanvas == null)
            {
                SetStatus(false, "删除布局失败 — 无选中");
                return;
            }
            var ok = UILayoutOverrides.Remove(_targetCanvas, _selected);
            _statusLine = UILayoutOverrides.LastOperationMessage;
            _statusOk = ok;
            _dirty = false;
        }

        public void RequestDeleteSelection()
        {
            if (_selected == null || _targetCanvas == null) return;
            var path = UILayoutOverrides.GetPath(_targetCanvas, _selected);
            var confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "删除 UI 组件",
                "确定隐藏并持久删除「" + _selected.name + "」吗？\n\n" +
                path + "\n\n可以通过布局工具的恢复按钮撤销。",
                "删除", "取消");
            if (!confirmed) return;

            var target = _selected;
            var canvas = _targetCanvas;
            if (!UILayoutOverrides.Delete(canvas, target))
            {
                _statusLine = UILayoutOverrides.LastOperationMessage;
                _statusOk = false;
                return;
            }
            _lastDeletedPath = path;
            _lastDeletedTarget = target;
            _lastDeletedCanvas = canvas;
            _selected = null;
            _dirty = false;
            target.gameObject.SetActive(false);
            PopulateTargets(canvas);
            _statusLine = UILayoutOverrides.LastOperationMessage;
            _statusOk = true;
        }

        public void RestoreLastDeleted()
        {
            if (string.IsNullOrEmpty(_lastDeletedPath)) return;
            var restoredPath = _lastDeletedPath;
            if (!UILayoutOverrides.Restore(restoredPath))
            {
                _statusLine = UILayoutOverrides.LastOperationMessage;
                _statusOk = false;
                return;
            }
            if (_lastDeletedTarget != null)
            {
                _lastDeletedTarget.gameObject.SetActive(true);
                var entry = UILayoutOverrides.Asset.Find(restoredPath);
                UILayoutOverrides.Apply(_lastDeletedTarget, entry);
                _targetCanvas = _lastDeletedCanvas;
                _selected = _lastDeletedTarget;
            }
            _lastDeletedPath = null;
            _lastDeletedTarget = null;
            _lastDeletedCanvas = null;
            _dirty = false;
            RefreshRestoredTargets(new HashSet<string> { restoredPath });
            _statusLine = UILayoutOverrides.LastOperationMessage;
            _statusOk = true;
        }

        public void RestoreAllDeleted()
        {
            var data = UILayoutOverrides.Asset;
            var deletedPaths = new HashSet<string>();
            if (data != null && data.entries != null)
            {
                for (var i = 0; i < data.entries.Count; i++)
                {
                    var entry = data.entries[i];
                    if (entry != null && entry.deleted && !string.IsNullOrEmpty(entry.path))
                        deletedPaths.Add(entry.path);
                }
            }
            if (UILayoutOverrides.RestoreAllDeleted() <= 0)
            {
                SetStatus(false, "没有可恢复的已删除组件");
                return;
            }
            _lastDeletedPath = null;
            _lastDeletedTarget = null;
            _lastDeletedCanvas = null;
            _dirty = false;
            RefreshRestoredTargets(deletedPaths);
            _statusLine = UILayoutOverrides.LastOperationMessage;
            _statusOk = true;
        }

        void SetStatus(bool ok, string message)
        {
            _statusOk = ok;
            _statusLine = message ?? "";
            if (ok) Debug.Log("[UI Layout] " + _statusLine);
            else Debug.LogWarning("[UI Layout] " + _statusLine);
        }

        void RefreshRestoredTargets(HashSet<string> restoredPaths)
        {
            var data = UILayoutOverrides.Asset;
            if (data == null || restoredPaths == null || restoredPaths.Count == 0) return;
            var canvases = FindObjectsOfType<Canvas>(true);
            var allRects = new List<RectTransform>(512);
            for (var c = 0; c < canvases.Length; c++)
            {
                var canvas = canvases[c];
                if (canvas == null || !canvas.isRootCanvas || canvas.gameObject == _captureRoot) continue;
                allRects.Clear();
                canvas.GetComponentsInChildren(true, allRects);
                for (var i = 0; i < allRects.Count; i++)
                {
                    var target = allRects[i];
                    if (target == null || target == canvas.transform) continue;
                    var path = UILayoutOverrides.GetPath(canvas, target);
                    if (!restoredPaths.Contains(path)) continue;
                    var entry = data.Find(path);
                    if (entry == null || entry.deleted) continue;
                    if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
                    UILayoutOverrides.Apply(target, entry);
                }
            }
            RefreshTargets();
        }

        /// <summary>
        /// Prefer the deepest / most specific editable RectTransform under the cursor.
        /// Skips named catchers/dimmers and near-fullscreen overlays when possible.
        /// </summary>
        RectTransform PickBest(Vector2 screenPoint)
        {
            RectTransform best = null;
            var bestScore = float.MinValue;
            var canvasArea = EstimateCanvasScreenArea();

            for (var i = 0; i < _rects.Count; i++)
            {
                var target = _rects[i];
                if (target == null || !ContainsScreenPoint(target, screenPoint)) continue;
                if (IsIgnorablePick(target)) continue;

                var screenRect = GetScreenRect(target);
                var area = Mathf.Abs(screenRect.width * screenRect.height);
                if (area < 4f) continue;

                if (UILayoutEditMode.SkipFullscreenCatchers && canvasArea > 1f &&
                    area >= canvasArea * FullscreenAreaRatio)
                    continue;

                // Smaller + deeper wins. Depth outweighs mild area differences.
                var depth = 0;
                for (var t = target.transform; t != null && t != _targetCanvas.transform; t = t.parent)
                    depth++;
                var score = depth * 100000f - area;
                if (score > bestScore)
                {
                    best = target;
                    bestScore = score;
                }
            }

            // Fallback: allow fullscreen targets if nothing else hit.
            if (best == null)
            {
                var fallbackArea = float.MaxValue;
                var fallbackDepth = -1;
                for (var i = 0; i < _rects.Count; i++)
                {
                    var target = _rects[i];
                    if (target == null || !ContainsScreenPoint(target, screenPoint)) continue;
                    if (IsIgnorablePick(target)) continue;
                    var screenRect = GetScreenRect(target);
                    var area = Mathf.Abs(screenRect.width * screenRect.height);
                    var depth = 0;
                    for (var t = target.transform; t != null && t != _targetCanvas.transform; t = t.parent)
                        depth++;
                    if (area < fallbackArea - 0.1f || (Mathf.Abs(area - fallbackArea) < 0.1f && depth > fallbackDepth))
                    {
                        best = target;
                        fallbackArea = area;
                        fallbackDepth = depth;
                    }
                }
            }
            return best;
        }

        float EstimateCanvasScreenArea()
        {
            if (_targetCanvas == null) return Screen.width * (float)Screen.height;
            var pr = _targetCanvas.pixelRect;
            return Mathf.Max(1f, pr.width * pr.height);
        }

        static bool IsIgnorablePick(RectTransform target)
        {
            if (target == null) return true;
            var n = target.name;
            if (string.IsNullOrEmpty(n)) return false;
            if (UILayoutOverrides.IsProtectedSystemOverlay(n)) return true;
            // Common full-screen input/dim layers that steal the "smallest" or only hit.
            if (ContainsIgnoreToken(n, "Catcher")) return true;
            if (ContainsIgnoreToken(n, "Dimmer")) return true;
            if (n.Equals("Dim", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (ContainsIgnoreToken(n, "Blocker")) return true;
            if (ContainsIgnoreToken(n, "Backdrop")) return true;
            if (ContainsIgnoreToken(n, "ModalBg")) return true;
            if (ContainsIgnoreToken(n, "HitArea") && target.GetComponent<Selectable>() == null) return true;
            return false;
        }

        static bool ContainsIgnoreToken(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int PickDragMode(RectTransform target, Vector2 screenPoint)
        {
            var rect = GetScreenRect(target);
            var left = Mathf.Abs(screenPoint.x - rect.xMin) <= CornerPixels;
            var right = Mathf.Abs(screenPoint.x - rect.xMax) <= CornerPixels;
            var bottom = Mathf.Abs(screenPoint.y - rect.yMin) <= CornerPixels;
            var top = Mathf.Abs(screenPoint.y - rect.yMax) <= CornerPixels;
            if (left && bottom) return ResizeBL;
            if (right && bottom) return ResizeBR;
            if (left && top) return ResizeTL;
            if (right && top) return ResizeTR;
            return Move;
        }

        bool ContainsScreenPoint(RectTransform target, Vector2 point)
        {
            var camera = GetCanvasCamera(_targetCanvas);
            return RectTransformUtility.RectangleContainsScreenPoint(target, point, camera);
        }

        Rect GetScreenRect(RectTransform target)
        {
            target.GetWorldCorners(_corners);
            var camera = GetCanvasCamera(_targetCanvas);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < 4; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, _corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Camera GetCanvasCamera(Canvas canvas)
        {
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }

        static Vector2 Snap(Vector2 value)
        {
            var grid = Mathf.Max(1f, UILayoutEditMode.GridSize);
            return new Vector2(Mathf.Round(value.x / grid) * grid, Mathf.Round(value.y / grid) * grid);
        }

        void OnGUI()
        {
            if (!UILayoutEditMode.Enabled || !Application.isPlaying) return;
            if (_targetCanvas == null) RefreshTargets();

            if (UILayoutEditMode.ShowAllFrames)
            {
                for (var i = 0; i < _rects.Count; i++)
                {
                    var target = _rects[i];
                    if (target != null && target != _selected && !IsIgnorablePick(target))
                        DrawOutline(ToGuiRect(GetScreenRect(target)), new Color(0.15f, 0.75f, 1f, 0.16f), 1f);
                }
            }
            if (_selected != null)
            {
                var rect = ToGuiRect(GetScreenRect(_selected));
                var locked = UILayoutEditMode.LockSelection;
                var outline = locked
                    ? new Color(1f, 0.55f, 0.1f, 1f)
                    : new Color(0.1f, 0.9f, 1f, 1f);
                DrawOutline(rect, outline, locked ? 3f : 2f);
                DrawHandle(rect.xMin, rect.yMin, locked);
                DrawHandle(rect.xMax, rect.yMin, locked);
                DrawHandle(rect.xMin, rect.yMax, locked);
                DrawHandle(rect.xMax, rect.yMax, locked);
                var labelColor = locked ? new Color(1f, 0.7f, 0.25f) : Color.cyan;
                var label = (locked ? "[锁定 LOCK] " : "") + (_dirty ? "* " : "") + _selected.name;
                GUI.Label(new Rect(rect.x + 3f, rect.y + 2f, Mathf.Max(140f, rect.width), 22f),
                    label, new GUIStyle(GUI.skin.label) { normal = { textColor = labelColor } });
            }

            if (_panelVisible)
            {
                var locked = UILayoutEditMode.LockSelection;
                var extra = 0f;
                if (_selected != null) extra += 22f;
                if (SelectionControlledByLayout) extra += 22f;
                if (!string.IsNullOrEmpty(StatusLine)) extra += 36f;
                var panel = new Rect(12f, 12f, 380f, 118f + extra);
                GUI.Box(panel, locked ? "通用 UI 布局 · 已锁定 (F7 隐藏)" : "通用 UI 布局 (F7 隐藏)");
                GUI.Label(new Rect(22f, 36f, 360f, 20f),
                    "点击选择 · 拖动移动 · 四角缩放 · Alt 选父级");
                GUI.Label(new Rect(22f, 54f, 360f, 20f),
                    "松手自动保存 · 方向键微调 · Delete 删除 · L 锁定选中");

                var y = 76f;
                var lockNext = GUI.Toggle(new Rect(22f, y, 360f, 20f), locked,
                    locked ? "锁定选中 / Lock selection  (ON · 按 L 解锁)" : "锁定选中 / Lock selection  (按 L)");
                if (lockNext != locked)
                    UILayoutEditMode.LockSelection = lockNext;
                y += 22f;

                if (_selected != null)
                {
                    var dirtyMark = _dirty ? " *" : "";
                    GUI.Label(new Rect(22f, y, 360f, 20f), "当前：" + _selected.name + dirtyMark);
                    y += 22f;
                    if (SelectionControlledByLayout)
                    {
                        var warning = new GUIStyle(GUI.skin.label);
                        warning.normal.textColor = new Color(1f, 0.72f, 0.2f);
                        GUI.Label(new Rect(22f, y, 360f, 20f), "父级 LayoutGroup 会接管位置，请 Alt+点击选择父级", warning);
                        y += 22f;
                    }
                }

                if (!string.IsNullOrEmpty(StatusLine))
                {
                    var statusStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
                    statusStyle.normal.textColor = StatusOk
                        ? new Color(0.35f, 0.95f, 0.45f)
                        : new Color(1f, 0.45f, 0.35f);
                    GUI.Label(new Rect(22f, y, 360f, 36f), StatusLine, statusStyle);
                }
            }
        }

        static bool IsControlledByLayout(RectTransform target)
        {
            if (target == null || target.parent == null) return false;
            var layout = target.parent.GetComponent<LayoutGroup>();
            if (layout == null || !layout.enabled) return false;
            var element = target.GetComponent<LayoutElement>();
            return element == null || !element.ignoreLayout;
        }

        static Rect ToGuiRect(Rect screenRect)
        {
            return new Rect(screenRect.x, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
        }

        static void DrawOutline(Rect rect, Color color, float thickness)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void DrawHandle(float x, float y, bool locked)
        {
            GUI.color = locked ? new Color(1f, 0.55f, 0.15f) : Color.cyan;
            GUI.DrawTexture(new Rect(x - 5f, y - 5f, 10f, 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    public sealed class UILayoutPointerSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public UILayoutEditController Owner;
        public void OnPointerDown(PointerEventData eventData) => Owner?.PointerDown(eventData);
        public void OnDrag(PointerEventData eventData) => Owner?.PointerDrag(eventData);
        public void OnPointerUp(PointerEventData eventData) => Owner?.PointerUp(eventData);
    }
}
#endif
