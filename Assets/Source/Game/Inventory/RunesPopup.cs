using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wargon.UI;

namespace Rogue {
    public class RunesPopup : Popup, IUIDraggable {
        [SerializeField] private Button close;
        [SerializeField] private List<InventorySlot> Slots;

        private InventoryService _inventoryService;
        private RectTransform _rectTransform;
        private Image _background;

        private Color movingColor;
        private Color defaultColor;
        
        public Canvas Canvas { get; set; }
        public CanvasGroup CanvasGroup { get; set; }
        
        public override void OnCreate() {
            close.onClick.AddListener(() => {
                UIService.Hide<RunesPopup>();
            });
            _inventoryService.OnEquipRune += HandleEquipRune;
            _rectTransform = GetComponent<RectTransform>();
            _background = GetComponent<Image>();

            defaultColor = _background.color;
            defaultColor.a = 0.6f;
            movingColor = _background.color;
            movingColor.a = 0.4f;
            
            for (var i = 0; i < Slots.Count; i++) {
                Slots[i].OnClick += HandleClickSlot;
            }
        }

        private void OnDestroy() {
            _inventoryService.OnEquipRune -= HandleEquipRune;
        }
        
        public override void PlayShowAnimation(Action callback = null) {
            gameObject.SetActive(true);
            callback?.Invoke();
        }

        public override void PlayHideAnimation(Action callback = null) {
            SetActive(false);
            callback?.Invoke();
        }

        private void HandleClickSlot(InventorySlot slot) {
            foreach (var inventorySlot in Slots) {
                inventorySlot.Deselect();
            }

            slot.Select();
        }
        public void UpdateView(ItemData item) {

            int index = 0;
            foreach (var inventorySlot in Slots) {
                inventorySlot.RemoveItem();
            }
            for (; index < item.MaxChilds; index++) {
                Slots[index].gameObject.SetActive(true);
            }

            for (; index < Slots.Count; index++) {
                Slots[index].gameObject.SetActive(false);
            }

            index = 0;
            foreach (var itemChild in item.Childs) {
                Slots[index].SetItem(itemChild);
                index++;
            }
        }

        private void HandleEquipRune(ItemData item) {
            foreach (var inventorySlot in Slots) {
                inventorySlot.RemoveItem();
            }
            var index = 0;
            foreach (var itemChild in item.Childs) {
                Slots[index].SetItem(itemChild);
                index++;
            }
        }

        public void OnDrag(PointerEventData eventData) {
            _rectTransform.anchoredPosition += eventData.delta / Canvas.scaleFactor;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            //CanvasGroup.blocksRaycasts = false;
            _background.color = movingColor;
        }

        public void OnEndDrag(PointerEventData eventData) {
            //CanvasGroup.blocksRaycasts = true;
            _background.color = defaultColor;
        }

    }
}