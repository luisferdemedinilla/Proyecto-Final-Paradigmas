using UnityEngine;
using UnityEngine.EventSystems;

namespace Creator
{
    public sealed class CreatorCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
    {
        public CreatorScreen screen;
        public int x, y;
        public UnityEngine.UI.RawImage icon;
        public UnityEngine.UI.Image background;
        public TMPro.TextMeshProUGUI badge;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left || e.button == PointerEventData.InputButton.Right)
                screen.Paint(x, y, e.button == PointerEventData.InputButton.Right);
        }
        public void OnPointerEnter(PointerEventData e)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                screen.Paint(x, y, mouse.rightButton.isPressed);
        }
    }
}
