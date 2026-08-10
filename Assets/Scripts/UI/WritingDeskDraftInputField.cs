using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Legacy <see cref="InputField"/> steals drag (text selection) from a parent
    /// <see cref="ScrollRect"/>. Forward drag to ScrollRect so the draft pans.
    /// Wheel is also forwarded when the pointer is over the field (IScrollHandler).
    /// Do not call base drag/scroll — InputField would offset the text rect and fight ScrollRect.
    /// </summary>
    public class WritingDeskDraftInputField : InputField,
        IScrollHandler,
        IInitializePotentialDragHandler
    {
        public ScrollRect scrollRect;
        public float textTopPad = 8f;

        public void OnScroll(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnScroll(eventData);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnEndDrag(eventData);
        }

        /// <summary>
        /// Must call <see cref="InputField.LateUpdate"/> — click-to-focus sets
        /// m_ShouldActivateNextUpdate and only base LateUpdate activates editing/caret.
        /// Then pin text Y so multiline caret-follow does not fight ScrollRect.
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (textComponent == null) return;
            var tr = textComponent.rectTransform;
            float y = -textTopPad;
            if (Mathf.Abs(tr.anchoredPosition.y - y) > 0.1f)
                tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, y);
        }
    }
}
