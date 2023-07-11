using System;
using System.Collections.Generic;

namespace Wargon.TinyEcs {
    public sealed class Archetype {
        private readonly Dictionary<int, ArchetypeEdge> Edges;
        private readonly HashSet<int> hashMask;
        public readonly int id;
        private readonly ArrayList<Query> queries;
        private readonly World world;
        /// ebat' vlad sho eto
        private int _queriesCount;

        internal Archetype(World world) {
            hashMask = new HashSet<int>();
            queries = new ArrayList<Query>(3);
            Edges = new Dictionary<int, ArchetypeEdge>();
            id = 0;
            _queriesCount = 0;
            this.world = world;
        }

        internal Archetype(World world, HashSet<int> hashMaskSource, int archetypeId) {
            queries = new ArrayList<Query>(3);
            Edges = new Dictionary<int, ArchetypeEdge>();
            id = archetypeId;
            _queriesCount = 0;
            hashMask = hashMaskSource;
            this.world = world;
            var worldQueries = world.GetQueries();
            var count = world.QueriesCount;
            for (var i = 0; i < count; i++) FilterQuery(worldQueries[i]);
        }

        internal Archetype(World world, ref Span<int> maskSource, int archetypeId) {
            queries = new ArrayList<Query>(3);
            Edges = new Dictionary<int, ArchetypeEdge>();
            id = archetypeId;
            _queriesCount = 0;
            hashMask = new HashSet<int>();
            this.world = world;
            foreach (var i in maskSource) hashMask.Add(i);

            var worldQueries = world.GetQueries();
            var count = world.QueriesCount;
            for (var i = 0; i < count; i++) FilterQuery(worldQueries[i]);
        }

        internal void TransformAdd(ref Entity entity, int component) {
            if (Edges.TryGetValue(component, out var edge)) {
                RemoveEntity(entity.Index);
                entity.Archetype = edge.add;
                entity.Archetype.AddEntity(entity.Index);
            }

            var maskAdd = new HashSet<int>(hashMask);
            maskAdd.Add(component);
            var maskRemove = new HashSet<int>(hashMask);
            maskRemove.Remove(component);

            Edges.Add(component, new ArchetypeEdge {
                add = new Archetype(world, maskAdd, GetCustomHashCode(maskAdd)),
                remove = new Archetype(world, maskRemove, GetCustomHashCode(maskRemove))
            });
        }

        internal void TransformRemove(ref Entity entity, int component) {
            if (Edges.TryGetValue(component, out var edge)) {
                RemoveEntity(entity.Index);
                entity.Archetype = edge.remove;
                entity.Archetype.AddEntity(entity.Index);
            }

            var maskAdd = new HashSet<int>(hashMask);
            maskAdd.Add(component);
            var maskRemove = new HashSet<int>(hashMask);
            maskRemove.Remove(component);

            Edges.Add(component, new ArchetypeEdge {
                add = new Archetype(world, maskAdd, GetCustomHashCode(maskAdd)),
                remove = new Archetype(world, maskRemove, GetCustomHashCode(maskRemove))
            });
        }

        private static int GetCustomHashCode(HashSet<int> mask) {
            unchecked {
                const int p = 16777619;
                var hash = (int)2166136261;

                foreach (var i in mask) hash = (hash ^ i) * p;
                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        internal void AddEntity(int entityId) {
            for (var i = 0; i < _queriesCount; i++) queries[i].OnAddWith(entityId);
        }

        internal void RemoveEntity(int entityId) {
            for (var i = 0; i < _queriesCount; i++) queries[i].OnRemoveWith(entityId);
        }

        private void FilterQuery(Query query) {
            for (var q = 0; q < query.without.Count; q++) {
                var type = query.without.Types[q];
                if (hashMask.Contains(type)) return;
            }

            var checks = 0;
            for (var q = 0; q < query.with.Count; q++)
                if (hashMask.Contains(query.with.Types[q])) {
                    checks++;
                    if (checks == query.with.Count) {
                        queries.Add(query);
                        _queriesCount++;
                        break;
                    }
                }
        }

        internal void RemoveEntityFromPools(World world, int entity) {
            foreach (var i in hashMask) {
                var pool = world.GetPoolByIndex(i);
                if (pool.Has(entity))
                    pool.Remove(entity);
            }
        }

        public bool HasComponent(int index) {
            return hashMask.Contains(index);
        }
    }

    internal struct ArchetypeEdge {
        public Archetype add;
        public Archetype remove;
    }
}