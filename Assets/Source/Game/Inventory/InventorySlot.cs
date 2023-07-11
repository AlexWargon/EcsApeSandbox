using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wargon.Ecsape;

namespace Rogue {
    public class InventorySlot : MonoBehaviour, IPointerClickHandler {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
        [SerializeField] private ItemData _itemData;
        public int Index;
        public Action OnAddItem;
        public Action<InventorySlot> OnClick, OnRemove;
        public ref ItemData Data => ref _itemData;
        public bool IsEmpty { get; private set; } = true;

        public void OnPointerClick(PointerEventData eventData) {
            if (IsEmpty) return;
            //Debug.Log("CLICK");
            OnClick?.Invoke(this);
        }

        public void SetItem(ItemData data) {
            if (!IsEmpty) return;
            _itemData = data;
            _icon.sprite = data.Entity.Get<Equipment>().icon;
            _icon.gameObject.SetActive(true);
            IsEmpty = false;
        }

        public void RemoveItem() {
            if (IsEmpty) return;
            Deselect();
            _icon.gameObject.SetActive(false);
            _icon.sprite = null;
            _itemData = default;
            OnRemove?.Invoke(this);
            IsEmpty = true;
        }

        public void Deselect() {
            _border.gameObject.SetActive(false);
        }

        public void Select() {
            _border.gameObject.SetActive(true);
        }
    }

    [Serializable]
    public class ItemData {
        public EquipmentType ItemType;
        public int SlotID;
        public int ID;
        public Entity Entity;
        public List<ItemData> Childs;
        public int MaxChilds;
    }
    [Serializable]
    public class EquipEventData {
        public int SlotIndex;
        public EquipmentType EquipmentType;
        public Entity Entity;
    }
    [Serializable]
    public class ItemConfig {
        public int ID;
        public EntityLink Prefab;
    }
}