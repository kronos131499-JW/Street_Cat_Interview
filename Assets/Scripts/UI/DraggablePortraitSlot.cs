#if UNITY_EDITOR
// Legacy placeholder — portrait slot editing moved to IMGUI in GameUI.PortraitEdit.cs
namespace StreetCat.UI
{
    public static class DraggablePortraitSlot
    {
        public static bool IsDragging => GameUI.PortraitSlotIsDragging;
    }
}
#endif
