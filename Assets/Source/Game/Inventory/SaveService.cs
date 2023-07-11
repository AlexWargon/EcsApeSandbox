using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Wargon.Ecsape;
using Component = Wargon.Ecsape.Component;

namespace Rogue {
    public class SaveService {
        private string fileName = "save.json";
        private string FullPath => Path.Combine(Application.persistentDataPath, fileName);

        private string fileNameBinary = "save.data";
        private string FullPathBinary => Path.Combine(Application.persistentDataPath, fileNameBinary);

        private string SavePath(string key) {
            return Path.Combine(Application.persistentDataPath, $"save_{key}.data");
        }
        private BinaryFormatter BinaryFormatter {
            get {
                var binaryFormatter = new BinaryFormatter();
                
                SurrogateSelector ss = new SurrogateSelector();
                Vector3SerializationSurrogate v3ss = new Vector3SerializationSurrogate();
                Vector2SerializationSurrogate v2ss = new Vector2SerializationSurrogate();
                QuanterionSerializationSurrogate qss = new QuanterionSerializationSurrogate();
                ss.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), v3ss);
                ss.AddSurrogate(typeof(Vector2), new StreamingContext(StreamingContextStates.All), v2ss);
                ss.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), qss);
                binaryFormatter.SurrogateSelector = ss;
                return binaryFormatter;
            }
        }

        private void Save<T>(T item, string key) {
            try {
                using var dataStream = new FileStream(SavePath(key), FileMode.OpenOrCreate);
                BinaryFormatter.Serialize(dataStream, item);
                dataStream.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        }

        public T Load<T>(string key) {
            T item;
            try {
                using var dataStream = new FileStream(SavePath(key), FileMode.Open);
                item = (T)BinaryFormatter.Deserialize(dataStream);
                dataStream.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
            return item;
        }
        public void SaveEntityBinary(Entity entity) {
            var components = entity.GetArchetype().GetPureComponents(entity);
            var saveData = new ComponentListSave {
                Components = new ComponentSave[components.Length]
            };
            for (var index = 0; index < components.Length; index++) {
                var component = components[index];
                var cType = component.GetType();
                saveData.Components[index] = new ComponentSave {
                    TypeIndex = Component.GetIndex(cType),
                    TypeName = cType.Name,
                    FullTypeName = cType.FullName,
                    Value = component
                };
            }
            
            Save(saveData, "test");
        }

        public void LoadEntityBinary(ref Entity entity) {

            var loadedData = Load<ComponentListSave>("test");
            
            for (var i = 0; i < loadedData.Components.Length; i++) {
                var componentInfo = loadedData.Components[i];
                if (entity.Has(componentInfo.TypeIndex)) {
                    entity.SetBoxed(componentInfo.Value);
                }
                else {
                    entity.AddBoxed(componentInfo.Value);
                }
            }
        }

        public void SaveInventory() {
            var service = DI.Get<InventoryService>();
            //ref var equipments = ref entity.Get<EquipmentList>();
            Save(service.Inventory, "inventory");
        }

        public void LoadInventory() {
            var service = DI.Get<InventoryService>();
            
            for (var i = 0; i < service.Inventory.Bag.Values.Count; i++) {
                var s = service.Inventory.Bag.Values.ElementAt(i);
                s.Entity.DestroyNow();
            }
            for (var i = 0; i < service.Inventory.ActiveSlots.Values.Count; i++) {
                var s = service.Inventory.ActiveSlots.Values.ElementAt(i);
                s.Entity.DestroyNow();
            }
            
            var inventory = Load<Inventory>("inventory");
            
            // var loots = DI.Get<LootServise>();
            // service.Clear();
            //
            // foreach (var itemData in inventory.Bag.Values) {
            //     ref var item = ref loots.SpawnItem(itemData.ID, Vector3.zero);
            //     service.AddItem(ref item);
            //     
            //
            // }
            // foreach (var itemData in inventory.ActiveSlots.Values) {
            //     ref var item = ref loots.SpawnItem(itemData.ID, Vector3.zero);
            //     service.EquipItem(ref item);
            // }

            service.SetNewInventory(inventory);
        }
    }
}