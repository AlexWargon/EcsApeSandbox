using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wargon.Ecsape;
using Wargon.UI;

namespace Rogue {
    public class InventoryPopup : Popup {
        [SerializeField] private Button _close, equip, drop;
        public InventorySlot Helm, BodyArmor, Boots, WeaponLeft, WeaponRight, Glows;
        [SerializeField] private List<InventorySlot> bagSlots;
        [SerializeField] private List<InventorySlot> runeSlots;
        private Dictionary<EquipmentType, InventorySlot> _activeSlots;
        private InventorySlot _currentSelected;
        private InventoryService _inventoryService;
        private Entity _player;
        public List<InventorySlot> BagSlots => bagSlots;
        private IPauseSerivce _pauseSerivce;
        public override void OnCreate() {
            
            _activeSlots = new Dictionary<EquipmentType, InventorySlot> {
                { EquipmentType.Helm, Helm },
                { EquipmentType.BodyArmor, BodyArmor },
                { EquipmentType.Boots, Boots },
                { EquipmentType.WeaponLeft, WeaponLeft },
                { EquipmentType.WeaponRight, WeaponRight },
                { EquipmentType.Glows, Glows }
            };

            _close.onClick.AddListener(() => {
                UIService.Hide<InventoryPopup>();
                _pauseSerivce.Unpause();
                
            });
            var slotID = 0;
            foreach (var inventorySlot in bagSlots) {
                inventorySlot.OnClick += HandleOnSelectItem;
                inventorySlot.Index = slotID++;
            }

            foreach (var inventorySlot in _activeSlots) inventorySlot.Value.OnClick += HandleOnSelectItem;

            foreach (var inventorySlot in runeSlots) inventorySlot.OnClick += HandleOnSelectItem;
            
            drop.onClick.AddListener(() => { DropHandle(_currentSelected); });
                
            equip.onClick.AddListener(() => { EquipHandle(_currentSelected); });
            
            _inventoryService.OnEquipRune += HandleEquipRune;
        }
        private void OnDestroy() {
            _inventoryService.OnEquipRune -= HandleEquipRune;
        }
        public override void OnShow() {
            UpdateView();
            _pauseSerivce.Pause();
        }

        private void UpdateView() {
            var inventory = _inventoryService.Inventory;
            foreach (var activeSlotsValue in _activeSlots.Values) activeSlotsValue.Deselect();
            foreach (var activeSlotsValue in _activeSlots.Values) activeSlotsValue.RemoveItem();
            foreach (var inventorySlot in bagSlots) inventorySlot.RemoveItem();
            foreach (var inventoryActiveSlot in inventory.ActiveSlots) {
                var type = inventoryActiveSlot.Key;
                if (_activeSlots.ContainsKey(type)) {
                    var slot = _activeSlots[type];
                    slot.SetItem(inventoryActiveSlot.Value);
                }
            }

            foreach (var bagSlot in inventory.Bag) {
                var inventoryBagSlot = bagSlot.Value;
                var id = bagSlot.Key;
                var bagSlotView = bagSlots[id];
                bagSlotView.SetItem(inventoryBagSlot);
            }
        }

        private void HandleOnSelectItem(InventorySlot slot) {
            foreach (var activeSlotsValue in _activeSlots.Values) activeSlotsValue.Deselect();
            runeSlots.ClearSlots();
            bagSlots.DeselectAll();
            
            slot.Select();
            _currentSelected = slot;
            
            if (slot.Data.ItemType != EquipmentType.Rune && slot.Data.ItemType != EquipmentType.None) {
                UpdateRunesView(slot.Data);
                _inventoryService.SetLastSelected(slot.Data);
            }
        }

        private void UpdateRunesView(ItemData itemData) {
            for (var index = 0; index < runeSlots.Count; index++) {
                runeSlots[index].gameObject.SetActive(false);
            }
            for (var index = 0; index < itemData.MaxChilds; index++) {
                runeSlots[index].gameObject.SetActive(true);
            }
            for (var i = 0; i < itemData.Childs.Count; i++) {
                runeSlots[i].SetItem(itemData.Childs[i]);
            }
        }
        private void HandleEquipRune(ItemData item) {
            foreach (var inventorySlot in runeSlots) {
                inventorySlot.RemoveItem();
            }
            var index = 0;
            foreach (var itemChild in item.Childs) {
                runeSlots[index].SetItem(itemChild);
                index++;
            }
        }
        
        private void DropHandle(InventorySlot slot) {
            if (slot is null) return;
            _inventoryService.RemoveItem(slot);
        }

        private void EquipHandle(InventorySlot slot) {
            if (slot is null) return;
            var data = slot.Data;
            if (data != null && data.ItemType != EquipmentType.Rune) {
                _inventoryService.EquipItem(slot);
            }
            else {
                _inventoryService.AddRuneToItem(slot.Data);
            }
            UpdateView();
        }
    }

    [Serializable]
    public class Slot {
        public ItemData Data;
        public int Index;
    }



    [Serializable]
    public class ComponentListSave {
        public ComponentSave[] Components;

        public override string ToString() {
            var stringBuiled = new StringBuilder();
            foreach (var component in Components) stringBuiled.Append(component.Value);

            return stringBuiled.ToString();
        }
    }

    [Serializable]
    public class ComponentSave {
        public int TypeIndex;
        public string TypeName;
        public string FullTypeName;
        public object Value;
    }

    public class ComponentFieldInfo {
        public string Name;
    }

    public class ComponentInfo {
        public int FildsCount;
        public string Name;
    }

    [Serializable]
    public struct Equipment : IComponent {
        public int id;
        public bool active;
        public byte maxRunesCount;
        public EquipmentType type;
        public Sprite icon;
        public List<Entity> runes;
    }

    [Serializable]
    public struct Rune : IComponent {
        public RuneType Type;
    }

    public struct CastOnAttack : IComponent { }


    public enum RuneType {
        Active,
        Passive
    }

    [Serializable]
    public struct EquipmentList : IComponent, IOnAddToEntity {
        public List<Entity> value;

        public void OnCreate() {
            value ??= new List<Entity>();
        }
    }

    public enum EquipmentType {
        None,
        Helm,
        BodyArmor,
        Glows,
        Boots,
        WeaponRight,
        WeaponLeft,
        Rune
    }

    public static class InventoryListExtension {
        public static void DeselectAll(this List<InventorySlot> slots) {
            for (var i = 0; i < slots.Count; i++) {
                slots[i].Deselect();
            }
        }
        public static void ClearSlots(this List<InventorySlot> slots) {
            for (var i = 0; i < slots.Count; i++) {
                slots[i].Deselect();
                slots[i].RemoveItem();
            }
        }
    }
}