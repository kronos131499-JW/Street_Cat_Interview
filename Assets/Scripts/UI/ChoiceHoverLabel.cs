using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StreetCat.UI
{
    /// <summary>
    /// Dialogue choice copy stays ink-colored at rest. Hover and press tint the
    /// bar dark, so the label switches to white for that time only.
    /// </summary>
    public sealed class ChoiceHoverLabel : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        TextMeshProUGUI label;
        Color idle = new Color(42f / 255f, 18f / 255f, 2f / 255f, 1f);
        bool over;
        bool pressed;

        public void Bind(TextMeshProUGUI target, Color idleColor)
        {
            label = target;
            idle = idleColor;
            Apply();
        }

        public void SetIdle(Color idleColor)
        {
            idle = idleColor;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            over = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            over = false;
            pressed = false;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Apply();
        }

        void Apply()
        {
            if (label == null) return;
            label.color = over || pressed ? Color.white : idle;
        }
    }
}
