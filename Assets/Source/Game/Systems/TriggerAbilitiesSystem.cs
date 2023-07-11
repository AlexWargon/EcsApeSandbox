using Wargon.Ecsape;

namespace Rogue {
    /// <summary>
    /// тригер абилок, сразу не вешается OnTriggerAbilityEvent, т.к. абилки должны тригерится в зависимости от их типа тригера
    /// полный путь евентов - Пуля критует, вешает на Овнера OnCritEvent.
    /// Пушка вешает на все абилки OnCritEvent,
    /// на абилки с [OnCritAbility, OnCritEvent] вешаем OnTriggerAbilityEvent.
    /// Все абилки с [OnCritAbility, OnTriggerAbilityEvent] сработают
    /// 
    /// </summary>
    sealed class TriggerAbilitiesSystem : ISystem {
        
        private Query onHitAbilitiesQuery;
        private Query onShotAbilitiesQuery;
        private Query onDamageAbilitiesQuery;
        private Query onKillAbilitiesQuery;
        private Query onCritAbilitiesQuery;
        private Query onGetDamageAbilitiesQuery;
        public void OnCreate(World world) {
            onHitAbilitiesQuery = world.GetQuery().WithAll<OnHitAbility, OnHitPhysicsEvent>();
            onShotAbilitiesQuery = world.GetQuery().WithAll<OnAttackAbility, OnAttackEvent>();
            onDamageAbilitiesQuery = world.GetQuery().WithAll<OnDamageAbility, OnHitWithDamageEvent>();
            onKillAbilitiesQuery = world.GetQuery().WithAll<OnKillAbility, OnKillEvent>();
            onCritAbilitiesQuery = world.GetQuery().WithAll<OnCritAbility, OnCritEvent>();
            onGetDamageAbilitiesQuery = world.GetQuery().WithAll<OnTakeDamageAbility, OnTakeDamageEvent>();
        }
        
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in onHitAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
            foreach (ref var entity in onShotAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
            foreach (ref var entity in onDamageAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
            foreach (ref var entity in onKillAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
            foreach (ref var entity in onCritAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
            foreach (ref var entity in onGetDamageAbilitiesQuery) {
                entity.Add<OnTriggerAbilityEvent>();
            }
        }
    }
}