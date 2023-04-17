using System.Collections.Generic;

namespace Wargon.TinyEcs {
    public class Pool<T> : IPool {
        private Dictionary<int, T> buffer;
        public int Count => buffer.Count;
        public Pool(int size) {
            buffer = new Dictionary<int, T>(size);
        }

        public void Add(int index, T comp) {
            buffer.TryAdd(index, comp);
        }

        public bool Remove(int entity) {
            return buffer.Remove(entity);
        }

        public bool Has(int entity) {
            return buffer.ContainsKey(entity);
        }

        public T Get(int entity) {
            return buffer[entity];
        }
    }
}