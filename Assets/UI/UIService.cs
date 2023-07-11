using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wargon.Ecsape;

namespace Wargon.UI {
    public class UIService : IUIService {
        private IUIElement CurrentMenuScreen { get; set; }
        private IUIElement CurrentPopup { get; set; }

        private readonly Transform _menuScreensParent;
        private readonly Transform _popupsParent;
        private readonly CanvasGroup _canvasGroup;
        private readonly Image _fade;
        private readonly UIFactory _uiFactory;
        
        private readonly Dictionary<string, IUIElement> _elements;
        private readonly List<IUIElement> _activePopups;
        private readonly List<IUIElement> _activeMenus;
        private IDependencyContainer _dependencyContainer;
        public UIService(UIRoot uiRoot) {
            
            _elements = new ();
            _activePopups = new ();
            _activeMenus = new ();
            _uiFactory = new UIFactory(uiRoot._elementsList, uiRoot.Canvas, uiRoot.CanvasGroup);
            _menuScreensParent = uiRoot.MenuScreenRoot;
            _popupsParent = uiRoot.PopupRoot;
            _canvasGroup = uiRoot.CanvasGroup;
        }

        public T Spawn<T>(bool active = true, bool hideOther = true) where T : class, IUIElement
        {
            var element = GetInternal<T>();
            //if(!element.IsActive) element.SetActive(true);
            var isPopup = UIElementInfo<T>.IsPopup;
            var isMenu = UIElementInfo<T>.IsMenu;
            
            var current = isPopup ? CurrentPopup : CurrentMenuScreen;
            
            if(current != null && current.TypeIndex == element.TypeIndex)
                return element;

            element.Transform.SetParent(
                isPopup ? _popupsParent : 
                isMenu ? _menuScreensParent : CurrentMenuScreen.Transform, 
                false);
            element.Transform.SetAsLastSibling();
            //Debug.Log($"{typeof(T).Name} is popop ? {isPopup}");
            if (isPopup) {
                if (CurrentPopup != null && hideOther)
                    CurrentPopup.Hide();
                CurrentPopup = element;
            }
            if (isMenu) {
                CurrentMenuScreen = element;
            }
            element.SetActive(active);
            return element;
        }
        
        public T Show<T>(Action onComplite = null, bool hideOther = true) where T : class, IUIElement {
            var element = Spawn<T>(true, hideOther);
            if (element.IsAnimating)
                return element;
            _canvasGroup.interactable = false;

            element.OnShow();
            element.SetActive(true);
            element.IsAnimating = true;
            element.PlayShowAnimation(() => {
                _canvasGroup.interactable = true;
                element.IsAnimating = false;
                onComplite?.Invoke();
            });
            switch (element) {
                case Popup:
                    _activePopups.Add(element);
                    break;
                case Menu:
                    _activeMenus.Add(element);
                    break;
            }

            return element;
        }

        public void Hide<T>(Action onComplite = null) where T : class, IUIElement {
            var key = typeof(T).Name;
            if (_elements.TryGetValue(key, out var element)) {
                if(element.IsAnimating)
                    return;
                _canvasGroup.interactable = false;
                element.IsAnimating = true;
                element.PlayHideAnimation(() => {
                    _canvasGroup.interactable = true;
                    CurrentPopup = _activePopups.OrderBy(pop => pop.RootIndex).LastOrDefault(pop => pop.IsActive);
                    element.IsAnimating = false;
                    onComplite?.Invoke();
                    element.Hide();
                });
            }
        }

        private T GetInternal<T>() where T :  class, IUIElement {
            var key = typeof(T).Name;
            if(_elements.ContainsKey(key))
                return (T)_elements[key];
            var newElement = _uiFactory.Create<T>(_dependencyContainer);
            _elements.Add(key, newElement);
            return newElement;
        }
        public T Get<T>(bool active = true) where T :  class, IUIElement {
            var key = typeof(T).Name;
            if (_elements.ContainsKey(key)) {
                _elements[key].SetActive(active);
                return (T)_elements[key];
            }
            var newElement = _uiFactory.Create<T>(_dependencyContainer);
            _elements.Add(key, newElement);
            newElement.SetActive(active);
            return newElement;
        }
        public void HideAllPopups() {
            foreach (var popup in _activePopups) {
                popup.Hide();
            }
            CurrentPopup = null;
        }
    }
}