using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Wargon.NEZS {
    public unsafe readonly struct World : IDisposable {
        [NativeDisableUnsafePtrRestriction]
        internal readonly WorldInternal* worldInternal;
        public const Allocator Allocator = Unity.Collections.Allocator.Persistent;
        public World(byte id, in WorldConfig config) {
            worldInternal = WorldInternal.Create(id, in config);
            worldInternal->Init(this);
        }
        public Pool<T> GetPool<T>() where T : struct => worldInternal->GetPool<T>();
        internal Pool GetPoolUntyped<T>() where T : struct => worldInternal->GetUntypedPool<T>();
        public void Dispose() {
            worldInternal->Dispose();
            UnsafeUtility.Free(worldInternal, World.Allocator);
        }

        public ref Entity GetEntity(int index) => ref worldInternal->GetEntity(index);
        public ref Entity CreateEntity() => ref worldInternal->CreateEntity();
        public Query GetQuery() => worldInternal->GetQuery(in this);
    }

    internal unsafe struct UnsafeListParallel<T> where T: unmanaged {
        internal UnsafeList<T>.ParallelWriter* listParallelWriter;
        internal UnsafeList<T>* list;
        public UnsafeListParallel(int capacity, Allocator allocator) {
            list = UnsafeList<T>.Create(capacity, allocator);
            listParallelWriter = UnsafeHelper.Malloc<UnsafeList<T>.ParallelWriter>(World.Allocator);
            *listParallelWriter = list->AsParallelWriter();
        }

        public static UnsafeListParallel<T>* Create(int capacity, Allocator allocator) {
            var ptr = UnsafeHelper.Malloc<UnsafeListParallel<T>>(allocator);
            *ptr = new UnsafeListParallel<T>(capacity, allocator);
            return ptr;
        }

        public static void Destroy(UnsafeListParallel<T>* list, Allocator allocator) {
            list->list->Dispose();
            UnsafeUtility.Free(list, allocator);
        }
        public void AddParallel(in T item) {
            listParallelWriter->AddNoResize(item);
        }

        public ref T ElementAt(int index) {
            return ref list->ElementAt(index);
        }
        public void Clear() {
            list->Clear();
        }
    }
    [BurstCompile]
    internal unsafe struct WorldInternal : IDisposable {
        internal UnsafeList<Entity> entities;
        internal UnsafeList<int> freeEntities;
        internal int lastEntity;
        internal UnsafeParallelHashMap<int, Pool> pools;
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<DirtyEntity>* dirtyEntities;
        internal UnsafeList<DirtyEntity>.ParallelWriter dirtyEntitiesParallelWriter;
        internal NativeParallelHashMap<int, Query> queries;
        
        internal NativeParallelHashMap<int, Archetype> archetypes;
        internal Archetype emptyArchetpye;
        internal int queriesCount;
        public int poolsCount;
        public byte id;
        internal Allocator allocator;
        internal World Self;
        internal WorldConfig Config;
        internal static WorldInternal* Create(byte id, in WorldConfig config) {
            var allocator = World.Allocator;
            var ptr = (WorldInternal*)UnsafeUtility.Malloc(sizeof(WorldInternal), UnsafeUtility.AlignOf<WorldInternal>(), allocator);
            ptr->entities = new UnsafeList<Entity>(config.EntitiesCache, allocator);
            ptr->entities.Length = 128;
            ptr->freeEntities = new UnsafeList<int>(config.EntitiesCache, allocator);
            ptr->pools = new UnsafeParallelHashMap<int, Pool>(config.PoolsAmount, allocator);
            ptr->dirtyEntities = UnsafeList<DirtyEntity>.Create(config.DirtyEntitiesCache, allocator);
            ptr->dirtyEntitiesParallelWriter = ptr->dirtyEntities->AsParallelWriter();
            ptr->queries = new NativeParallelHashMap<int, Query>(32, allocator);
            ptr->archetypes = new NativeParallelHashMap<int, Archetype>(64, allocator);
            ptr->queriesCount = 0;
            ptr->poolsCount = 0;
            ptr->id = id;
            ptr->allocator = allocator;
            ptr->lastEntity = 0;
            ptr->Config = config;
            return ptr;
        }

        internal void Init(World world) {
            Self = world;
            emptyArchetpye = new Archetype(world);
            archetypes.Add(0, emptyArchetpye);
            CreateEntity();
        }
        public void Dispose() {
            entities.Dispose();
            foreach (var pool in pools) {
                if (pool.Value.IsCreated) {
                    pool.Value.Dispose();
                }
            }
            freeEntities.Dispose();
            pools.Dispose();
            UnsafeList<DirtyEntity>.Destroy(dirtyEntities);
            foreach (var keyValue in archetypes) {
                keyValue.Value.Dispose();
            }
            foreach (var keyValue in queries) {
                if(keyValue.Value.queryInternal != null)
                    keyValue.Value.Dispose();
            }
            queries.Dispose();
            archetypes.Dispose();
        }

        private void ResizeEntitiesArray(int newSize) {
            entities.Resize(newSize);
            entities.Length = newSize;
        }
        public ref Entity CreateEntity() {
            Entity entity;
            entity.Index = lastEntity;
            entity.World = id;
            entity.Archetype = emptyArchetpye.Ptr;
            if (lastEntity >= entities.Length) {
                ResizeEntitiesArray(lastEntity + 64);
            }
            entities.ElementAt(lastEntity) = entity;
            lastEntity++;
            return ref entities.ElementAt(entity.Index);
        }

        public ref Entity GetEntity(int index) {
            return ref entities.ElementAt(index);
        }
        [BurstDiscard]
        public Archetype GetOrCreateArchetype(ref NativeParallelHashSet<int> mask) {
            int hash = mask.IsEmpty ? 0 : ArchetypeHashCode.Get(ref mask);
            if (archetypes.TryGetValue(hash, out Archetype archetype)) {
                mask.Dispose();
                return archetype;
            }
            archetype = new Archetype(in Self, ref mask, hash);
            if (archetypes.TryAdd(hash, archetype) == false) { // fix for parallel 
                archetype.Dispose();
                archetype = archetypes[hash];
            }
            return archetype;
        }
        public Query GetQuery(in World world) {
            var newQuery = new Query(queriesCount, in world);
            queries.Add(queriesCount, newQuery);
            queriesCount++;
            return newQuery;
        }
        public Pool<T> GetPool<T>() where T : struct {
            return GetUntypedPool<T>().AsTypedPool<T>();
        }
        
        public Pool GetUntypedPool<T>() where T : struct {
            var index = ComponentType<T>.Index;
            if (pools.TryGetValue(index, out Pool pool)) {
                return pool;
            }
            pool = new Pool(Config.PoolSizeCache, ComponentType<T>.AsComponentType());
            pools.Add(index, pool);
            poolsCount++;
            return pool;
        }
    }

    public struct WorldConfig {
        public int EntitiesCache;
        public int DirtyEntitiesCache;
        public int PoolSizeCache;
        public int PoolsAmount;
        public static WorldConfig Default {
            get=>new WorldConfig() {
                EntitiesCache = 128,
                DirtyEntitiesCache = 128,
                PoolSizeCache = 128,
                PoolsAmount = 32
            };
        }
    }
    public class Worlds {
        [ReadOnly]
        private readonly static SharedStatic<NativeList<World>> ShaderStaticList = SharedStatic<NativeList<World>>.GetOrCreate<Worlds>();
        public static ref NativeList<World> List => ref ShaderStaticList.Data;

        public static ref World Create() {
            if (ShaderStaticList.Data.IsCreated == false) {
                ShaderStaticList.Data = new NativeList<World>(4, Allocator.Persistent);
            }
            var index = (byte)List.Length;
            var world = new World(index, WorldConfig.Default);
            List.Add(in world);
            return ref List.ElementAt(index);
        }
        public static ref World Create(in WorldConfig config) {
            if (ShaderStaticList.Data.IsCreated == false) {
                ShaderStaticList.Data = new NativeList<World>(4, Allocator.Persistent);
            }
            var index = (byte)List.Length;
            var world = new World(index, in config);
            List.Add(in world);
            return ref List.ElementAt(index);
        }
        public static void Dispose() {
            ShaderStaticList.Data.Dispose();
            ComponentTypeMap.Dispose();
        }
    }
}