using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {
    private VisualElement baseRoot;
    private VisualElement root;
    [SerializeField] private string text;
    private void OnEnable() {
        var ui = GetComponent<UIDocument>();
        root = ui.rootVisualElement;
        var popup = new PopupBase();
        var topText = popup.Q<Label>("TopText");
        topText.SetEnabled(false);
        baseRoot.Add(popup);
        root.Add(baseRoot);
    }

    private void OnDisable() {
        root.Remove(baseRoot);
    }
}
