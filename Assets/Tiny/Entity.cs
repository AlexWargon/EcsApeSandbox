using System;

namespace Wargon.TinyEcs {
    public struct Entity : IEquatable<Entity> {
        public int Index;
        internal World world;
        internal Archetype Archetype;
        public bool Equals(Entity other) {
            return other.Index == Index;
        }
    }
    public static class EntityExtensions {
        public static void Add<T>(ref this Entity entity, T componet) {
            entity.Archetype.TransformAdd(ref entity, ComponentMeta.GetIndex<T>());
        }
        public static void Remove<T>(ref this Entity entity) {
            entity.Archetype.TransformRemove(ref entity, ComponentMeta.GetIndex<T>());
        }
        public static void Has<T>(ref this Entity entity) {
            entity.Archetype.HasComponent(ComponentMeta.GetIndex<T>());
        }

        public static T Get<T>(in this Entity entity) {
            return entity.world.GetPool<T>().Get(entity.Index);
        }
    }
}