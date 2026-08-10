using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Legacy <see cref="InputField"/> implements <see cref="IScrollHandler"/> (often
    /// explicitly), so <c>ExecuteEvents.GetEventHandler</c> stops on the field and never
    /// reaches a parent <see cref="ScrollRect"/>. Re-implement the interface here and
    /// forward the wheel to the desk draft ScrollRect instead of trying to override.
    /// </summary>
    public class WritingDeskDraftInputField : InputField, IScrollHandler
    {
        public ScrollRect scrollRect;

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (scrollRect != null)
                scrollRect.OnScroll(eventData);
            // Do not call InputField's scroll — it would offset the text rect and fight ScrollRect.
        }
    }
}
