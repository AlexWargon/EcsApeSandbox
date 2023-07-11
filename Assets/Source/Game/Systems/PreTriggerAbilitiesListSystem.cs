using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    sealed class PreTriggerAbilitiesListSystem : ISystem, IClearBeforeUpdate<OnTriggerAbilityEvent> {
        private Query OnShotEvents;
        private Query OnHitEvents;
        private Query OnDamageEvents;
        private Query OnKillEvents;
        private Query OnCritEvents;
        private Query OnTakeDamageEvents;
        private IPool<AbilityList> abilities;
        private IPool<CurrentWeapons> currentWeapons;
        public void OnCreate(World world) {
            
            OnShotEvents = world.GetQuery().WithAll<OnAttackEvent, AbilityList, CurrentWeapons>();
            OnHitEvents = world.GetQuery().WithAll<OnHitPhysicsEvent, AbilityList, CurrentWeapons>();
            OnDamageEvents = world.GetQuery().WithAll<OnHitWithDamageEvent, AbilityList, CurrentWeapons>();
            OnKillEvents = world.GetQuery().WithAll<OnKillEvent, AbilityList, CurrentWeapons>();
            OnCritEvents = world.GetQuery().WithAll<OnCritEvent, AbilityList, CurrentWeapons>();
            OnTakeDamageEvents = world.GetQuery().WithAll<OnTakeDamageEvent, AbilityList, CurrentWeapons>();
        }

        private void Pretrigger<TEvent>(Query query) where TEvent : struct,IComponent {
            if (!query.IsEmpty) {
                foreach (ref var entity in query) {
                    ref var list = ref abilities.Get(ref entity);
                    ref var weapon = ref currentWeapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Entity.Add<TEvent>();
                    }
                    for (var i = 0; i < list.AbilityEntities.Count; i++) {
                        list.AbilityEntities[i].Add<TEvent>();
                    }
                }
            }
        }
        public void OnUpdate(float deltaTime) {
            Pretrigger<OnAttackEvent>(OnShotEvents);
            Pretrigger<OnHitPhysicsEvent>(OnHitEvents);
            Pretrigger<OnHitWithDamageEvent>(OnDamageEvents);
            Pretrigger<OnKillEvent>(OnKillEvents);
            Pretrigger<OnCritEvent>(OnCritEvents);
            Pretrigger<OnTakeDamageEvent>(OnTakeDamageEvents);
        }
    }
}