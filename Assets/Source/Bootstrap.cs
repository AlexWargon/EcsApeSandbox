using System.Collections;
using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.UI;

[DefaultExecutionOrder(ExecutionOrder.Bootstrap)]
public class Bootstrap : MonoBehaviour {
    public UIRoot _root;
    public AnimationsHolder AnimationsHolder;
    private void Awake() {
        World.ENTITIES_CACHE = 256;
        var uiService = new UIService(
            new UIFactory(_root._elementsList), 
            _root.MenuScreenRoot,
            _root.PopupRoot, 
            _root.CanvasGroup);
        
        DI.Register<IUIService>().From(uiService);
        DI.Register<AnimationsHolder>().From(AnimationsHolder);
        DI.Register<IObjectPool>(new GameObjectPool());
        
    }
}

public static class ExecutionOrder {
    public const int Bootstrap = -25;
    public const int EcsMain = -24;
}