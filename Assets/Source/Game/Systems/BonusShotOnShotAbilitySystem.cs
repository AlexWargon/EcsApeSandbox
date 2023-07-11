using Wargon.Ecsape;

namespace Rogue {
    sealed class BonusShotOnShotAbilitySystem : ISystem {
        private Query Query;
        private IEntityFabric Fabric;
        public void OnCreate(World world) {
            Query = world.GetQuery().WithAll<BonusShotAbility, OnTriggerAbilityEvent>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var bonus = ref entity.Get<BonusShotAbility>();
                ref var weapon = ref entity.GetOwner().Get<SpreadWeapon>();
                Fabric.Instantiate(bonus.Shot, weapon.firePoint.position, weapon.Spread());
            }
        }
    }
}