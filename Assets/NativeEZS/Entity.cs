using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Wargon.NEZS
{

    public unsafe struct Entity {
        public int Index;
        internal byte World;
        [NativeDisableUnsafePtrRestriction] internal ArchetypeInternal* Archetype;
        public int ArchetypeHash => Archetype->Hash;
        public string ArchetypeString => Archetype->ToString();
        public bool IsNUll() {
            return Archetype == null;
        }

        public unsafe Entity(int index, World world, ArchetypeInternal* archetype) {
            Index = index;
            World = world.worldInternal->id;
            Archetype = archetype;
        }

        public void Destroy() {
            Archetype->TransferDestroy(ref this);
        }
    }
    [BurstCompile]
    public static class EntityExtensions{
        public unsafe static bool Has<TComponent>(this ref Entity entity) where TComponent : struct {
            return entity.Archetype->HasComponent(ComponentType<TComponent>.Index);
        }
        [BurstCompile]
        public unsafe static ref TComponent Get<TComponent>(this ref Entity entity) where TComponent : struct {
            if (entity.Archetype->HasComponent(ComponentType<TComponent>.Index) == false) {
                throw new Exception($"Entity:{entity.Index} don't contaons {typeof(TComponent).FullName} component");
            }
            return ref Worlds.List.ElementAt(entity.World).GetPoolUntyped<TComponent>().Get<TComponent>(entity.Index);
        }
        [BurstCompile]
        public unsafe static void Add<TComponent>(this ref Entity entity, in TComponent component) where TComponent : struct {
            var componentType = ComponentType<TComponent>.Index;
            if (entity.Archetype->HasComponent(componentType)) return;
            ref var world = ref Worlds.List.ElementAt(entity.World);
            world.GetPoolUntyped<TComponent>().Set(entity.Index, in component);
            entity.Archetype->TransferAdd(ref entity, componentType);
        }
        [BurstCompile]
        public unsafe static void Remove<TComponent>(this ref Entity entity) where TComponent : struct {
            var componentType = ComponentType<TComponent>.Index;
            if(entity.Archetype->HasComponent(componentType) == false) return;
            ref var world = ref Worlds.List.ElementAt(entity.World);
            world.GetPoolUntyped<TComponent>().Set(entity.Index, default(TComponent));
            entity.Archetype->TransferRemove(ref entity, componentType);
        }
        [BurstCompile]
        public unsafe static void Destroy(this ref Entity entity) {
            entity.Archetype->TransferDestroy(ref entity);
        }
    }

    public interface IPool {
        
    }




    public unsafe struct FixedIntBuffer16 {
        public fixed int buffer[16];
        public int count;
        public int Count => count;
        public void Add(int value) {
            if(count == 15) return;
            buffer[count++] = value;
        }
    }
    public unsafe struct FixedIntBuffer8 {
        public fixed int buffer[8];
        private int count;
        public int Count => count;
        public void Add(int value) {
            if(count == 7) return;
            buffer[count++] = value;
        }
    }




    public static class UnsafeHelper {
        private static readonly Dictionary<Type, int> cachedAligns = new ();

        public unsafe static T* Malloc<T>(Allocator allocator) where T : unmanaged {
            return (T*)UnsafeUtility.Malloc(sizeof(T), UnsafeUtility.AlignOf<T>(), allocator);
        }
        public static int AlignOf(Type type)
        {
            if (cachedAligns.ContainsKey(type)) {
                return cachedAligns[type];
            }
            var result = 1;
            
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                if (fieldType.IsPrimitive)
                {
                    result = Math.Max(result, Marshal.SizeOf(fieldType));
                }
            }
            cachedAligns.TryAdd(type, result);
            return result;
        }
    }
}
