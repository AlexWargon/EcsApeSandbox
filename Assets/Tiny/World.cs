using System;
using System.Collections.Generic;

namespace Wargon.TinyEcs
{
    public partial class World {
        private Dictionary<int, IPool> pools;
        private ISystem[] Systems;
        private int systemsCount;
        public World() {
            pools = new Dictionary<int, IPool>();
            Entities = new Entity[128];
            Systems = new ISystem[8];
        }

        
        public Pool<T> GetPool<T>() {
            var key = ComponentMeta.GetIndex<T>();
            if (pools.TryGetValue(key, out var pool)) {
                return (Pool<T>)pool;
            }

            var pl = new Pool<T>(32);
            pools.Add(key, pl);
            return pl;
        }

        public IPool GetPoolByIndex(int index) {
            return pools[index];
        }
        public float DeltaTime;

        public void Update(float deltaTime) {
            DeltaTime = deltaTime;
            for (int i = 0; i < systemsCount; i++) {
                Systems[i].OnUpdate(this);
            }
        }
    }


    public partial class World {
        private Entity[] Entities;
        private Dictionary<int, Archetype> _archetypes;
        private int entntiesCount;

        internal void AddArchetype(Archetype archetype) {
            _archetypes.Add(archetype.id, archetype);
        }
        public Entity CreateEntity() {
            Entity e;
            e.Index = entntiesCount;
            e.world = this;
            e.Archetype = _archetypes[0];
            Entities[entntiesCount] = e;
            entntiesCount++;
            return e;
        }

        public ref Entity GetEntity(int index) => ref Entities[index];
    }
    
    public partial class World {
        internal void AddDirtyQuery(Query query) {
            dirtyQ.Add(query);
        }

        private ArrayList<Query> dirtyQ;
        private Query[] Queries;
        internal int QueriesCount;
        internal Query[] GetQueries() {
            return Queries;
        }
    }

    public partial class World {
        public void forEach<T1, T2>(Action<T1, T2> action) {
            
        }
    }
    public struct ComponentMeta {
        private static Dictionary<Guid, int> indexes;
        private static int lastIndex = 0;
        static ComponentMeta() {
            indexes = new Dictionary<Guid, int>();
        }

        public static int GetIndex<T>() {
            var key = typeof(T).GUID;
            if (indexes.TryGetValue(key, out int ind))
                return ind;
            indexes.Add(key, lastIndex);
            lastIndex++;
            return lastIndex - 1;
        }
    }
    public interface ISystem {
        void OnUpdate(World world);
    }

    sealed class SystemTest : ISystem {
        public void OnUpdate(World world) {
           world.forEach((int s, float b) => {
               
           });
        }
    }

    public interface IPool {
        bool Has(int e);
        bool Remove(int entity);
    }



    public ref struct Enumerator {
        public Query query;
        public int index;

        public Enumerator(Query query) {
            this.query = query;
            index = -1;
        }
        public bool MoveNext() {
            index++;
            return index < query.count;
        }

        public void Reset() {
            index = -1;
        }

        public ref Entity Current {

            get => ref query.GetEntity(index);
        }
    }

    internal struct Mask {
        public readonly int[] Types;
        public int Count;

        public Mask(int size) {
            Types = new int[size];
            Count = 0;
        }

        public Mask(HashSet<int> set) {
            Types = new int[set.Count];
            Count = 0;
            foreach (var i in set) Types[Count++] = i;
        }

        public Mask(ref Span<int> set) {
            Types = new int[set.Length];
            Count = 0;
            foreach (var i in set) Types[Count++] = i;
        }

        public void Add(int type) {
            Types[Count] = type;
            Count++;
        }

        public bool Contains(int value) {
            for (var i = 0; i < Count; i++)
                if (Types[i] == value)
                    return true;

            return false;
        }
    }
}
