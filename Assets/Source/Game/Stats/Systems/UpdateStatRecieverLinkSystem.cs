using Wargon.Ecsape;

namespace Rogue {
    public class UpdateStatRecieverLinkSystem : ISystem, IOnCreate {
        
        private Query _changeStatsRequests;
        private Query _removeStatsRequests;
        private IPool<StatsChangedRequest> _statsChangedRequests;
        private IPool<StatsRemovedRequest> _statsRemovedRequests;
        public void OnCreate(World world) {
            _changeStatsRequests = world.GetQuery().With<StatsChangedRequest>();
            _removeStatsRequests = world.GetQuery().With<StatsRemovedRequest>();
        }
        public void OnUpdate(float deltaTime) {
            ProcessChangeStatRequests();
            ProcessRemoveStatRequests();
        }
        private void ProcessChangeStatRequests()
        {
            foreach (ref var e in _changeStatsRequests) {
                ref var request = ref _statsChangedRequests.Get(ref e);
                request.StatContext.SetStatsChanged(request.StatReceiver);
                e.Destroy();
            }
        }
        
        private void ProcessRemoveStatRequests()
        {
            foreach (ref var e in _removeStatsRequests) {
                ref var request = ref _statsRemovedRequests.Get(ref e);
                request.StatContext.SetStatsRemoved();
                e.Destroy();
            }
        }
        
    }

    public static class EntityStatsExtensions {
        public static void SetStatsChanged(this ref Entity statContextEntity, Entity statReceiverEntity) {
            if(statContextEntity.Has<StatEntities>()) return;

            ref var statEntities = ref statContextEntity.Get<StatEntities>();
            for (var i = 0; i < statEntities.Value.Count; i++) {
                var statEntity = statEntities.Value[i];
                statEntity.Add<StatsChangedEvent>();

                var statReceiverLink = new StatReceiverLink() { Value = statReceiverEntity };
                
                if(statContextEntity.Has<StatReceiverLink>())
                    statEntity.Set(statReceiverLink);
                else
                    statEntity.Add(statReceiverLink);
            }
        }
        
        public static void SetStatsRemoved(this ref Entity statContextEntity)
        {
            if (!statContextEntity.Has<StatEntities>()) return;
            ref var statEntities = ref statContextEntity.Get<StatEntities>();

            for (int i = 0; i < statEntities.Value.Count; i++)
            {
                var statEntity = statEntities.Value[i];
                statEntity.Add<StatsRemovedEvent>();
                
            }
        }
    }
}