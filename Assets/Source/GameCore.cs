using System;
using System.Collections.Generic;
using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.UI;

namespace Rogue {
    [DefaultExecutionOrder(ExecutionOrder.EcsMain)]
    public class GameCore : WorldHolder
    {
        private Systems systems;
        private IUIService uiService;
        private IObjectPool objectPool;
        private IEntityFabric fabric;
        void Awake()
        {
            World.ENTITIES_CACHE = 32;
            DI.Build(this);
            world = World.GetOrCreate();
            fabric = new EntityFabric().Init(world);
            DI.Register<IEntityFabric>().From(fabric);
            systems = new Systems(world)
                .AddInjector(DI.GetOrCreateContainer())
                .Add<ConvertEntitySystem>()
                
                .Add<Rogue.PreTriggerAbilitiesListSystem>()
                .Add<Rogue.TriggerAbilitiesSystem>()
                .Add<Rogue.BonusShotOnShotAbilitySystem>()
                .Add<PlayerInputSystem>()
                .Add<WeaponSystem>(new ())
                .Add<ProjectileMoveSystem>()
                .Add<MoveSystem>()
                .Add<WeaponRotationSystem>()
                .Add<PlayerAnimationSystem>()
                .Add<TestPlayerAnimationSystem>()
                .Add<Animation2DSystem>()
                .Add<TweensTestSystem>()
                //.Add<ProjectileCollisionSystem>()


                .Add<PoolObjectLifeTimeSystem>()
                .Add<PoolObjectReleaseSystem>()
                .Add<OnTriggerSystem>()
                .Add<SyncTransformsSystem>()

                
            .Init();

        }

        void Update()
        {
            systems.Update(Time.deltaTime);
        }
    }

    public struct CurrentWeapons : IComponent , IOnCreate{
        public List<Entity> Entities;
        public void OnCreate() {
            Entities = new List<Entity>();
        }
    }
    public struct AbilityList : IComponent, IOnCreate {
        public List<Entity> AbilityEntities;
        public void OnCreate() {
            AbilityEntities = new List<Entity>();
        }
    }

    public struct OnKillEvent : IComponent {
        public Entity killedTarget;
    }

    public struct OnHitEvent : IComponent {
        public Vector2 Position;
        public int Damage;
    }

    public struct OnTakeDamageEvent : IComponent {}
    public struct OnDamageEvent : IComponent { }
    public struct OnCritEvent : IComponent { }
    public struct OnShotAbility : IComponent { }
    public struct OnHitAbility : IComponent { }
    public struct OnDamageAbility : IComponent { }
    public struct OnKillAbility : IComponent { }
    public struct OnCritAbility : IComponent { }
    public struct OnTriggerAbilityEvent : IComponent {}
    public struct OnTakeDamageAbility : IComponent{}
    
    sealed class PreTriggerAbilitiesListSystem : ISystem {
        private Query OnShotEvents;
        private Query OnHitEvents;
        private Query OnDamageEvents;
        private Query OnKillEvents;
        private Query OnCritEvents;
        private Query OnTakeDamageEvents;
        private IPool<AbilityList> abilities;
        private IPool<CurrentWeapons> weapons;
        public void OnCreate(World world) {
            OnShotEvents = world.GetQuery().WithAll<ShotEvent, AbilityList, CurrentWeapons>();
            OnHitEvents = world.GetQuery().WithAll<OnHitEvent, AbilityList, CurrentWeapons>();
            OnDamageEvents = world.GetQuery().WithAll<OnDamageEvent, AbilityList, CurrentWeapons>();
            OnKillEvents = world.GetQuery().WithAll<OnKillEvent, AbilityList, CurrentWeapons>();
            OnCritEvents = world.GetQuery().WithAll<OnCritEvent, AbilityList, CurrentWeapons>();
            OnTakeDamageEvents = world.GetQuery().WithAll<OnTakeDamageEvent, AbilityList, CurrentWeapons>();
        }
        public void OnUpdate(float deltaTime) {
            if(!OnShotEvents.IsEmpty)
                foreach (ref var entity in OnShotEvents) {
                    ref var list = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<ShotEvent>();
                    }
                    for (var i = 0; i < list.AbilityEntities.Count; i++) {
                        list.AbilityEntities[i].Add<ShotEvent>();
                    }
                }
            if(!OnHitEvents.IsEmpty)
                foreach (ref var entity in OnHitEvents) {
                    ref var mod = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<OnHitEvent>();
                    }
                    for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                        mod.AbilityEntities[i].Add<OnHitEvent>();
                    }
                }
            if(!OnDamageEvents.IsEmpty)
                foreach (ref var entity in OnDamageEvents) {
                    ref var mod = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<OnDamageEvent>();
                    }
                    for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                        mod.AbilityEntities[i].Add<OnDamageEvent>();
                    }
                }
            if(!OnKillEvents.IsEmpty)
                foreach (ref var entity in OnKillEvents) {
                    ref var mod = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<OnKillEvent>();
                    }
                    for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                        mod.AbilityEntities[i].Add<OnKillEvent>();
                    }
                }
            if(!OnCritEvents.IsEmpty)
                foreach (ref var entity in OnCritEvents) {
                    ref var mod = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<OnCritEvent>();
                    }
                    for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                        mod.AbilityEntities[i].Add<OnCritEvent>();
                    }
                }
            if(!OnTakeDamageEvents.IsEmpty)
                foreach (ref var entity in OnTakeDamageEvents) {
                    ref var mod = ref abilities.Get(ref entity);
                    ref var weapon = ref weapons.Get(ref entity);
                    for (var i = 0; i < weapon.Entities.Count; i++) {
                        weapon.Entities[i].Add<OnTakeDamageEvent>();
                    }
                    for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                        mod.AbilityEntities[i].Add<OnTakeDamageEvent>();
                    }
                }
        }
    }
    /// <summary>
    /// тригер абилок, сразу не вешается OnTriggerAbilityEvent, т.к. абилки должны тригерится в зависимости от их типа тригера
    /// полный путь евентов - Пуля критует, вешает на Овнера OnCritEvent.
    /// Пушка вешает на все абилки OnCritEvent,
    /// на абилки с [OnCritAbility, OnCritEvent] вешаем OnTriggerAbilityEvent.
    /// Все абилки с [OnCritAbility, OnTriggerAbilityEvent] сработают
    /// 
    /// </summary>
    sealed class TriggerAbilitiesSystem : ISystem, IClearBeforeUpdate<OnTriggerAbilityEvent> {
        
        private Query onHitAbilitiesQuery;
        private Query onShotAbilitiesQuery;
        private Query onDamageAbilitiesQuery;
        private Query onKillAbilitiesQuery;
        private Query onCritAbilitiesQuery;
        private Query onGetDamageAbilitiesQuery;
        public void OnCreate(World world) {
            onHitAbilitiesQuery = world.GetQuery().WithAll<OnHitAbility, OnHitEvent>();
            onShotAbilitiesQuery = world.GetQuery().WithAll<OnShotAbility, ShotEvent>();
            onDamageAbilitiesQuery = world.GetQuery().WithAll<OnDamageAbility, OnDamageEvent>();
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

    public struct BonusShotAbility : IComponent {
        public EntityLink Shot;
        public int Amount;
    }
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

    sealed class ShotOnHitAbilitySystem : ISystem {
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
    public sealed class AbilitiesFabric {
        private AbilitiesSO abilities;
        private Dictionary<string, EntityLink> abilitiesMap;
        private World world;

        public AbilitiesFabric Init() {
            abilitiesMap = new Dictionary<string, EntityLink>();
            foreach (var entityLink in abilities.List) {
                abilitiesMap.Add(entityLink.name, entityLink);
            }
            return this;
        }
        public Entity GetRandom() {
            var e = World.GetOrCreate().CreateEntity();
            abilities.List.RandomElement().LinkFast(e);
            return e;
        }

        public List<EntityLink> GetAll() => abilities.List;

        public Entity Get(string name) {
            if (!abilitiesMap.TryGetValue(name, out EntityLink link)) {
                throw new Exception($"No ability with name {name}");
            }
            var e = World.GetOrCreate().CreateEntity();
            link.LinkFast(e);
            return e;
        }
    }
    
    sealed class PlayerInputSystem : ISystem {
        private IPool<InputData> inputs;
        private Query query;
        public void OnCreate(World world) {
            query = world.GetQuery().With<InputData>();
        }

        public void OnUpdate(float deltaTime) {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");
            var r = Input.GetKey(KeyCode.R);

            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                input.horizontal = h;
                input.vertical = v;
                input.fire = r;
            }
        }
    }
    
    sealed class MoveSystem : ISystem {
        Query query;
        IPool<InputData> inputs;
        IPool<Translation> translations;
        IPool<MoveSpeed> moveSpeeds;
        IPool<SpriteRender> spriteRender;
        public void OnCreate(World world) {
            query = world.GetQuery().WithAll<InputData,Translation,MoveSpeed, SpriteRender>();
            inputs = world.GetPool<InputData>();
            translations = world.GetPool<Translation>();
            moveSpeeds = world.GetPool<MoveSpeed>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                ref var translation = ref translations.Get(ref entity);
                ref var moveSpeed = ref moveSpeeds.Get(ref entity);
        
                translation.position += new Vector3(input.horizontal, input.vertical) * deltaTime * moveSpeed.value;
                ref var render = ref spriteRender.Get(ref entity);
                render.flipX = input.horizontal < 0F;

            }
        }
    }
    
    sealed class PlayerAnimationSystem : ISystem {
        private IPool<SpriteAnimation> animations;
        private IPool<InputData> inputs;
        private Query query;
        public void OnCreate(World world) {
            query = world.GetQuery().WithAll<SpriteAnimation,InputData>();
        }
    
        public void OnUpdate(float deltaTime) {
        
            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                ref var animation = ref animations.Get(ref entity);

                if (input.horizontal < .1F && input.vertical < .1F && input.horizontal > -.1F && input.vertical > -.1F)
                    animation.Play(Animations.Idle);
                else 
                    animation.Play(Animations.Run);
                if (input.fire) {
                    animation.Play(Animations.Run,4);
                }
            }
        }
    }

    public struct WeaponParent : IComponent {
        public Transform Transform;
        public Vector3 difference;
        public SpriteRenderer weaponRender;
    }
    
    sealed class WeaponRotationSystem : ISystem {
        private Camera Camera;
        private readonly Vector3 right = new Vector3(1, 1, 1);
        private readonly Vector3 left = new Vector3(1, -1, 1);
        private IPool<SpriteRender> spriteRenders;
        private IPool<WeaponParent> weaponParents;
        private Query Query;
        
        public void OnCreate(World world) {
            Query = world.GetQuery().WithAll(typeof(SpriteRender), typeof(WeaponParent));
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var weaponParent = ref weaponParents.Get(ref entity);
                ref var render = ref spriteRenders.Get(ref entity);
                weaponParent.difference = MousePosition(Camera) - weaponParent.Transform.position;
                weaponParent.difference.Normalize();

                float rotZ = Mathf.Atan2(weaponParent.difference.y, weaponParent.difference.x) * Mathf.Rad2Deg;
                weaponParent.Transform.rotation = Quaternion.Slerp(weaponParent.Transform.rotation,
                    Quaternion.Euler(0, 0, rotZ), 1.4f);
                SetSide(rotZ, weaponParent.weaponRender, render.value, weaponParent.Transform);
            }
        }
        
        private void SetSide(float rotZ, SpriteRenderer weapon, SpriteRenderer ownder, Transform transform)
        {

            if (rotZ > 0)
            {
                var pos = weapon.transform.localPosition;
                pos.z = 1f;
                weapon.transform.localPosition = pos;
            }
            if (rotZ < 0)
            {
                var pos = weapon.transform.localPosition;
                pos.z = -1f;
                weapon.transform.localPosition = pos;
            }

            if (rotZ is < 90 and > -90)
            {
                transform.localScale = right;
                ownder.flipX = false;
            }

            else
            {
                transform.localScale = left;
                ownder.flipX = true;
            }
        }
        private static readonly Vector3 Offset = new Vector3(0, 0, 10);

        private static Vector3 MousePosition(Camera camera)
        {
            return camera.ScreenToWorldPoint(Input.mousePosition) + Offset;
        }
    }
    
    sealed class WeaponSystem : ISystem, IClearBeforeUpdate<ShotEvent> {
        private Query query;
        private IPool<SpreadWeapon> weapons;
        private IPool<InputData> inputs;
        private IPool<WeaponAnimation> animations;
        private IEntityFabric fabric;
        public void OnCreate(World world) {
            query = world.GetQuery()
                .WithAll(typeof(SpreadWeapon), typeof(InputData), typeof(WeaponAnimation))
                .Without<Dead>();
        }

        public void OnUpdate(float deltaTime) {

            
            foreach (ref var entity in query) {
                ref var weapon = ref weapons.Get(ref entity);
                ref var input = ref inputs.Get(ref entity);
                ref var animation = ref this.animations.Get(ref entity);
                entity.Add<SpreadWeapon, InputData>();
                if (input.fire) {
                    if (weapon.delayCounter > weapon.delay) {

                        fabric.Instantiate(weapon.flash, weapon.firePoint.position, weapon.firePoint.rotation);
                        //flash.Get<Translation>().scale = Extensions.Random(0.8f, 1.8f);
                        //flash.Get<Translation>().rotation = Extensions.RandomZ(0f, 90f);
                        for (int i = 0; i < weapon.count; i++) {
                            fabric.Instantiate(weapon.projectile, weapon.firePoint.position, weapon.Spread())
                                .SetOwner(entity);
                        }
                        weapon.delayCounter = 0;
                        entity.Add<ShotEvent>();
                    }
                }
                weapon.delayCounter += deltaTime;
            }
        }
    }
    
    public struct WeaponAnimation : IComponent {
        public Animation value;
    }
}

        