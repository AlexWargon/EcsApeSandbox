using System;

namespace Wargon.TinyEcs {
    public class Query {
        internal int count;
        internal int[] entities;
        internal int[] entityMap;
        
        internal int entityToUpdateCount;
        internal EntityToUpdate[] entityToUpdates;

        internal int indexInside;
        
        internal Mask with;
        internal Mask without;
        internal Mask any;
        internal bool IsDirty;
        public Query(World world) {
            WorldInternal = world;
            entities = new int[256];
            entityMap = new int[256];
            with = new Mask(10);
            without = new Mask(4);
            any = new Mask(6);
            entityToUpdates = new EntityToUpdate[256];
            count = 0;
        }

        internal World WorldInternal;

        public int FullSize {
            get=> entities.Length;
        }

        public int Count {
            get => count;
        }
        public bool IsEmpty {
            get => count == 0;
        }


        internal (int[], int[], EntityToUpdate[], int) GetRaw() {
            return (entities, entityMap, entityToUpdates, count);
        }


        internal void OnAddWith(int entity) {
            if (entityToUpdates.Length <= entityToUpdateCount)
                Array.Resize(ref entityToUpdates, entityToUpdateCount + 16);
            ref var e = ref entityToUpdates[entityToUpdateCount];
            e.entity = entity;
            e.add = true;
            entityToUpdateCount++;
            WorldInternal.AddDirtyQuery(this);
            IsDirty = true;
        }

        internal void OnRemoveWith(int entity) {
            if (entityToUpdates.Length <= entityToUpdateCount)
                Array.Resize(ref entityToUpdates, entityToUpdateCount + 16);
            ref var e = ref entityToUpdates[entityToUpdateCount];
            e.entity = entity;
            e.add = false;
            entityToUpdateCount++;
            WorldInternal.AddDirtyQuery(this);
            IsDirty = true;
        }

        public ref Entity GetEntity(int index) {
            return ref WorldInternal.GetEntity(entities[index]);
        }

        private void Remove(int entity) {
            if (!Has(entity)) return;
            var index = entityMap[entity] - 1;
            entityMap[entity] = 0;
            count--;
            if (count > index) {
                entities[index] = entities[count];
                entityMap[entities[index]] = index + 1;
            }
        }

        private void Add(int entity) {
            if (entities.Length - 1 <= count) {
                Array.Resize(ref entities, count + 16);
            }

            if (entityMap.Length - 1 <= entity) {
                Array.Resize(ref entityMap, entity + 16);
            }
            if (Has(entity)) return;
            entities[count++] = entity;
            entityMap[entity] = count;
        }

        internal void Update() {
            for (var i = 0; i < entityToUpdateCount; i++) {
                ref var e = ref entityToUpdates[i];
                if (e.add)
                    Add(e.entity);
                else
                    Remove(e.entity);
            }

            entityToUpdateCount = 0;
            IsDirty = false;
        }

        private bool Has(int entity) {
            if (entityMap.Length <= entity)
                return false;
            return entityMap[entity] > 0;
        }

        public Enumerator GetEnumerator() {
            Enumerator e;
            e.query = this;
            e.index = -1;
            return e;
        }

        internal int GetEntityIndex(int index) {
            return entities[index];
        }

        internal struct EntityToUpdate {
            public int entity;
            public bool add;
        }
    }
}