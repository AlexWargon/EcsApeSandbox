using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;

namespace Rogue {


    public sealed class WeaponsGroup : Systems.Group {
        public WeaponsGroup() : base() {
            
             Add<SpawnPrefabsOnShot>()
            .Add<PlayerAttackSystem>()
;
            
        }
    }
    sealed class CooldownSystem : ISystem {
        public void OnCreate(World world) {
            query = world.GetQuery().With<Cooldown>();
        }

        private IPool<Cooldown> cooldown;
        private Query query;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var c = ref cooldown.Get(ref entity);
                if (c.Value > 0F) {
                    c.Value -= deltaTime;
                    continue;
                }
                //Debug.Log(c.Value);
                entity.Remove<Cooldown>();
            }
        }
    }
   

    sealed class SpawnPrefabsOnShot : ISystem {
        private Query Query;
        private IPool<SpreadWeapon> weapons;
        private IEntityFabric fabric;
        public void OnCreate(World world) {
            Query = world.GetQuery().WithAll<SpreadWeapon, OnAttackEvent>().Without<Cooldown>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var weapon = ref weapons.Get(ref entity);
                fabric.Instantiate(weapon.flash, weapon.firePoint.position, weapon.firePoint.rotation);
                for (int i = 0; i < weapon.count; i++) {
                    fabric.Instantiate(weapon.projectile, weapon.firePoint.position, weapon.Spread())
                        .SetOwner(entity.GetOwner());
                }
                entity.Add(new Cooldown{Value = weapon.delay});
            }
        }
    }
    sealed class WeaponSystem : ISystem, IClearBeforeUpdate<OnAttackEvent> {
        private Query query;
        private IPool<SpreadWeapon> weapons;
        private IPool<InputData> inputs;
        private IPool<WeaponAnimation> animations;
        private IPool<SpriteRender> sprites;
        private IEntityFabric fabric;
        public void OnCreate(World world) {
            query = world.GetQuery()
                .WithAll(typeof(SpreadWeapon), typeof(InputData), typeof(WeaponAnimation), typeof(SpriteRender))
                .Without<Dead>();
        }

        public void OnUpdate(float deltaTime) {

            foreach (ref var entity in query) {
                ref var weapon = ref weapons.Get(ref entity);
                ref var input = ref inputs.Get(ref entity);

                if (input.fire) {
                    
                    // ref var animation = ref animations.Get(ref entity);
                    // ref var sprite = ref sprites.Get(ref entity);
                    // animation.reverse = !animation.reverse;
                    // var reverse = animation.reverse && !sprite.flipX || animation.reverse && sprite.flipX;
                    // if (reverse) {
                    //     animation.Animator.Play("StaffAttack");
                    // }
                    // else {
                    //     animation.Animator.Play("StaffAttackReverse");
                    // }

                    fabric.Instantiate(weapon.flash, weapon.firePoint.position, weapon.firePoint.rotation);
                    for (int i = 0; i < weapon.count; i++) {
                        fabric.Instantiate(weapon.projectile, weapon.firePoint.position, weapon.Spread())
                            .SetOwner(entity);
                    }

                    entity.Add<OnAttackEvent>();
                }
            }
        }
    }
}