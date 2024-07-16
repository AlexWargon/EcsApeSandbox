using System;
using System.Collections.Generic;
using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;
using Wargon.Ecsape.Tween;
using Wargon.UI;
using Random = UnityEngine.Random;

namespace Rogue {
    public static class GameStaticData {
        public static string MainWorldName;
        public static World MainWorld => World.GetOrCreate(MainWorldName);
    }
    [DefaultExecutionOrder(ExecutionOrder.EcsMain)] 
    public class GameCore : WorldHolder, IPauseble {
        [SerializeField] private EntityLink Player;
        private IUIService uiService;
        private IObjectPool objectPool;
        private LootServise _lootServise;
        private InventoryService _inventoryService;
        private SaveService _saveService;
        private const string TRIGGERS = "TRIGGERS;";
        private const string DAMAGE = "DAMAGE;";
        void Awake()
        {
            DI.Build(this);
            world = World.Default;
            GameStaticData.MainWorldName = world.Name;
            _lootServise.SetWorld(world);
            DI.Register<IEntityFabric>().From(new EntityFabric().Init(world));
            
            world
                //.Add<ConvertEntitySystem>()
                .Add<CooldownSystem>()
                .Add<PlayerInputSystem>()
                .Add<PlayerAttackSystem>()
                .AddGroup(new EnemyGroup())
                
                .AddGroup(new Systems.Group(TRIGGERS)
                    .Add<PreTriggerAbilitiesListSystem>()
                    .Add<PreTriggerEquipmentListSystem>()
                    .Add<TriggerEquipmentListSystem>()
                    .Add<TriggerAbilitiesSystem>()
                    .Add<BonusShotOnShotAbilitySystem>()
                    .Add<TriggeredOnAttackSystem>()
                    .Add<TriggeredAttackOnHitSystem>()
                    .Add<TriggerVampireSystem>())
                
                .Add<PlayerSearchTargetSystem>()
                .Add<SearchTargetSystem>()
                .Add<SpawnPrefabsOnShot>()
                .AddGroup(new Systems.Group(DAMAGE)
                    .Add<HitBoxCollisionSystem>()
                    .Add<DamageEntitiesSystem>()
                    .Add<HealSystem>()
                    .Add<ShowDamageSystem>()
                    .Add<ReactOnTakingHitSystem>())
                
                .Add<ProjectileMoveSystem>()
                .Add<MoveSystem>()
                .Add<PlayerMoveSystem>()
                .Add<WeaponRotationSystem>()
                .Add<PlayerAnimationSystem>()
                .Add<AnimationSystem>()
                .Add<Animation2DSystem>()
                .Add<OnPlayerSpawnSystem>()
                .Add<PoolObjectLifeTimeSystem>()
                .Add<TotalLifeTimeSystem>()
                //.Add<PoolObjectLifeTimeSystem2>()
                .Add<OnTriggerSystem>()
                .Add<DeathEventSystem>()
            .Init();
            uiService.Spawn<InventoryPopup>(false);
            //uiService.Spawn<RunesPopup>(false);
        }

        private void OnDestroy() {
            World.Destory(world);
        }

        void Update()
        {

            if(Paused) return;
            world.OnUpdate(Time.deltaTime);
        
            if (Input.GetKeyDown(KeyCode.V)) {
                // ref var ab = ref World.GetEntity(0).Get<AbilityList>();
                // ab.AbilityEntities.Add(abilitiesFabric.GetRandom());
                ref var loot = ref _lootServise.SpawnRandomItem(Vector3.zero);
                _inventoryService.AddItem(ref loot);
            }
            if (Input.GetKeyDown(KeyCode.C)) {
                uiService.Show<InventoryPopup>();
            }
            if (Input.GetKeyDown(KeyCode.X)) {
                _saveService.SaveInventory();
            }
            if (Input.GetKeyDown(KeyCode.Z)) {
                _saveService.LoadInventory();
            }

        }

        private void FixedUpdate() {
            world.OnFixedUpdate(Time.fixedDeltaTime);
        }

        public void Destroy() {
            World.Destory(world);
        }

        public bool Paused { get; set; }
    }

    sealed class OnPlayerSpawnSystem : ISystem {
        [With(typeof(Player), typeof(EntityLinkSpawnedEvent))] Query Query;
        private InventoryService _inventoryService;

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                _inventoryService.SetPlayer(entity);
                Debug.Log("Player Spawned");
            }
        }
    }
    sealed class ShotOnHitAbilitySystem : ISystem, IOnCreate {
        Query Query;
        IEntityFabric Fabric;
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

    public sealed class RunTimeData {
        public Entity Player;
    }

    public struct AnimatorRef : IComponent {
        public Animator value;
    }
    public class AnimationSystem : ISystem, IOnCreate {
        private Query Query;
        private IPool<AnimatorRef> animators;
        private IPool<InputData> inputs;

        public void OnCreate(World world) {
            Query = world.GetQuery().WithAll<AnimatorRef, InputData>();
        }

        public void OnUpdate(float deltaTime) { 
            foreach (ref var entity in Query) {
                ref var animator = ref animators.Get(ref entity).value;
                ref var input = ref inputs.Get(ref entity);

                if (input.horizontal < .1F && input.vertical < .1F && 
                    input.horizontal > -.1F &&
                    input.vertical > -.1F) {
                    
                    animator.Play(stateName: "Idle");
                }
                else {
                    animator.Play("Run");
                }
            }
        }
    }


    /// <summary>
    /// trigger items on character
    /// </summary>
    sealed class PreTriggerEquipmentListSystem : ISystem {
        [With(typeof(EquipmentList), typeof(OnAttackEvent))]            Query OnShotEvents;
        [With(typeof(EquipmentList), typeof(OnHitPhysicsEvent))]        Query OnHitEvents;
        [With(typeof(EquipmentList), typeof(OnHitWithDamageEvent))]     Query OnDamageEvents;
        [With(typeof(EquipmentList), typeof(OnKillEvent))]              Query OnKillEvents;
        [With(typeof(EquipmentList), typeof(OnCritEvent))]              Query OnCritEvents;
        [With(typeof(EquipmentList), typeof(OnTakeDamageEvent))]        Query OnTakeDamageEvents;
        private IPool<EquipmentList> abilities;

        private void Pretrigger<TEvent>(Query query) where TEvent : struct , IComponent {
            if (!query.IsEmpty) {
                foreach (ref var entity in query) {
                    ref var list = ref abilities.Get(ref entity);
                    for (var i = 0; i < list.value.Count; i++) {
                        list.value[i].Add<TEvent>();
                    }
                    entity.Add<OnTriggerAbilityEvent>();
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
    /// <summary>
    /// trigger runes
    /// </summary>
    sealed class TriggerEquipmentListSystem : ISystem {
        private Query OnShotEvents;
        private Query OnHitEvents;
        private Query OnDamageEvents;
        private Query OnKillEvents;
        private Query OnCritEvents;
        private Query OnTakeDamageEvents;
        private IPool<Equipment> equipment;
        public void OnCreate(World world) {
            OnShotEvents = world.GetQuery().WithAll<OnAttackEvent, Equipment>();
            OnHitEvents = world.GetQuery().WithAll<OnHitPhysicsEvent, Equipment>();
            OnDamageEvents = world.GetQuery().WithAll<OnHitWithDamageEvent, Equipment>();
            OnKillEvents = world.GetQuery().WithAll<OnKillEvent, Equipment>();
            OnCritEvents = world.GetQuery().WithAll<OnCritEvent, Equipment>();
            OnTakeDamageEvents = world.GetQuery().WithAll<OnTakeDamageEvent, Equipment>();
        }

        private void Trigger<TEvent>(Query query) where TEvent : struct , IComponent {
            if (!query.IsEmpty) {
                foreach (ref var entity in query) {
                    ref var list = ref equipment.Get(ref entity);
                    for (var i = 0; i < list.runes.Count; i++) {
                        list.runes[i].Add<TEvent>();
                    }
                }
            }
        }
        public void OnUpdate(float deltaTime) {
            Trigger<OnTriggerAbilityEvent>(OnShotEvents);
            Trigger<OnTriggerAbilityEvent>(OnHitEvents);
            Trigger<OnTriggerAbilityEvent>(OnDamageEvents);
            Trigger<OnTriggerAbilityEvent>(OnKillEvents);
            Trigger<OnTriggerAbilityEvent>(OnCritEvents);
            Trigger<OnTriggerAbilityEvent>(OnTakeDamageEvents);
        }
    }

    sealed class EnemyGroup : Systems.Group {
        public EnemyGroup() {
            Add<MoveToPlayerSystem>();
        }
    }
    sealed class MoveToPlayerSystem : ISystem {
        [With(typeof(EnemyTag), typeof(MoveSpeed), typeof(Translation))]
        private Query Query;
        [With(typeof(Player),typeof(Translation))]
        private Query players;

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var translation = ref entity.Get<Translation>();
                ref var movespeed = ref entity.Get<MoveSpeed>();
                
                foreach (ref var player in players) {
                    ref var playerTranslation = ref player.Get<Translation>();
                    var movementDirection = (playerTranslation.position - translation.position).normalized;
                    
                    if(movementDirection != Vector3.zero)
                        translation.rotation = Quaternion.LookRotation (movementDirection);
                    var distance = Vector3.Distance(playerTranslation.position, translation.position);
                    if(distance > 1f)
                        translation.position += movementDirection * movespeed.value * deltaTime;
                    else {
                        translation.position = translation.position;
                    }
                }
            }
        }
    }

    sealed class HitBoxCollisionSystem : 
        ISystem, 
        IClearBeforeUpdate<TakeHitEvent>, 
        IClearBeforeUpdate<OnHitWithDamageEvent> {

        [With(typeof(DamageColliderCreateRequest))] Query Query;
        private IPool<DamageColliderCreateRequest> requests;
        private readonly Collider[] _colliders = new Collider[256];
        private World _world;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var request = ref requests.Get(ref entity);
                var hits = Physics.OverlapSphereNonAlloc(request.pos, request.radius, _colliders);
                
                for (int i = 0; i < hits; i++) {
                    if (_colliders[i].gameObject.TryGetComponent(out IEntityLink link)) {
                        
                        ref var e = ref link.Entity;
                        if (e.Has<EnemyTag>()) {
                            _world.CreateEntity().
                            Add(new TakeHitEvent {
                                @from    = request.owner,
                                to       = e,
                                amount   = request.amount
                            });
                            request.owner.Add(new OnHitWithDamageEvent());
                        }
                    }
                }
                entity.Destroy();
            }
        }
    }

    sealed class DamageEntitiesSystem : ISystem, IClearBeforeUpdate<MakeDamagePerFrame>, IClearBeforeUpdate<TakeDamagePerFrame> {
        
        [With(typeof(TakeHitEvent))]
        Query eventsQuery;
        World world;
        IPool<TakeHitEvent> events;
        IPool<Translation> translations;
        ITextService _textService;
        
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in eventsQuery) {
                ref var damageEvent = ref events.Get(ref entity);
                ref var h = ref damageEvent.to.Get<Health>();
                damageEvent.@from.Get<MakeDamagePerFrame>().amount += damageEvent.amount;
                damageEvent.to.Get<TakeDamagePerFrame>().amount += damageEvent.amount;
                h.current -= damageEvent.amount;
                _textService.Show(translations.Get(ref damageEvent.to).position + Vector3.up, $"-{damageEvent.amount}", Color.white);
                
                if (h.current <= 0) {
                    damageEvent.to.Add<DeathEvent>();
                }
                entity.Destroy();
            }
        }
    }
    sealed class ShowDamageSystem : ISystem {
        
        [With(typeof(TakeDamagePerFrame))]
        Query eventsQuery;
        World world;
        IPool<TakeDamagePerFrame> events;
        IPool<Translation> translations;
        ITextService _textService;
        
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in eventsQuery) {
                ref var damageEvent = ref events.Get(ref entity);
                _textService.Show(translations.Get(ref entity).position + Vector3.up, $"-{damageEvent.amount}", Color.white);
            }
        }
    }
    public struct MakeDamagePerFrame : IComponent {
        public int amount;
    }

    public struct TakeDamagePerFrame : IComponent {
        public int amount;
    }
    
    sealed class DeathEventSystem : ISystem {
        private IObjectPool _pool;
        [With(typeof(DeathEvent), typeof(DeathEffectParticle), typeof(Translation))] private Query Query;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                _pool.Spawn(entity.Get<DeathEffectParticle>().value, entity.Get<Translation>().position, Quaternion.identity);
                entity.Destroy();
            }
        }
    }
    sealed class ReactOnTakingHitSystem : ISystem {
        [With(typeof(TakeDamagePerFrame))] Query q;
        public void OnUpdate(float dt) {
            foreach (ref var entity in q) {
                entity.doScale(1, 1.2f, 0.15f).WithEasing(Easings.EasingType.BounceEaseIn).WithLoop(2, LoopType.Yoyo);
            }
        }
    }
    
    
    
    sealed class PoolObjectLifeTimeSystem : ISystem {
        IObjectPool pool;
        IPool<Pooled> pooleds;
        IPool<ViewLink> views;
        World _world; 
        [With(typeof(Pooled),typeof(ViewLink))][Without(typeof(StaticTag))]
        Query query;

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var pooled = ref pooleds.Get(ref entity);
                pooled.lifeTime -= deltaTime;
                if (pooled.lifeTime < 0) {
                    pool.Release(views.Get(ref entity).Link, pooled.id);
                }
            }
        }
    }

    sealed class PlayerAttackSystem : ISystem, IOnCreate, IClearBeforeUpdate<OnAttackEvent> {
        private Query query;
        private IPool<InputData> inputs;
        private IPool<Attack> attacks;
        private IObjectPool Pool;
        private World _world;
        public void OnCreate(World world) {
            query = world.GetQuery().WithAll<InputData, Attack, Damage>().Without<Cooldown>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                if (input.fire) {
                    entity.Add<OnAttackEvent>();
                    ref Attack attack = ref attacks.Get(entity.Index);
                    
                    _world.CreateEntity().Add(new DamageColliderCreateRequest {
                        owner = entity,
                        amount = entity.Get<Damage>().value,
                        pos = entity.Get<Translation>().position + entity.Get<Translation>().forward,
                        radius = attack.radius
                    });
                    
                    entity.Add(new Cooldown{Value = attack.delay});


                    var pos = entity.Get<Translation>().position + entity.Get<Translation>().forward;
                    var e = Pool.Spawn(attack.viewPrefab, pos, Quaternion.identity);

                    e.Entity.Get<Translation>().scale = Vector3.one * attack.radius;
                }
            }
        }
    }
    sealed class PlayerSearchTargetSystem : ISystem {
        [With(typeof(TargetSearchType), typeof(AttackTarget))] private Query query;
        [With(typeof(Translation), typeof(EnemyTag))] private Query enemeies;
        private IPool<Translation> translations;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var search = ref entity.Get<TargetSearchType>();
                switch (search.value) {
                    case TargetSearch.RandomAround:
                        var random = Random.Range(0, enemeies.Count);
                        entity.Get<AttackTarget>().value = enemeies.GetEntity(random);
                        break;
                    case TargetSearch.Nearest:
                        if (!enemeies.IsEmpty) {
                            
                            var nearestDistance = float.MaxValue;
                            Entity nearest = enemeies.GetEntity(0);
                            foreach (ref var enemy in enemeies) {
                                ref var translation = ref translations.Get(ref enemy);
                                ref var pTranslation = ref translations.Get(ref entity);
                                var distance = Vector3.Distance(translation.position, pTranslation.position);
                                if (distance < nearestDistance) {
                                    nearestDistance = distance;
                                    nearest = enemy;
                                }
                            }
                            entity.Get<AttackTarget>().value = nearest;
                        }

                        break;
                }
            }
        }
    }
    
    sealed class SearchTargetSystem : ISystem {
        [With(typeof(TargetSearchType), typeof(AttackTarget))][Without(typeof(Player))] private Query query;
        [With(typeof(Translation), typeof(EnemyTag))] private Query enemeies;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var search = ref entity.Get<TargetSearchType>();
                switch (search.value) {
                    case TargetSearch.SameAsPlayer:
                        entity.Get<AttackTarget>().value = entity.GetOwner().Get<AttackTarget>().value;
                        break;
                    case TargetSearch.RandomAround:
                        var random = Random.Range(0, enemeies.Count);
                        entity.Get<AttackTarget>().value = enemeies.GetEntity(random);
                        break;
                }
            }
        }
    }

    sealed class TriggeredOnAttackSystem : ISystem {
        [With(typeof(Attack),typeof(AttackTarget),typeof(OnAttackAbility),typeof(OnTriggerAbilityEvent))]
        [Without(typeof(Cooldown))] 
        Query Query;
        private World _world;
        private IObjectPool Pool;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                entity.Remove<OnTriggerAbilityEvent>();
                ref var target = ref entity.Get<AttackTarget>();
                if (target.value.IsNull()) {
                    
                    continue;
                }
                
                ref var attack = ref entity.Get<Attack>();
                var pos = target.value.Get<Translation>().position;
                _world.CreateEntity().Add(new DamageColliderCreateRequest {
                    owner = entity.GetOwner(),
                    amount = entity.Get<Damage>().value,
                    pos = pos,
                    radius = attack.radius
                });
                
                entity.Add(new Cooldown{Value = attack.delay});

                var e = Pool.Spawn(attack.viewPrefab, pos, Quaternion.identity);
                
                //e.Entity.Get<Translation>().scale = Vector3.one * attack.radius;
            }
        }
    }
    
    sealed class TriggeredAttackOnHitSystem : ISystem {
        [With(typeof(Attack),typeof(AttackTarget),typeof(OnHitAbility),typeof(OnTriggerAbilityEvent)), Without(typeof(Cooldown))] 
        Query _query;
        World _world;
        IObjectPool _pool;
        public void OnUpdate(float deltaTime) {

            foreach (ref var entity in _query) {
                entity.Remove<OnTriggerAbilityEvent>();
                ref var target = ref entity.Get<AttackTarget>().value;
                if (target.IsNull()) {
                    continue;
                }
                ref var attack = ref entity.Get<Attack>();
                var pos = target.Get<Translation>().position;
                _world.CreateEntity()
                    .Add(new DamageColliderCreateRequest {
                    owner = entity.GetOwner(),
                    amount = entity.Get<Damage>().value,
                    pos = pos,
                    radius = attack.radius
                });
                
                entity.Add(new Cooldown{Value = attack.delay});

                var e = _pool.Spawn(attack.viewPrefab, pos, Quaternion.identity);
                
                //e.Entity.Get<Translation>().scale = Vector3.one * attack.radius;
            }
        }
    }

    public struct Vampire : IComponent {
        public float pernenteFromDamage;
    }

    public struct HealEvent : IComponent {
        public int amount;
    }
    sealed class TriggerVampireSystem : ISystem, IClearBeforeUpdate<HealEvent> {
        [With(typeof(Vampire), typeof(OnHitAbility), typeof(OnTriggerAbilityEvent))] Query query;
        private IPool<Vampire> vampires;
        
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var vampire = ref vampires.Get(ref entity);
                ref var targetToHeal = ref entity.GetOwner();
                int damaged = targetToHeal.Get<MakeDamagePerFrame>().amount;

                var healAmount = (int)(damaged * vampire.pernenteFromDamage * 0.01f);
                if(healAmount < 1) continue;
                targetToHeal.Add(new HealEvent { amount = healAmount });
            }
        }
    }

    sealed class HealSystem : ISystem {
        [With(typeof(Health),typeof(HealEvent))]
        Query query;
        IPool<Translation> translations;
        IPool<HealEvent> healEvents;
        IPool<Health> healthes;
        ITextService _textService;

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var healEvent = ref healEvents.Get(ref entity);
                ref var health = ref healthes.Get(ref entity);

                health.current += healEvent.amount;
                
                _textService.Show(translations.Get(ref entity).position + Vector3.up, $"+{healEvent.amount}", Color.green);
                if (health.max < health.current)
                    health.current = health.max;
            }
        }
    }
    public struct DeathEffectParticle : IComponent {
        public EntityLink value;
    }


    struct TotalLifeTime : IComponent {
        public float value;
    }

    sealed class TotalLifeTimeSystem : ISystem {
        [With(typeof(TotalLifeTime))] private Query _query;
        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in _query) {
                entity.Get<TotalLifeTime>().value += deltaTime;
            }
        }
    }

    struct EntityStats : IOnAddToEntity, IComponent {
        public List<Entity> Entities;
        public void OnAdd() {
            Entities ??= new List<Entity>();
        }
        public void Add(Entity entity) { Entities.Add(entity); }
        public void Remove(Entity entity) { Entities.Remove(entity); }
    }

    public struct ListTest : IComponent {
        public List<int> Test;
    }
    public struct ArrayTest : IComponent {
        public int[] Test;
    }

    public struct ListTest2 : IComponent {
        public List<EntityLink> Links;
    }
    [Serializable]
    public struct TestData {
        public int value1;
        public Vector2 value2;
        public GameObject value3;
    }
    
    public struct TestNested : IComponent {
        public float value;
        public TestData valueNested;
    }
}

        