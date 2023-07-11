using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Wargon.Ecsape;
using Object = UnityEngine.Object;

namespace Wargon.UI {
    
    public interface IUIElement {
        public static int TypesCount;
        int TypeIndex { get; set; }
        GameObject GameObject { get; }
        Transform Transform { get; }
        int RootIndex { get; }
        bool IsActive { get; }
        bool IsAnimating { get; set; }
        void Hide() {
            GameObject.SetActive(false);
        }
        
        internal void Create() {
            DI.GetOrCreateContainer().Build(this);
            InitProperties();
            OnCreate();
        }

        void InitProperties();
        void OnCreate();
        void OnShow();
        IUIElement SetPosition(Vector3 position);
        void SetActive(bool value);
        void PlayShowAnimation(Action callback = null);
        void PlayHideAnimation(Action callback = null);
    }

    public interface IUIDraggable : IDragHandler, IBeginDragHandler, IEndDragHandler {
        Canvas Canvas { get; set; }
        CanvasGroup CanvasGroup { get; set; }
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

        void IUIElement.InitProperties() {
            _gameObject = gameObject;
            _transform = transform;
        }
        public virtual void OnCreate() {
            
        }
        /// <summary>
        /// Can be overrided without base
        /// </summary>
        public virtual void OnShow() {
            
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
        public static void Create() {
            var type = typeof(T);
            IsMenu = typeof(Menu).IsAssignableFrom(type);
            IsPopup = typeof(Popup).IsAssignableFrom(type);
            Index = IUIElement.TypesCount++;
        }
    }
    
    public class UIFactory {
        private readonly Dictionary<Type, UIElement> _elements;
        private readonly Canvas _canvas;
        private readonly CanvasGroup _canvasGroup;
        public UIFactory(UIElementsList uiConfig, Canvas canvas, CanvasGroup canvasGroup) {
            _elements = new ();
            foreach (var uiElement in uiConfig.elements) {
                _elements.Add(uiElement.GetType(), uiElement);
            }

            _canvas = canvas;
            _canvasGroup = canvasGroup;
        }

        public T Create<T>(IDependencyContainer container) where T : IUIElement {
            var type = typeof(T);
            if (!_elements.ContainsKey(type)) {
                throw new KeyNotFoundException($"There is no UI element of type : '{type}' in UI collection");
            }
            var element = (IUIElement)Object.Instantiate(_elements[type]);
            if (element is IUIDraggable draggable) {
                draggable.Canvas = _canvas;
                draggable.CanvasGroup = _canvasGroup;
            }
            UIElementInfo<T>.Create();
            container.Build(this);
            element.Create();
            element.TypeIndex = UIElementInfo<T>.Index;
            return (T)element;
        }
    }

    public interface IUIService {
        T Get<T>(bool active = true) where T : class, IUIElement;
        T Show<T>(Action onComplite = null, bool hideOther = true) where T : class, IUIElement;
        void Hide<T>(Action onComplite = null) where T : class, IUIElement;
        T Spawn<T>(bool active = true, bool hideOther = true) where T : class, IUIElement;
    }
}
