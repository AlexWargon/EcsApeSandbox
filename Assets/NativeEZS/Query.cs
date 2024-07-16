using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Wargon.NEZS {
    [BurstCompile]
    public unsafe struct Query : IDisposable {
        [NativeDisableUnsafePtrRestriction]
        internal QueryInternal* queryInternal;
        public int Count => queryInternal->Count;

        public Query(int id, in World world) {
            queryInternal = QueryInternal.Create(id, in world);
        }
        public Query With<T>() where T : struct {
            queryInternal->With<T>();
            return this;
        }
        public Query None<T>() where T : struct {
            queryInternal->None<T>();
            return this;
        }
        public Query Any<T>() where T : struct {
            queryInternal->Any<T>();
            return this;
        }
        internal Query With(ComponentType type) {
            queryInternal->With(type);
            return this;
        }
        internal Query None(ComponentType type) {
            queryInternal->None(type);
            return this;
        }
        internal Query Any(ComponentType type) {
            queryInternal->Any(type);
            return this;
        }
        public ref Entity GetEntity(int index) {
            return ref queryInternal->GetEntity(index);
        }

        public void Dispose() {
            QueryInternal.Destroy(queryInternal);
        }
    }
    [BurstCompile]
    public unsafe struct QueryInternal {
        internal FixedIntBuffer16 with;
        internal FixedIntBuffer8 none;
        internal FixedIntBuffer8 any;
        internal UnsafeList<int> entities;
        internal UnsafeList<int> entityMap;
        internal int Count;
        [NativeDisableUnsafePtrRestriction]
        internal readonly WorldInternal* world;
        internal int ID;

        internal QueryInternal(int id, in World world) {
            any = default;
            with = default;
            none = default;
            entities = new UnsafeList<int>(16, World.Allocator);
            entityMap = new UnsafeList<int>(16, World.Allocator);
            ID = id;
            this.world = world.worldInternal;
            Count = 0;
        }
        public static QueryInternal* Create(int id, in World world) {
            var ptr = (QueryInternal*)UnsafeUtility.Malloc(sizeof(QueryInternal), UnsafeUtility.AlignOf<QueryInternal>(), World.Allocator);
            *ptr = new QueryInternal(id, in world);
            return ptr;
        }

        public static void Destroy(QueryInternal* queryInternal) {
            queryInternal->entities.Dispose();
            queryInternal->entityMap.Dispose();
            UnsafeUtility.Free(queryInternal, World.Allocator);
        }

        internal void With<T>() where T : struct {
            with.Add(ComponentType<T>.Index);
        }
        internal void None<T>() where T : struct {
            none.Add(ComponentType<T>.Index);
        }
        internal void Any<T>() where T : struct {
            any.Add(ComponentType<T>.Index);
        }
        internal void With(ComponentType type){
            with.Add(ComponentTypeMap.Index(type));
        }
        internal void None(ComponentType type) {
            none.Add(ComponentTypeMap.Index(type));
        }
        internal void Any(ComponentType type) {
            any.Add(ComponentTypeMap.Index(type));
        }
        public ref Entity GetEntity(int index) {
            return ref world->GetEntity(entities[index]);
        }
        [BurstCompile]
        public void AddEntity(int entity) {
            if (entities.Length - 1 <= Count) {
                entities.Resize(Count + 16);
            }
            if (entityMap.Length - 1 <= entity) {
                entityMap.Resize(entity + 16);
            }
            entities[Count] = entity;
            Interlocked.Increment(ref Count);
            entityMap[entity] = Count;
        }
        public void RemoveEntity(int entity) {
            
            if (Has(entity) == false) return;
            var index = entityMap[entity] - 1;
            entityMap[entity] = 0;
            Interlocked.Decrement(ref Count);
            if (Count > index) {
                entities[index] = entities[Count];
                entityMap[entities[index]] = index + 1;
            }
        }
        [BurstCompile]
        public bool Has(int entity) {
            if (entityMap.Length <= entity) return false;
            return entityMap[entity] > 0;
        }
    }
}