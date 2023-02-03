using System;
using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;
using Object = UnityEngine.Object;

namespace Wargon.UI {
    
    public interface IUIElement {
        int TypeIndex { get; set; }
        GameObject GameObject { get; }
        Transform Transform { get; }
        int RootIndex { get; }
        bool IsActive { get; }
        bool IsAnimating { get; set; }
        void Hide() {
            GameObject.SetActive(false);
        }
        
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

    public abstract class UIElement : MonoBehaviour, IUIElement
    {
        private GameObject _gameObject;
        private Transform _transform;
        public Transform Transform => _transform;
        public int TypeIndex { get; set; }
        public GameObject GameObject => _gameObject;

        public bool IsActive => _gameObject.activeInHierarchy;

        public bool IsAnimating { get; set; }
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
        public static int Index;
        private static int Count;
        public static void Create() {
            var type = typeof(T);
            IsMenu = typeof(Menu).IsAssignableFrom(type);
            IsPopup = typeof(Popup).IsAssignableFrom(type);
            Index = Count++;
        }
    }
    
    public class UIFactory {
        private readonly Dictionary<Type, UIElement> _elements;

        public UIFactory(UIElementsList uiConfig) {
            _elements = new ();
            foreach (var uiElement in uiConfig.elements) {
                _elements.Add(uiElement.GetType(), uiElement);
            }
        }

        public T Create<T>() where T : IUIElement {
            var type = typeof(T);
            var element = (IUIElement)Object.Instantiate(_elements[type]);
            UIElementInfo<T>.Create();
            element.Create();
            element.TypeIndex = UIElementInfo<T>.Index;
            return (T)element;
        }
    }

    public interface IUIService {
        T Show<T>(Action onComplite = null) where T : class, IUIElement;
        void Hide<T>(Action onComplite = null) where T : class, IUIElement;
    }
}
