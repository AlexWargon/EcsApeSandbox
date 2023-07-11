using System;
using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;
using Wargon.UI;

namespace Rogue {
    public class InventoryService {
        private Entity Player;
        private IUIService uiService;
        public Inventory Inventory { get; private set; }
        
        public ItemsCollectionsConfig ItemsConfig { get; }
        private ItemData lastSelected;
        public event Action<ItemData> OnEquipRune, OnEquipItem; 
        public InventoryService Construct() {
            Inventory = new Inventory();
            return this;
        }

        public void Clear() {
            Inventory.Clear();
            Player.Get<EquipmentList>().value.Clear();
        }

        private Transform _bag;

        private Transform BagParrent {
            get {
                if (_bag == null) {
                    _bag = new GameObject("Bag").transform;
                }
                return _bag;
            }
        }
        public void SetLastSelected(ItemData item) => lastSelected = item;

        public void SetNewInventory(Inventory inventory) {
            Inventory = inventory;
            
            var loots = DI.Get<LootServise>();

            foreach (var bagItem in Inventory.Bag.Values) {
                ref var newItem = ref loots.SpawnItem(bagItem.ID, Vector3.zero);
                newItem.SetOwner(Player);
                bagItem.Entity = newItem;
                bagItem.Entity.Get<ViewGO>().GameObject.SetActive(false);
                bagItem.Entity.Get<TransformReference>().value.SetParent(BagParrent);
                ref var equipment = ref bagItem.Entity.Get<Equipment>();
                foreach (var bagValueChild in bagItem.Childs) {
                    ref var newChild = ref loots.SpawnItem(bagValueChild.ID, Vector3.zero);
                    newChild.SetOwner(Player);
                    bagValueChild.Entity = newChild;
                    bagValueChild.Entity.Get<ViewGO>().GameObject.SetActive(false);
                    equipment.runes.Add(newChild);
                    newChild.Add(new EquipedTag());
                }
            }

            ref var equipmentList = ref Player.Get<EquipmentList>();
            foreach (var activeSlotItem in Inventory.ActiveSlots.Values) {
                ref var newItem = ref loots.SpawnItem(activeSlotItem.ID, Vector3.zero);
                newItem.SetOwner(Player);
                activeSlotItem.Entity = newItem;
                activeSlotItem.Entity.Get<ViewGO>().GameObject.SetActive(false);
                equipmentList.value.Add(newItem);
                activeSlotItem.Entity.Get<TransformReference>().value.SetParent(BagParrent);
                ref var equipment = ref activeSlotItem.Entity.Get<Equipment>();
                ref var trasform = ref activeSlotItem.Entity.Get<TransformReference>().value;
                foreach (var activeItemChild in activeSlotItem.Childs) {
                    ref var newChild = ref loots.SpawnItem(activeItemChild.ID, Vector3.zero);
                    newChild.SetOwner(Player);
                    activeItemChild.Entity = newChild;
                    activeItemChild.Entity.Get<ViewGO>().GameObject.SetActive(false);
                    equipment.runes.Add(newChild);
                    newChild.Add(new EquipedTag());
                    newChild.Get<TransformReference>().value.SetParent(trasform);
                }
            }
        }
        public void SetPlayer(Entity p) {
            Player = p;
        }

        private int FindLastEmptySlot() {
            var slot = 0;
            while (Inventory.Bag.ContainsKey(slot)) slot++;
            return slot;
        }

        private ItemData CreateItemData(ref Entity entity, int slotID) {
            ref var equipment = ref entity.Get<Equipment>();
            var childs = new List<ItemData>();
            for (var i = 0; i < equipment.runes.Count; i++) {
                ref var rune = ref equipment.runes[i].Get<Equipment>();
                childs.Add(new ItemData {
                    ID = rune.id,
                    MaxChilds = rune.maxRunesCount,
                    ItemType = rune.type,
                    Entity = equipment.runes[i]
                });
            }

            return new ItemData {
                ID = equipment.id,
                MaxChilds = equipment.maxRunesCount,
                ItemType = equipment.type,
                Entity = entity,
                Childs = childs,
                SlotID = slotID
            };
        }
        
        public void AddItem(ref Entity entity) {
            if (Inventory.Full) return;

            Inventory.LastEmpty = FindLastEmptySlot();

            Inventory.Bag.Add(Inventory.LastEmpty, CreateItemData(ref entity, Inventory.LastEmpty));
            Inventory.SlotsCount++;
            entity.Get<ViewGO>().GameObject.SetActive(false);
            entity.SetOwner(Player);
        }

        public void RemoveItem(InventorySlot slot) {
            var data = Inventory.Bag[slot.Index];

            Inventory.SlotsCount--;
            Inventory.Bag.Remove(slot.Index);
            slot.RemoveItem();
            Debug.Log(data.Entity.Get<ViewGO>().GameObject.name);
            data.Entity.Get<ViewGO>().GameObject.SetActive(true);
        }

        public void EquipItem(InventorySlot slot) {
            ref var equipments = ref Player.Get<EquipmentList>();
            ref var itemToEquip = ref slot.Data;
            if (slot.Data.ItemType is EquipmentType.Rune or EquipmentType.None) return;
            Inventory.Bag.Remove(slot.Index);
            
            if (Inventory.ActiveSlots.ContainsKey(itemToEquip.ItemType)) {
                var itemToRemove = Inventory.ActiveSlots[itemToEquip.ItemType];
                var slotIndex = slot.Index;
                Inventory.Bag.Add(slotIndex, itemToRemove);
                Inventory.ActiveSlots.Remove(itemToRemove.ItemType);
                equipments.value.Remove(itemToRemove.Entity);
                itemToRemove.Entity.Remove<EquipedTag>();
            }
            else
                Inventory.SlotsCount--;
            itemToEquip.Entity.SetOwner(Player);
            itemToEquip.Entity.Add(default(EquipedTag));
            equipments.value.Add(itemToEquip.Entity);
            Inventory.ActiveSlots.Add(itemToEquip.ItemType, itemToEquip);
        }

        public void EquipItem(ref Entity entity) {
            ref var equipments = ref Player.Get<EquipmentList>();
            ref var equipment = ref entity.Get<Equipment>();

            if (Inventory.ActiveSlots.ContainsKey(equipment.type)) {
                var itemToMoveToBag = Inventory.ActiveSlots[equipment.type];
                Inventory.ActiveSlots.Remove(itemToMoveToBag.ItemType);
                equipments.value.Remove(itemToMoveToBag.Entity);
            }

            equipments.value.Add(entity);
            Inventory.ActiveSlots.Add(equipment.type, new ItemData {
                ID = equipment.id,
                ItemType = equipment.type,
                Entity = entity
            });

            entity.Get<ViewGO>().GameObject.SetActive(false);
        }

        public void AddRuneToItem(ItemData rune) {
            var dataFromInventory = lastSelected;
            dataFromInventory.Childs.Add(rune);
            dataFromInventory.Entity.Get<Equipment>().runes.Add(rune.Entity);
            rune.Entity.SetOwner(Player);
            if(dataFromInventory.Entity.Has<EquipedTag>())
                rune.Entity.Add(default(EquipedTag));
            Inventory.Bag.Remove(rune.SlotID);
            Inventory.SlotsCount--;
            OnEquipRune?.Invoke(dataFromInventory);
        }

        public void RemoveRuneFromItem(ItemData rune) {
            lastSelected.Childs.Remove(rune);
            lastSelected.Entity.Get<Equipment>().runes.Remove(rune.Entity);
        }
    }
    
    [Serializable]
    public class Inventory {
        private const int MAX_SLOTS = 35;
        public int SlotsCount;
        public int LastEmpty;
        public Dictionary<EquipmentType, ItemData> ActiveSlots = new();
        public Dictionary<int, ItemData> Bag = new();
        [NonSerialized] public Dictionary<int, EntityLink> Links = new();
        public bool Full => SlotsCount >= MAX_SLOTS;

        public void Clear() {
            ActiveSlots.Clear();
            Bag.Clear();
            SlotsCount = 0;
            LastEmpty = 0;
            Links.Clear();
        }
    }
}