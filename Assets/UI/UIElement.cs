using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wargon.Ecsape;
using Object = UnityEngine.Object;

namespace Wargon.UI {
    
    public interface IUIElement {
        GameObject GameObject { get; }
        Transform Transform { get; }
        int RootIndex { get; }
        bool IsActive { get; }

        public void Create() {
            DI.GetOrCreateContainer().Build(this);
            OnCreate();
        }
        void OnCreate();
        IUIElement SetPosition(Vector3 position);
        void SetActive(bool value);
        void PlayShowAnimation(Action callback = null);
        void PlayHideAnimation(Action callback = null);
    }

    public abstract class UIElement : MonoBehaviour, IUIElement {
        private GameObject _gameObject;
        private Transform _transform;
        public Transform Transform => _transform;
        public GameObject GameObject => _gameObject;
        public bool IsActive => _gameObject.activeInHierarchy;
        protected IUIService UIService;
        
        public virtual void OnCreate() {
            _gameObject = gameObject;
            _transform = transform;
        }
        
        public IUIElement SetPosition(Vector3 position) {
            _transform.position = position;
            return this;
        }
        public int RootIndex => _transform.GetSiblingIndex();
        public void SetActive(bool value) => _gameObject.SetActive(value);

        public virtual void PlayShowAnimation(Action callback = null) {
            callback?.Invoke();
        }

        public virtual void PlayHideAnimation(Action callback = null) {
            callback?.Invoke();
        }
    }

    internal struct UIElementInfo<T> {
        public static bool IsPopup;
        public static bool IsMenu;

        public static void Create() {
            var type = typeof(T);
            IsMenu = typeof(Menu).IsAssignableFrom(type);
            IsPopup = typeof(Popup).IsAssignableFrom(type);
        }
    }
    
    public class UIFactory {
        private readonly IDictionary<Type, UIElement> _elements;

        public UIFactory(UIElementsList uiConfig) {
            _elements = new Dictionary<Type, UIElement>();
            foreach (var uiElement in uiConfig.elements) {
                _elements.Add(uiElement.GetType(), uiElement);
            }
        }

        public T Create<T>() where T : IUIElement {
            var type = typeof(T);
            var showable = (IUIElement)Object.Instantiate(_elements[type]);
            UIElementInfo<T>.Create();
            showable.Create();
            return (T)showable;
        }
    }

    public interface IUIService {
        T Show<T>(Action onComplite = null) where T : class, IUIElement;
        void Hide<T>(Action onComplite = null) where T : class, IUIElement;
        T Get<T>() where T : class, IUIElement;
    }
    
    public class UIService : IUIService {
        private IUIElement CurrentMenuScreen { get; set; }
        private IUIElement CurrentPopup { get; set; }

        private readonly Transform _menuScreensParent;
        private readonly Transform _popupsParent;
        private readonly CanvasGroup _canvasGroup;
        private readonly Image _fade;
        private readonly UIFactory _uiFactory;
        
        private readonly IDictionary<string, IUIElement> _elements;
        private readonly IList<IUIElement> _activePopups;
        private readonly IList<IUIElement> _activeMenus;

        public UIService(UIFactory uiFactory, Transform menuScreensRoot, Transform popupsRoot, CanvasGroup canvasGroup) {
            
            _elements = new Dictionary<string, IUIElement>();
            _activePopups = new List<IUIElement>();
            _activeMenus = new List<IUIElement>();
            
            _uiFactory = uiFactory;
            _menuScreensParent = menuScreensRoot;
            _popupsParent = popupsRoot;
            _canvasGroup = canvasGroup;
        }

        private T Spawn<T>() where T : class, IUIElement {
            var element = _uiFactory.Create<T>();
                
            element.Transform.SetParent(
                UIElementInfo<T>.IsPopup ? _popupsParent : 
                UIElementInfo<T>.IsMenu ? _menuScreensParent : CurrentMenuScreen.Transform, 
                false);
            element.Transform.SetAsLastSibling();

            if (UIElementInfo<T>.IsPopup) {
                if (CurrentPopup != null)
                    Object.Destroy(CurrentPopup.GameObject);
                CurrentPopup = element;
            }

            if (UIElementInfo<T>.IsMenu) {
                CurrentMenuScreen = element;
            }

            return element;
        }
        
        public T Show<T>(Action onComplite = null) where T : class, IUIElement {
            _canvasGroup.interactable = false;
            var element = Spawn<T>();
            
            var key = typeof(T).Name;
            if (!_elements.ContainsKey(key))
            {
                Debug.Log($"ADDED {typeof(T)}");
                _elements.Add(key, element);
            }
            element.SetActive(true);
            element.PlayShowAnimation(() => {
                _canvasGroup.interactable = true;
                onComplite?.Invoke();
            });
            switch (element, key) {
                case (Popup, _):
                    _activePopups.Add(element);
                    break;
                case (Menu, _):
                    _activeMenus.Add(element);
                    break;
            }

            return element;
        }

        public void Hide<T>(Action onComplite = null) where T : class, IUIElement {
            var key = typeof(T).Name;
            _canvasGroup.interactable = false;
            if (_elements.TryGetValue(key, out var element)) {
                
                element.PlayHideAnimation(() => {
                    _canvasGroup.interactable = true;
                    CurrentPopup = _activePopups.OrderBy(pop => pop.RootIndex).LastOrDefault(pop => pop.IsActive);
                    onComplite?.Invoke();
                    Object.Destroy(element.GameObject);
                });
            }
        }

        public T Get<T>() where T :  class, IUIElement {
            var key = typeof(T).Name;
            if(_elements.ContainsKey(key))
                return (T)_elements[key];
            return _uiFactory.Create<T>();
        }

        public void HideAllPopups() {
            foreach (var popup in _activePopups) {
                popup.SetActive(false);
            }
            CurrentPopup = null;
        }
    }
}
