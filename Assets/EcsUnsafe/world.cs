using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace EcsUnsafe
{
    public unsafe struct world {
        private vec<entity>* entities;
        private int entitiesCount;
        private IntPtr* poolsPtrs;
        private vec<GCHandle>* Handles;
        private int handlesCount;
        private world* this_ptr;
        
        public static world* create() {
            components.init();
            var w = allocator.alloc<world>();
            w->entities = vec<entity>.create(256);
            w->Handles = vec<GCHandle>.create(256);
            //w->create_pools();
            w->this_ptr = w;
            return w;
        }

        public pool<T>* get_pool<T>() where T : struct {
            return (pool<T>*)poolsPtrs[components.get_index<T>()];
        }
        
        private void create_pools() {
            poolsPtrs = (IntPtr*)allocator.alloc(typeof(IntPtr),256);
            for (int i = 0; i < components.amount; i++) {
                poolsPtrs[i] = (IntPtr)pool_fabric.create_pool(components.types[i], 256);
            }
        }

        internal void add_handle(GCHandle handle) {
            Handles->set(handlesCount++,handle);
        }

        public entity* create_entity() {
            entity e;
            e.index = entitiesCount;
            e.world = this_ptr;
            entities->set(entitiesCount, e);
            return &e;
        }

        public entity* get_entity(int index) {
            var e = entities->get(index);
            return &e;
        }
    }

    public unsafe static class allocator {
        public static int size_of<T>() where T : struct {
            return Marshal.SizeOf(typeof(T));
        }

        public static int size_of(Type type) {
            return Marshal.SizeOf(type);
        }
        public static void** alloc_array_of_pointers(int amount){
            return (void**)Marshal.AllocHGlobal(sizeof(void*)* amount);
        }
        public static void* alloc(int amount){
            return (void*)Marshal.AllocHGlobal(amount);
        }
        public static T* alloc<T>() where T : unmanaged {
            return (T*)Marshal.AllocHGlobal(size_of<T>());
        }

        public static void* alloc<T>(int amount) where T : struct {
            return (void*)Marshal.AllocHGlobal(size_of<T>()*amount);
        }
        public static void* alloc(Type type){
            return (void*)Marshal.AllocHGlobal(size_of(type));
        }
        public static void* alloc(Type type,int amount){
            return (void*)Marshal.AllocHGlobal(size_of(type)*amount);
        }

        public static void* get_ptr(object item) {
            TypedReference tr = __makeref(item);
            IntPtr ptr = **(IntPtr**)(&tr);
            return (void*)ptr;
        }
    }

    public unsafe struct vec_of_ptr<T> where T : unmanaged {
        public void* buffer;
        
        public static vec<T>* create(int size) {
            var v = allocator.alloc<vec<T>>();
            v->buffer = allocator.alloc<T>(size);
            return v;
        }
    }
    public static class Generic {
        public static object New(Type genericType, Type elementsType, params object[] parameters) {
            return Activator.CreateInstance(genericType.MakeGenericType(elementsType), parameters);
        }
        public static object New(Type genericType, Type elementsType) {
            return Activator.CreateInstance(genericType.MakeGenericType(elementsType));
        }
    }

    public unsafe struct vec {
        public static void* create(Type type,int size) {
            var buffer = allocator.alloc(type, size);
            var obj = Generic.New(typeof(vec<>), type);
            var ptr = allocator.get_ptr(obj);
            obj.GetType().GetMethod("set_data").Invoke(obj, new object[]{(IntPtr)buffer});
            return ptr;
        }
    }
    public unsafe struct vec<T> where T : struct {
        public void* buffer;

        public vec(int size) {
            buffer = allocator.alloc<T>(size);
        }
        public void set_data(void* data) {
            buffer = data;
        }
        public static vec<T>* create(int size = 16) {
            var v = allocator.alloc<vec<T>>();
            v->buffer = allocator.alloc<T>(size);
            return v;
        }
        public static void* create_non_generic(int size = 16) {
            var v = allocator.alloc<vec<T>>();
            v->buffer = allocator.alloc<T>(size);
            return v;
        }
        public ref T get(int idx) {
            return ref UnsafeUtility.ArrayElementAsRef<T>(buffer, idx);
        }

        public void set(int idx, T item) {
            UnsafeUtility.WriteArrayElement(buffer, idx, item);
        }
    }
    
    internal unsafe struct pool_fabric {
        
        public static pool<T>* create<T>(int size) where T : struct {
            var p = allocator.alloc<pool<T>>();
            p->buffer = vec<T>.create(size);
            p->entities = vec<int>.create(size);
            return p;
        }
        
        public static void* create(Type type,int size) {
            Debug.Log(type.Name);
            var buffer = allocator.alloc(type, size);
            var obj = Generic.New(typeof(vec<>), type);
            var ptr = allocator.get_ptr(obj);
            obj.GetType().GetMethod("set_data")?.Invoke(obj, new object[]{(IntPtr)buffer});
            return ptr;
        }
        public static void* create_pool(Type type,int size) {
            var buffer = create(type, size);
            var entities = vec<int>.create(size);
            var obj = Generic.New(typeof(pool<>), type);
            var ptr = allocator.get_ptr(obj);
            obj.GetType().GetMethod("set_entities")?.Invoke(obj, new object[]{(IntPtr)entities});
            obj.GetType().GetMethod("set_buffer")?.Invoke(obj, new object[]{(IntPtr)buffer});
            return ptr;
        }
    }
    public unsafe struct pool<T> where T: struct {
        public vec<T>* buffer;
        public vec<int>* entities;
        public int count;
        public static pool<T>* create(int size = 256) {
            var p = allocator.alloc<pool<T>>();
            p->buffer = vec<T>.create(size);
            p->entities = vec<int>.create(size);
            return p;
        }

        public void set_entities(vec<int>* entites) {
            this.entities = entites;
        }

        public void set_buffer(vec<T>* data) {
            buffer = data;
        }
        
        public ref T get(int entity) {
            return ref buffer->get(entities->get(entity));
        }

        public void add(int entity, T component) {
            entities->set(entity, count);
            buffer->set(count, component);
            count++;
        }
    }
    public interface icomponent{}
    
    public struct component<T> {
        public static int index;

        static component() {
            index = components.amount++;
        }
    }

    public struct components {
        public static int amount;
        private static bool inited;
        private static Dictionary<Type, int> indexes = new Dictionary<Type, int>();
        public static List<Type> types = new List<Type>();

        public static int get_index<T>() {
            return indexes[typeof(T)];
        }
        public static void init() {
            if(inited) return;
            var tp = FindAllTypeWithInterface(typeof(icomponent));
            
            foreach (var key in tp) {
                indexes.Add(key, amount);
                types.Add(key);
                amount++;
            }
            inited = true;
        }
        private static Type[] FindAllTypeWithInterface(Type interfaceType, Func<Type, bool> comprasion = null) {
            if(comprasion!=null) return  AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && comprasion(p) && p != interfaceType).ToArray();
            
            return  AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && p != interfaceType).ToArray();
        }
    }
    public unsafe struct entity {
        public int index;
        internal world* world;
    }

    public struct transform : icomponent {
        public Vector3 pos;
        public Vector3 size;
    }
}
