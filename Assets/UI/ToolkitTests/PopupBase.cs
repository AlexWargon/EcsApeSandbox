using UnityEngine.UIElements;

public class PopupBase : VisualElement
{
    [UnityEngine.Scripting.Preserve]
    public new class UxmlFactory : UxmlFactory<PopupBase> { }

    public PopupBase() {
        
    }
}
