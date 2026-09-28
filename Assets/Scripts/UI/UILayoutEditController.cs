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

        public bool HasSelection => _selected != null;
        public bool CanRestoreLastDeleted => !string.IsNullOrEmpty(_lastDeletedPath);
        public string LastDeletedDisplayName => string.IsNullOrEmpty(_lastDeletedPath) ? "无" : _lastDeletedPath;
        public bool SelectionControlledByLayout => IsControlledByLayout(_selected);
        public string SelectedDisplayName => _selected != null
            ? UILayoutOverrides.GetPath(_targetCanvas, _selected)
            : "未选择";

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
                _selected = null;

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
            var canvas = FindBestCanvas(eventData.position);
            if (canvas != _targetCanvas)
            {
                _targetCanvas = canvas;
                PopulateTargets(canvas);
            }
            var picked = PickSmallest(eventData.position);
            if (picked == null) return;
            picked = ResolveEditableTarget(picked, true);

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                var parent = picked.parent as RectTransform;
                if (parent != null && parent != _targetCanvas.transform)
                    picked = ResolveEditableTarget(parent, false);
            }
            _selected = picked;
            _dragMode = PickDragMode(picked, eventData.position);
            _dragCamera = eventData.pressEventCamera;
            var parentRect = picked.parent as RectTransform;
            if (parentRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, _dragCamera, out _dragStartLocal))
            {
                _dragMode = 0;
                return;
            }
            _dragStartPosition = picked.anchoredPosition;
            _dragStartSize = picked.rect.size;
            UnityEditor.Selection.activeGameObject = picked.gameObject;
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

        public void SaveSelection()
        {
            if (_selected == null || _targetCanvas == null) return;
            UILayoutOverrides.Save(_targetCanvas, _selected);
        }

        public void RemoveSelectionOverride()
        {
            if (_selected == null || _targetCanvas == null) return;
            UILayoutOverrides.Remove(_targetCanvas, _selected);
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
            if (!UILayoutOverrides.Delete(canvas, target)) return;
            _lastDeletedPath = path;
            _lastDeletedTarget = target;
            _lastDeletedCanvas = canvas;
            _selected = null;
            target.gameObject.SetActive(false);
            PopulateTargets(canvas);
        }

        public void RestoreLastDeleted()
        {
            if (string.IsNullOrEmpty(_lastDeletedPath)) return;
            var restoredPath = _lastDeletedPath;
            if (!UILayoutOverrides.Restore(restoredPath)) return;
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
            RefreshRestoredTargets(new HashSet<string> { restoredPath });
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
            if (UILayoutOverrides.RestoreAllDeleted() <= 0) return;
            _lastDeletedPath = null;
            _lastDeletedTarget = null;
            _lastDeletedCanvas = null;
            RefreshRestoredTargets(deletedPaths);
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

        RectTransform PickSmallest(Vector2 screenPoint)
        {
            RectTransform result = null;
            var bestArea = float.MaxValue;
            var bestSibling = -1;
            for (var i = 0; i < _rects.Count; i++)
            {
                var target = _rects[i];
                if (!ContainsScreenPoint(target, screenPoint)) continue;
                var screenRect = GetScreenRect(target);
                var area = Mathf.Abs(screenRect.width * screenRect.height);
                var sibling = target.GetSiblingIndex();
                if (area < bestArea - 0.1f || (Mathf.Abs(area - bestArea) < 0.1f && sibling > bestSibling))
                {
                    result = target;
                    bestArea = area;
                    bestSibling = sibling;
                }
            }
            return result;
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
                    if (target != null && target != _selected)
                        DrawOutline(ToGuiRect(GetScreenRect(target)), new Color(0.15f, 0.75f, 1f, 0.16f), 1f);
                }
            }
            if (_selected != null)
            {
                var rect = ToGuiRect(GetScreenRect(_selected));
                DrawOutline(rect, new Color(0.1f, 0.9f, 1f, 1f), 2f);
                DrawHandle(rect.xMin, rect.yMin);
                DrawHandle(rect.xMax, rect.yMin);
                DrawHandle(rect.xMin, rect.yMax);
                DrawHandle(rect.xMax, rect.yMax);
                GUI.Label(new Rect(rect.x + 3f, rect.y + 2f, Mathf.Max(100f, rect.width), 22f),
                    _selected.name, new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } });
            }

            if (_panelVisible)
            {
                var panelHeight = _selected == null ? 82f : (SelectionControlledByLayout ? 134f : 112f);
                var panel = new Rect(12f, 12f, 350f, panelHeight);
                GUI.Box(panel, "通用 UI 布局 (F7 隐藏)");
                GUI.Label(new Rect(22f, 38f, 330f, 22f), "点击选择 · 拖动移动 · 四角缩放 · Alt 选父级");
                GUI.Label(new Rect(22f, 60f, 330f, 22f), "松手自动保存 · 方向键微调 · Delete 删除");
                if (_selected != null)
                {
                    GUI.Label(new Rect(22f, 82f, 330f, 22f), "当前：" + _selected.name);
                    if (SelectionControlledByLayout)
                    {
                        var warning = new GUIStyle(GUI.skin.label);
                        warning.normal.textColor = new Color(1f, 0.72f, 0.2f);
                        GUI.Label(new Rect(22f, 102f, 330f, 22f), "父级 LayoutGroup 会接管位置，请 Alt+点击选择父级", warning);
                    }
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

        static void DrawHandle(float x, float y)
        {
            GUI.color = Color.cyan;
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
