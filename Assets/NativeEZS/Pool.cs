using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Wargon.NEZS {
    public unsafe struct Pool<T> : IPool where T : struct {
        [NativeDisableUnsafePtrRestriction] private Pool* buffer;
        public Pool(Pool* array) {
            buffer = array;
        }
        public ref T Get(int index) {
            return ref buffer->Get<T>(index);
        }
        public void Set(int index, in T value) {
            buffer->Set(index, in value);
        }
    }

    public unsafe struct Pool : IPool, IDisposable {
        [NativeDisableUnsafePtrRestriction]
        internal UntypedUnsafeArray* buffer;
        public bool IsCreated;
        public Pool(int capacity, Type type) {
            buffer = UntypedUnsafeArray.Create(capacity, type, World.Allocator);
            IsCreated = true;
        }
        public Pool(int capacity, ComponentType type) {
            buffer = UntypedUnsafeArray.Create(capacity, type, World.Allocator);
            IsCreated = true;
        }
        public ref T Get<T>(int index) where T : struct {
            return ref buffer->ElementAt<T>(index);
        }
        
        public void Set<T>(int index, in T value) where T : struct {
            if(buffer->capacity <= index) buffer->Resize(index + 16);
            buffer->ElementAt<T>(index) = value;
        }

        public Pool<T> AsTypedPool<T>() where T: struct {
            var ptr = (Pool*)UnsafeUtility.Malloc(sizeof(Pool), UnsafeUtility.AlignOf<Pool>(), World.Allocator);
            *ptr = this;
            return new Pool<T>(ptr);
        }
        public void Dispose() {
            UntypedUnsafeArray.Destroy(buffer, World.Allocator);
            IsCreated = false;
        }
    }

    public unsafe struct UntypedUnsafeArray : IDisposable {
        [NativeDisableUnsafePtrRestriction]
        public void* buffer;
        public int typeSize;
        public int alignOf;
        public int capacity;
        [BurstDiscard]
        public static UntypedUnsafeArray* Create(int capacity, Type type, Allocator allocator) {
            var ptr = (UntypedUnsafeArray*)UnsafeUtility.Malloc(sizeof(UntypedUnsafeArray), UnsafeUtility.AlignOf<UntypedUnsafeArray>(), allocator);
            *ptr = new UntypedUnsafeArray(capacity, type, allocator);
            return ptr;
        }
        [BurstDiscard]
        public static UntypedUnsafeArray* Create(int capacity, ComponentType type, Allocator allocator) {
            var ptr = (UntypedUnsafeArray*)UnsafeUtility.Malloc(sizeof(UntypedUnsafeArray), UnsafeUtility.AlignOf<UntypedUnsafeArray>(), allocator);
            *ptr = new UntypedUnsafeArray(capacity, type, allocator);
            return ptr;
        }
        public UntypedUnsafeArray(int size, Type type, Allocator allocator) {
            capacity = size;
            typeSize = UnsafeUtility.SizeOf(type);
            alignOf = UnsafeHelper.AlignOf(type);
            buffer = UnsafeUtility.Malloc(typeSize * capacity, alignOf, allocator);
        }
        public UntypedUnsafeArray(int size, ComponentType type, Allocator allocator) {
            capacity = size;
            typeSize = type.SizeInBytes;
            alignOf = type.Align;
            buffer = UnsafeUtility.Malloc(typeSize * capacity, alignOf, allocator);
        }
        public static void Destroy(UntypedUnsafeArray* array, Allocator allocator) {
            array->Dispose();
            UnsafeUtility.Free(array, allocator);
        }

        public ref T ElementAt<T>(int index) where T: struct {
            if (index >= capacity) throw new IndexOutOfRangeException();
            return ref UnsafeUtility.ArrayElementAsRef<T>(buffer, index);
        }
        public void Resize(int newCapacity) {
            void* newBuffer = null;
            newBuffer = UnsafeUtility.Malloc(typeSize * newCapacity, alignOf, World.Allocator);

            if (buffer != null && newCapacity > 0)
            {
                var itemsToCopy = math.min(newCapacity, capacity);
                var bytesToCopy = itemsToCopy * typeSize;
                UnsafeUtility.MemCpy(newBuffer, buffer, bytesToCopy);
            }
            UnsafeUtility.Free(buffer, World.Allocator);
            buffer = newBuffer;
            capacity = newCapacity;
        }

        public void Dispose() {
            UnsafeUtility.Free(buffer, World.Allocator);
        }
    }
}