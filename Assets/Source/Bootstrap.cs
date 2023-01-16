﻿using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.UI;

[DefaultExecutionOrder(-25)]
public class Bootstrap : MonoBehaviour {
    public UIRoot _root;
    public AnimationsHolder AnimationsHolder;
    private void Awake() {

        var menuservice = new UIService(
            new UIFactory(_root._elementsList), 
            _root.MenuScreenRoot,
            _root.PopupRoot, 
            _root.CanvasGroup);
        
        DI.Register<IUIService>().From(menuservice);
        DI.Register<AnimationsHolder>().From(AnimationsHolder);
    }
}