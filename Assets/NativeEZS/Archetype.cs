using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Wargon.NEZS {
    public unsafe struct Archetype {
        [NativeDisableUnsafePtrRestriction]
        internal ArchetypeInternal* Ptr;

        public bool IsCreated => Ptr != null;
        public Archetype(in World world) {
            Ptr = ArchetypeInternal.Create(in world);
        }
        public Archetype(in World world, ref NativeParallelHashSet<int> mask, int hash) {
            Ptr = ArchetypeInternal.Create(in world, ref mask, hash);
        }

        public void Dispose() {
            ArchetypeInternal.Destroy(Ptr);
            Ptr = null;
        }
    }

    public unsafe struct ArchetypeInternal {
        [NativeDisableUnsafePtrRestriction]
        internal WorldInternal* WorldInternal;
        internal ArchetypeInternal* Self;
        internal NativeParallelHashSet<int> Mask;
        internal int MaskSize;
        internal UnsafeParallelHashMap<int, ComponentEdge> ComponentEdges;
        internal UnsafePtrList<QueryInternal> queries;
        private ArchetypeEdge DestroyEdge;
        private ArchetypeEdge CreateEdge;
        internal int Hash;
        private bool IsEmpty;
        private int usingOtherThread;
        // public override string ToString() {
        //     var build = new StringBuilder("Archetype");
        //     if (Mask.IsEmpty) {
        //         build.Append("_Empty");
        //         return build.ToString();
        //     }
        //     build.Append('<');
        //     foreach (var i in Mask) {
        //         build.Append(ComponentTypeMap.Type(i).Name);
        //         build.Append(',');
        //     }
        //     build.Append('>');
        //     var value = build.ToString();
        //     build.Clear();
        //     return value;
        // }
        // public string MaskToString(in NativeParallelHashSet<int> mask) {
        //     var build = new StringBuilder("Archetype");
        //     build.Append('<');
        //     foreach (var i in mask) {
        //         build.Append(ComponentTypeMap.Type(i).Name);
        //         build.Append(',');
        //     }
        //     build.Append('>');
        //     var value = build.ToString();
        //     build.Clear();
        //     return value;
        // }
        

        public static void Destroy(ArchetypeInternal* archetype) {
            archetype->Mask.Dispose();
            foreach (var keyValue in archetype->ComponentEdges) {
                keyValue.Value.Dispose();
            }
            archetype->ComponentEdges.Dispose();
            archetype->queries.Dispose();
            archetype->CreateEdge.Dispose();
            archetype->DestroyEdge.Dispose();
            UnsafeUtility.Free(archetype, World.Allocator);
        }

        public static ArchetypeInternal* Create(in World world) {
            ArchetypeInternal* pointer = UnsafeHelper.Malloc<ArchetypeInternal>(World.Allocator);
            *pointer = new ArchetypeInternal(in world, pointer);
            return pointer;
        }

        public static ArchetypeInternal* Create(in World world, ref NativeParallelHashSet<int> mask, int hash) {
            ArchetypeInternal* pointer = UnsafeHelper.Malloc<ArchetypeInternal>(World.Allocator);
            *pointer = new ArchetypeInternal(in world, in mask, hash, pointer);
            return pointer;
        }
        
        public ArchetypeInternal(in World world, ArchetypeInternal* self) {
            WorldInternal = world.worldInternal;
            Mask = new NativeParallelHashSet<int>(1, World.Allocator);
            ComponentEdges = new UnsafeParallelHashMap<int, ComponentEdge>(4, World.Allocator);
            queries = new UnsafePtrList<QueryInternal>(4, World.Allocator);
            Hash = 0;
            MaskSize = 1;
            IsEmpty = true;
            DestroyEdge = new ArchetypeEdge();
            CreateEdge = new ArchetypeEdge();
            usingOtherThread = 0;
            Self = self;
        }
        public ArchetypeInternal(in World world, in NativeParallelHashSet<int> mask, int hash, ArchetypeInternal* self) {
            WorldInternal = world.worldInternal;
            Mask = mask;
            ComponentEdges = new UnsafeParallelHashMap<int, ComponentEdge>(4, World.Allocator);
            queries = new UnsafePtrList<QueryInternal>(4, World.Allocator);
            Hash = hash;
            MaskSize = mask.Count();
            IsEmpty = false;
            DestroyEdge = default;
            CreateEdge = default;
            usingOtherThread = 0;
            Self = self;
            foreach (var keyValue in WorldInternal->queries) {
                var q = keyValue.Value.queryInternal;
                if (IsQueryMatch(q)) {
                    queries.Add(q);
                }
            }

            DestroyEdge =  new ArchetypeEdge(queries.Length, WorldInternal->emptyArchetpye.Ptr);
            CreateEdge = new ArchetypeEdge(queries.Length, Self);
            for (int i = 0; i < queries.Length; i++) {
                var queryPtr = queries[i];
                DestroyEdge.QueriesToRemoveEntity->Add(queryPtr);
                CreateEdge.QueriesToAddEntity->Add(queryPtr);
            }
            
        }
        private bool IsQueryMatch(QueryInternal* q) {

            for (var i = 0; i < q->none.Count; i++) {
                if (Mask.Contains(q->none.buffer[i]))
                    return false;
            }
            var match = 0;
            for (var i = 0; i < q->with.Count; i++) {
                if (Mask.Contains(q->with.buffer[i])) {
                    match++;
                    if (match == q->with.Count) {
                        return true;
                    }
                }
            }
            return false;
        }
        [BurstCompile]
        private bool HasQuery(QueryInternal* query) {
            for (var i = 0; i < queries.Length; i++)
                if (queries[i]->ID == query->ID)
                    return true;
            return false;
        }

        internal void TransferDestroy(ref Entity entity) {
            entity.Archetype = DestroyEdge.Archetype;
            WorldInternal->dirtyEntities->Add(new DirtyEntity {
                Edge = DestroyEdge,
                Entity = entity.Index
            });
        }

        internal void TransferAdd(ref Entity entity, int component) {
            if (ComponentEdges.TryGetValue(component, out ComponentEdge edge)) {
                entity.Archetype = edge.onAddComponent.Archetype;
                WorldInternal->dirtyEntitiesParallelWriter.AddNoResize(new DirtyEntity {
                    Edge = edge.onAddComponent,
                    Entity = entity.Index
                });
                return;
            }

            CreateComponentEdge(component, out edge);
            entity.Archetype = edge.onAddComponent.Archetype;
            WorldInternal->dirtyEntitiesParallelWriter.AddNoResize(new DirtyEntity {
                Edge = edge.onAddComponent,
                Entity = entity.Index
            });

        }

        internal void TransferRemove(ref Entity entity, int component) {
            if (ComponentEdges.TryGetValue(component, out ComponentEdge edge)) {
                entity.Archetype = edge.onRemoveComponent.Archetype;
                WorldInternal->dirtyEntitiesParallelWriter.AddNoResize(new DirtyEntity {
                    Edge = edge.onRemoveComponent,
                    Entity = entity.Index
                });
                return;
            }
       
            CreateComponentEdge(component, out edge);
            entity.Archetype = edge.onRemoveComponent.Archetype;
            WorldInternal->dirtyEntitiesParallelWriter.AddNoResize(new DirtyEntity {
                Edge = edge.onRemoveComponent,
                Entity = entity.Index
            });
        }
        [BurstDiscard]
        private void CreateComponentEdge(int component, out ComponentEdge edge) {
            var maskAdd = new NativeParallelHashSet<int>(MaskSize+1, World.Allocator);
            var maskRemove = new NativeParallelHashSet<int>(math.max(1,MaskSize-1), World.Allocator);
            foreach (var i in Mask) {
                maskAdd.Add(i);
                maskRemove.Add(i);
            }
            maskAdd.Add(component);
            maskRemove.Remove(component);
            var edgeOnAdd = GetOrCreateMigration(WorldInternal->GetOrCreateArchetype(ref maskAdd).Ptr);
            var edgeOnRemove = GetOrCreateMigration(WorldInternal->GetOrCreateArchetype(ref maskRemove).Ptr);
            edge = new ComponentEdge(edgeOnAdd, edgeOnRemove);
            if (ComponentEdges.TryAdd(component, edge) == false) {
                edge.Dispose();
                edge = ComponentEdges[component];
            }
        }

        private ArchetypeEdge GetOrCreateMigration(ArchetypeInternal* archetypeNext) {
            ArchetypeEdge migrationEdge = new ArchetypeEdge(8, archetypeNext);
            for (var i = 0; i < queries.Length; i++) {
                var query = queries[i];
                if (!archetypeNext->HasQuery(query))
                    migrationEdge.QueriesToRemoveEntity->Add(query);
            }

            for (var i = 0; i < archetypeNext->queries.Length; i++) {
                var query = archetypeNext->queries[i];
                if (!HasQuery(query))
                    migrationEdge.QueriesToAddEntity->Add(query);
            }
            return migrationEdge;
        }
        
        public bool HasComponent(int componentIndex) {
            foreach (var i in Mask) {
                if (i == componentIndex) return true;
            }

            return false;
        }
        
    }
    [BurstCompile]
    public struct DirtyEntity {
        public int Entity;
        public ArchetypeEdge Edge;
    }

    public unsafe struct ComponentEdge {
        public ArchetypeEdge onAddComponent;
        public ArchetypeEdge onRemoveComponent;

        public ComponentEdge(ArchetypeEdge onAddComponent, ArchetypeEdge onRemoveComponent) {
            this.onAddComponent = onAddComponent;
            this.onRemoveComponent = onRemoveComponent;
        }

        public void Dispose() {
            onAddComponent.Dispose();
            onRemoveComponent.Dispose();
        }
    }

    public unsafe struct ArchetypeEdge {
        
        [NativeDisableUnsafePtrRestriction]
        public UnsafePtrList<QueryInternal>* QueriesToRemoveEntity;
        
        [NativeDisableUnsafePtrRestriction]
        public UnsafePtrList<QueryInternal>* QueriesToAddEntity;
        
        [NativeDisableUnsafePtrRestriction]
        public ArchetypeInternal* Archetype;
        public ArchetypeEdge(int capacity, ArchetypeInternal* archetype) {
            QueriesToAddEntity = UnsafePtrList<QueryInternal>.Create(capacity, World.Allocator);
            QueriesToRemoveEntity = UnsafePtrList<QueryInternal>.Create(capacity, World.Allocator);
            Archetype = archetype;
        }
        internal void Execute(int entity) {
            if(QueriesToRemoveEntity != null)
                for (int i = 0; i < QueriesToRemoveEntity->Length; i++) {
                    QueriesToRemoveEntity->ElementAt(i)->RemoveEntity(entity);
                }
            if(QueriesToAddEntity != null)
                for (int i = 0; i < QueriesToAddEntity->Length; i++) {
                    QueriesToAddEntity->ElementAt(i)->AddEntity(entity);
                }
        }

        public void Dispose() {
            if(QueriesToAddEntity != null)
                UnsafePtrList<QueryInternal>.Destroy(QueriesToRemoveEntity);
            if(QueriesToAddEntity != null)
                UnsafePtrList<QueryInternal>.Destroy(QueriesToAddEntity);
        }
    }
    
    [BurstCompile]
    internal static class ArchetypeHashCode {
        [BurstCompile]
        public static int Get(ref NativeParallelHashSet<int> mask) {
            unchecked {
                int hash = (int) 2166136261;
                const int p = 16777619;

                foreach (var i in mask) {
                    hash = (hash ^ i) * p;
                }
                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                hash += hash;
                        //>> owner;
                return hash;
            }
        }
    }
}