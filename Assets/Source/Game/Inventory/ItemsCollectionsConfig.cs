using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    [CreateAssetMenu]
    public class ItemsCollectionsConfig : ScriptableObject {
        public List<ItemConfig> items;
        private Dictionary<int, ItemConfig> map;

        public void Construct() {
            map = new Dictionary<int, ItemConfig>();
            foreach (var itemConfig in items) {
                map.Add(itemConfig.ID, itemConfig);
            }
        }

        public ItemConfig GetConfig(int id) => map[id];
        private void OnValidate() {
            for (var i = 0; i < items.Count; i++) {
                items[i].ID = i;
            }
        }
    }

    public class LootServise {
        private ItemsCollectionsConfig _collectionsConfig;
        private World _world;
        public void SetWorld(World world) => _world = world;

        public ref Entity SpawnItem(int id, Vector3 pos) {
            return ref SpawnItem(_collectionsConfig.GetConfig(id), pos);
        }
        public ref Entity SpawnItem(ItemConfig config, Vector3 pos) {
            var link = Object.Instantiate(config.Prefab, pos, Quaternion.identity);
            var e = _world.CreateEntity();
            link.Link(ref e);
            link.Entity.Get<Equipment>().id = config.ID;
            
            return ref link.Entity;
        }

        public ref Entity SpawnRandomItem(Vector3 pos) {
            var random = _collectionsConfig.items.RandomElement();
            
            return ref SpawnItem(random, pos);
        }
    }
}