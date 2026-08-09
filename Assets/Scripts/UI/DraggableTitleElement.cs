#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Play Mode Game-view editor for title menu: drag body to move, corners to resize.
    /// </summary>
    [DisallowMultipleComponent]
    public class DraggableTitleElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        public string ElementId;
        public string Title;
        public RectTransform TargetRect;

        RectTransform _rt;
        Image _img;
        Canvas _canvas;
        RectTransform _layer;
        Mode _mode;
        Vector2 _startMouse;
        Vector2 _startMin;
        Vector2 _startMax;
        Color _idleColor;
        bool _wasEdit;
        bool _ownsHitImage;

        enum Mode { None, Move, ResizeBL, ResizeBR, ResizeTL, ResizeTR }

        void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null)
                _idleColor = _img.color;
            RefreshRefs();
        }

        public void Configure(string id, string title, RectTransform target, Image hitImage, bool ownsHit)
        {
            ElementId = id;
            Title = title;
            TargetRect = target;
            _img = hitImage;
            _ownsHitImage = ownsHit;
            if (_img != null)
                _idleColor = _ownsHitImage ? new Color(1f, 1f, 1f, 0.001f) : _img.color;
            RefreshRefs();
            ApplyEditVisual(TitleMenuEditMode.Enabled);
        }

        void RefreshRefs()
        {
            _rt = TargetRect != null ? TargetRect : GetComponent<RectTransform>();
            if (TargetRect == null)
                TargetRect = _rt;
            _canvas = GetComponentInParent<Canvas>();
            _layer = _rt != null ? _rt.parent as RectTransform : null;
        }

        void OnEnable()
        {
            _wasEdit = false;
            ApplyEditVisual(TitleMenuEditMode.Enabled);
        }

        void Update()
        {
            if (_rt == null) RefreshRefs();
            ApplyEditVisual(TitleMenuEditMode.Enabled);
        }

        void ApplyEditVisual(bool edit)
        {
            if (_img == null) return;
            _img.raycastTarget = true;
            if (edit)
            {
                _img.color = new Color(0.2f, 0.75f, 1f, 0.28f);
                _wasEdit = true;
            }
            else if (_wasEdit)
            {
                _img.color = _ownsHitImage ? new Color(1f, 1f, 1f, 0.001f) : _idleColor;
                _wasEdit = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!TitleMenuEditMode.Enabled) return;
            _mode = PickMode(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!TitleMenuEditMode.Enabled || _rt == null) return;
            if (_mode == Mode.None) _mode = PickMode(eventData);
            _startMouse = eventData.position;
            _startMin = _rt.anchorMin;
            _startMax = _rt.anchorMax;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!TitleMenuEditMode.Enabled || _mode == Mode.None || _layer == null || _rt == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer, eventData.position, eventData.pressEventCamera, out var localNow))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer, _startMouse, eventData.pressEventCamera, out var localStart))
                return;

            var size = _layer.rect.size;
            if (size.x < 1f || size.y < 1f) return;
            var dAnchor = localNow - localStart;
            dAnchor = new Vector2(dAnchor.x / size.x, dAnchor.y / size.y);

            var min = _startMin;
            var max = _startMax;
            switch (_mode)
            {
                case Mode.Move:
                    min += dAnchor;
                    max += dAnchor;
                    break;
                case Mode.ResizeBL:
                    min += dAnchor;
                    break;
                case Mode.ResizeBR:
                    max.x += dAnchor.x;
                    min.y += dAnchor.y;
                    break;
                case Mode.ResizeTL:
                    min.x += dAnchor.x;
                    max.y += dAnchor.y;
                    break;
                case Mode.ResizeTR:
                    max += dAnchor;
                    break;
            }

            var clamped = TitleMenuLayoutData.ClampRect(new Vector4(min.x, min.y, max.x, max.y));
            _rt.anchorMin = new Vector2(clamped.x, clamped.y);
            _rt.anchorMax = new Vector2(clamped.z, clamped.w);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!TitleMenuEditMode.Enabled || _rt == null) return;
            _mode = Mode.None;
            TitleMenuLayout.SaveRectFromTransform(ElementId, _rt);
        }

        Mode PickMode(PointerEventData eventData)
        {
            if (_rt == null) return Mode.Move;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rt, eventData.position, eventData.pressEventCamera, out var local))
                return Mode.Move;

            var r = _rt.rect;
            float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
            const float edge = 0.22f;
            bool left = nx < edge;
            bool right = nx > 1f - edge;
            bool bottom = ny < edge;
            bool top = ny > 1f - edge;
            if (left && bottom) return Mode.ResizeBL;
            if (right && bottom) return Mode.ResizeBR;
            if (left && top) return Mode.ResizeTL;
            if (right && top) return Mode.ResizeTR;
            return Mode.Move;
        }

        void OnGUI()
        {
            if (!TitleMenuEditMode.Enabled || _rt == null) return;
            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            var corners = new Vector3[4];
            _rt.GetWorldCorners(corners);
            var bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            var tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            float x = bl.x;
            float y = Screen.height - tr.y;
            float w = tr.x - bl.x;
            var label = string.IsNullOrEmpty(Title) ? ElementId : Title;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.Label(new Rect(x + 1, y + 1, Mathf.Max(80f, w), 22), label);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, Mathf.Max(80f, w), 22), label);
        }
    }
}
#endif
