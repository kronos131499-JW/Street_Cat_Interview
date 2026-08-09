#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.Investigation
{
    /// <summary>
    /// Play Mode Game-view editor: drag body to move, drag corners to resize.
    /// Only active when InvestigateHotspotEditMode.Enabled.
    /// </summary>
    [DisallowMultipleComponent]
    public class DraggableInvestigateHotspot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        public string HotspotId;
        public string Title;

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

        enum Mode { None, Move, ResizeBL, ResizeBR, ResizeTL, ResizeTR }

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _img = GetComponent<Image>();
            _canvas = GetComponentInParent<Canvas>();
            _layer = transform.parent as RectTransform;
            if (_img != null) _idleColor = _img.color;
        }

        void OnEnable()
        {
            _wasEdit = false;
            ApplyEditVisual(InvestigateHotspotEditMode.Enabled);
        }

        void Update()
        {
            ApplyEditVisual(InvestigateHotspotEditMode.Enabled);
        }

        void ApplyEditVisual(bool edit)
        {
            if (_img == null) return;
            if (edit)
            {
                _img.color = new Color(1f, 0.55f, 0.15f, 0.35f);
                _wasEdit = true;
            }
            else if (_wasEdit)
            {
                _img.color = _idleColor;
                _wasEdit = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!InvestigateHotspotEditMode.Enabled) return;
            _mode = PickMode(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!InvestigateHotspotEditMode.Enabled) return;
            if (_mode == Mode.None) _mode = PickMode(eventData);
            _startMouse = eventData.position;
            _startMin = _rt.anchorMin;
            _startMax = _rt.anchorMax;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!InvestigateHotspotEditMode.Enabled || _mode == Mode.None || _layer == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer, eventData.position, eventData.pressEventCamera, out var localNow))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer, _startMouse, eventData.pressEventCamera, out var localStart))
                return;

            var size = _layer.rect.size;
            if (size.x < 1f || size.y < 1f) return;
            var delta = localNow - localStart;
            var dAnchor = new Vector2(delta.x / size.x, delta.y / size.y);

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

            var clamped = InvestigateHotspotLayoutData.ClampRect(new Vector4(min.x, min.y, max.x, max.y));
            _rt.anchorMin = new Vector2(clamped.x, clamped.y);
            _rt.anchorMax = new Vector2(clamped.z, clamped.w);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!InvestigateHotspotEditMode.Enabled) return;
            _mode = Mode.None;
            InvestigateHotspotLayout.SaveRectFromTransform(HotspotId, _rt);
        }

        Mode PickMode(PointerEventData eventData)
        {
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
            if (!InvestigateHotspotEditMode.Enabled || _rt == null) return;
            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            var corners = new Vector3[4];
            _rt.GetWorldCorners(corners);
            var bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            var tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            // GUI y is top-down
            float x = bl.x;
            float y = Screen.height - tr.y;
            float w = tr.x - bl.x;
            float h = tr.y - bl.y;
            var label = string.IsNullOrEmpty(Title) ? HotspotId : Title;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.Label(new Rect(x + 1, y + 1, w, 22), label);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 22), label);
        }
    }
}
#endif
