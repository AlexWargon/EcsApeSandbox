using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

        public UIService(UIFactory uiFactory, Transform menuScreensRoot, Transform popupsRoot, CanvasGroup canvasGroup) {
            
            _elements = new ();
            _activePopups = new ();
            _activeMenus = new ();
            
            _uiFactory = uiFactory;
            _menuScreensParent = menuScreensRoot;
            _popupsParent = popupsRoot;
            _canvasGroup = canvasGroup;
        }

        private T Spawn<T>() where T : class, IUIElement
        {
            var element = Get<T>();
            
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

            if (isPopup) {
                if (CurrentPopup != null)
                    CurrentPopup.Hide();
                CurrentPopup = element;
            }

            if (isMenu) {
                CurrentMenuScreen = element;
            }

            return element;
        }
        
        public T Show<T>(Action onComplite = null) where T : class, IUIElement {
            var element = Spawn<T>();
            if (element.IsAnimating)
                return element;
            _canvasGroup.interactable = false;
            var key = typeof(T).Name;
            if (!_elements.ContainsKey(key))
            {
                Debug.Log($"ADDED {typeof(T)}");
                _elements.Add(key, element);
            }
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
            _canvasGroup.interactable = false;
            if (_elements.TryGetValue(key, out var element)) {
                if(element.IsAnimating)
                    return;
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

        private T Get<T>() where T :  class, IUIElement {
            var key = typeof(T).Name;
            if(_elements.ContainsKey(key))
                return (T)_elements[key];
            var newElement = _uiFactory.Create<T>();
            _elements.Add(key, newElement);
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