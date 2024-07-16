using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Wargon.NEZS {
    
    public struct ComponentType<T> where T : struct {
        private readonly static SharedStatic<ComponentType> shaderIndex = SharedStatic<ComponentType>.GetOrCreate<ComponentType<T>>();

        public static int Index {
            get {
                if (shaderIndex.Data.IsCreated == false) {
                    var type = new ComponentType(Interlocked.Increment(ref ComponentTypesCount.Value.Data),
                        UnsafeUtility.AlignOf<T>(), UnsafeUtility.SizeOf<T>());
                    shaderIndex.Data = type;
                    ComponentTypeMap.Add(type);
                    shaderIndex.Data.IsCreated = true;
                }
                return shaderIndex.Data.Index;
            }
        }

        public static ComponentType AsComponentType() => shaderIndex.Data;
        public int index;
        // static ComponentType() {
        //     shaderIndex = SharedStatic<ComponentType>.GetOrCreate<ComponentType<T>>();
        //     var type = new ComponentType(Interlocked.Increment(ref ComponentTypesCount.Value),
        //         UnsafeUtility.AlignOf<T>(), UnsafeUtility.SizeOf<T>());
        //     shaderIndex.Data = type;
        //     ComponentTypeMap.Add(type);
        // }
        public static implicit operator ComponentType(ComponentType<T> type)=> new ComponentType(ComponentTypesCount.Value.Data,
            UnsafeUtility.AlignOf<T>(), UnsafeUtility.SizeOf<T>());

        public static explicit operator ComponentType<T>(ComponentType type) =>
            new ComponentType<T> { index = type.Index };
    }
    [BurstCompile]
    public struct ComponentType : IEquatable<ComponentType> {
        public readonly int Align;
        public readonly int Index;
        public readonly int SizeInBytes;
        public bool IsCreated;
        public ComponentType(int index, int align, int sizeInBytes) {
            Index = index;
            Align = align;
            SizeInBytes = sizeInBytes;
            IsCreated = false;
        }
        [BurstCompile]
        public bool Equals(ComponentType other) {
            return other.Index == this.Index;
        }
        [BurstCompile]
        public override int GetHashCode() {
            return Index;
        }
    }
    public struct ComponentTypeMap {
        private readonly static UnsafeHashMap<ComponentType, int> mapTypeToIndex = new UnsafeHashMap<ComponentType, int>(32, World.Allocator);
        private readonly static UnsafeHashMap<int, ComponentType> mapIndexToNativeType = new UnsafeHashMap<int, ComponentType>(32, World.Allocator);
        public static ComponentType NativeType(int index) {
            return mapIndexToNativeType[index];
        }
        [BurstDiscard]
        public static void Add(ComponentType type) {
            var index = type.Index;
            mapTypeToIndex.Add(type, index);
            mapIndexToNativeType.Add(index, type);
        }
        [BurstDiscard]
        public static int Index(ComponentType type) {
            if (mapTypeToIndex.TryGetValue(type, out int index)) return index;
            index = type.Index;
            mapTypeToIndex.Add(type, index);
            mapIndexToNativeType.Add(index, type);
            return index;
        }
        [BurstDiscard]
        public static void Dispose() {
            mapIndexToNativeType.Dispose();
            mapTypeToIndex.Dispose();
        }
    }

    public struct ComponentTypesCount {
        public readonly static SharedStatic<int> Value = SharedStatic<int>.GetOrCreate<ComponentTypesCount>();
    }
}