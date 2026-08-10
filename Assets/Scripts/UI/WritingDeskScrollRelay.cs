using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Forwards mouse-wheel events from Viewport (or any Graphic) to a ScrollRect.
    /// Needed when the pointer hits the viewport chrome rather than the InputField.
    /// </summary>
    public class WritingDeskScrollRelay : MonoBehaviour, IScrollHandler
    {
        public ScrollRect scrollRect;

        public void OnScroll(PointerEventData eventData)
        {
            if (scrollRect != null && scrollRect.IsActive())
                scrollRect.OnScroll(eventData);
        }
    }
}
