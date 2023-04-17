namespace Wargon.TinyEcs {
    public class Migration {
        internal readonly int Archetype;
        internal readonly int ComponentType;
        internal readonly int Key;
        internal readonly ArrayList<Query> QueriesToAddEntity;
        internal readonly ArrayList<Query> QueriesToRemoveEntity;
        internal bool IsEmpty;


        internal Migration(int key, int componentType, int archetype) {
            Archetype = archetype;
            ComponentType = componentType;
            Key = key;
            QueriesToAddEntity = new ArrayList<Query>(1);
            QueriesToRemoveEntity = new ArrayList<Query>(1);
            IsEmpty = true;
        }

        public void Execute(int entity) {
            if (IsEmpty) return;
            for (var i = 0; i < QueriesToAddEntity.Count; i++) QueriesToAddEntity[i].OnAddWith(entity);

            for (var i = 0; i < QueriesToRemoveEntity.Count; i++) QueriesToRemoveEntity[i].OnRemoveWith(entity);
        }
        
        public bool HasQueryToAddEntity(Query query) {
            for (int i = 0; i < QueriesToAddEntity.Count; i++) {
                if (QueriesToAddEntity[i] == query)
                    return true;
            }

            return false;
        }

        public bool HasQueryToRemoveEntity(Query query) {
            for (int i = 0; i < QueriesToRemoveEntity.Count; i++) {
                if (QueriesToRemoveEntity[i] == query)
                    return true;
            }

            return false;
        }
    }
}