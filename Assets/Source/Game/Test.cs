using System;
using Animation2D;
using Rogue;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;
using Wargon.Ecsape.Tween;
using Wargon.UI;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(ExecutionOrder.EcsMain)]
public class Test : WorldHolder {
    
    [SerializeField] private int fps;
    [SerializeField] private EntityLink cube;
    private AnimationsHolder animationsHolder;
    private Transform activeCube;
    private IUIService uiService;
    private IObjectPool objectPool;
    private IEntityFabric fabric;
    private void Awake() {
        World.ENTITIES_CACHE = 32;
        DI.Build(this);
        //Application.targetFrameRate = fps;
        animationsHolder.Init();
        world = World.GetOrCreate();
        fabric = new EntityFabric().Init(world);
        DI.Register<IEntityFabric>().From(fabric);

        world
            .AddDIContainer(DI.GetOrCreateContainer())
            
            .AddGroup(new ShmupGame())
            .AddGroup(new Systems.Group("updates")
                //.Add<ConvertEntitySystem>()
                
                .Add<TestPlayerAnimationSystem>()
                .Add<Animation2DSystem>()
                .Add<TweensTestSystem>()
                //.Add<ProjectileCollisionSystem>()
                
                
                
                .Add<OnTriggerSystem>()
                //.Add<SyncTransformsSystem>()
                .Add<PoolObjectLifeTimeSystem>()
                .Add<PoolObjectReleaseSystem>()
            )
            //.Add(new TestBurstSystem())
            //.Add(new MassageSystem<DestroyObject>(x => Destroy(x.target)))            
            .Init();

        // unsafe {
        //     var w = EcsUnsafe.world.create();
        //     var e1 = w->create_entity();
        //     var e2 = w->create_entity();
        //     
        //     Debug.Log(e1->index);
        //     Debug.Log(e2->index);
        // }
        //SpawnCubes();
        //SpawnFilterTestEntities();
    }

    private void Update() {
        world.OnUpdate(Time.deltaTime);
        
        if(Input.GetKeyDown(KeyCode.Space))
            uiService.Show<Popup>(() =>
            {
                uiService.Hide<Popup>();
            });
    }
}

sealed class ProjectileMoveSystem : ISystem {
    private Query query;
    private IPool<Direction> directions;
    private IPool<MoveSpeed> speeds;
    private IPool<Translation> translations;
    private World _world;
    private static float dt;
    public void OnCreate(World world) {
        query = world.GetQuery()
            .WithAll<IsProjectile, MoveSpeed, Translation, Pooled>()
            .Without<StaticTag>();
    }

    public void OnUpdate(float deltaTime) {
        if(query.IsEmpty) return;
        // foreach (ref var entity in query) {
        //     ref var translation = ref translations.Get(ref entity);
        //     ref var speed = ref speeds.Get(ref entity);
        //     translation.position += translation.rotation * Vector3.right * speed.value * deltaTime;
        // }
        var job = new MoveJob {
            speeds = speeds.AsNative(),
            translations = translations.AsNative(),
            Query = query.AsNative(),
            deltaTime = deltaTime
        };
        job.ScheduleAndComplete(query);

    }

    [BurstCompile]
    struct MoveJob : IJobParallelFor {
        public NativeQuery Query;
        public NativePool<MoveSpeed> speeds;
        public NativePool<Translation> translations;
        public float deltaTime;
        public void Execute(int index) {

            var e = Query.GetEntity(index);
            ref var speed = ref speeds.Get(e);
            ref var translation = ref translations.Get(e);
            translation.position += translation.rotation * Vector3.right * speed.value * deltaTime;
        }
    }
}


sealed class SetCritDamageSystem : ISystem {
    private Query query;
    private IPool<Crit> crit;
    private IPool<TakeHitEvent> damage;

    public void OnCreate(World world) {
        query = world.GetQuery<TakeHitEvent>();
        crit = world.GetPool<Crit>();
        damage = world.GetPool<TakeHitEvent>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var d = ref damage.Get(ref entity);
            ref var c = ref crit.Get(ref d.from);

            if (Random.value > c.chance) {
                d.amount *= c.size;
            }
        }
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
    public void OnCreate(World world) {
        query = world.GetQuery().WithAll<InputData,Translation,MoveSpeed>();
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
        }
    }
}
sealed class TweensTestSystem : ISystem {
    private Query query;
    private IPool<Translation> translations;
    private World World;

    public void OnCreate(World world) {
        query = world.GetQuery().WithAll<Translation, InputData>().Without<BlockJump>();
    }

    public void OnUpdate(float deltaTime) {
        if (Input.GetKeyDown(KeyCode.Space)) {
            foreach (ref var entity in query) {
        
                ref var translation = ref translations.Get(entity.Index);
                entity.doScale(Vector3.one, new Vector3(2, 1, 2), .5f)
                    .WithLoop(2, LoopType.Yoyo)
                    .WithEasing(Easings.EasingType.ExponentialEaseInOut);
                
                entity.doRotation(Vector3.zero, new Vector3(0, 0, Random.value > .5f ? 360 : -360), 0.2f)
                    .WithLoop(4, LoopType.Restart);
                
                entity.doMove(translation.position, translation.position + Vector3.up * 3, .5f)
                    .WithLoop(2, LoopType.Yoyo)
                    .OnComplete(e => e.Remove<BlockJump>());
                entity.Add<BlockJump>();
        
            }
        }
    }
}

public struct BlockJump : IComponent{}
[Serializable]
public struct InputData : IComponent {
    public float horizontal;
    public float vertical;
    public bool fire;
}
sealed class TestPlayerAnimationSystem : ISystem {
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

public struct MoveDirections {
    public const int UP = 0;
    public const int DOWN = 1;
    public const int LEFT = 2;
    public const int RIGHT = 3;
    public const int UP_LEFT = 4;
    public const int UP_RIGHT = 5;
    public const int DOWN_LEFT = 6;
    public const int DOWN_RIGHT = 7;
}


sealed class ShmupGame : Systems.Group {
    public ShmupGame() : base() {
        Add<PlayerInputSystem>()
            .Add<CooldownSystem>()
            .Add<Rogue.PreTriggerAbilitiesListSystem>()
            .Add<Rogue.TriggerAbilitiesSystem>()
            .Add<Rogue.BonusShotOnShotAbilitySystem>()
            .Add<MoveSystem>()
            .Add<BonusShotSystem>()
            .Add<SpreadWeaponSystem>()
            .Add<ProjectileMoveSystem>()
            .Add<SetDamageSystem>()
            .Add<OnTakeDamageHitReactionSystem>()
            .Add<SetDamageToHealthSystem>()
            .Add<OnDeadSystem>()
            ;

    }
}

public struct BonusShot : IComponent {
    public EntityLink prefab;
}
sealed class BonusShotSystem : ISystem {
    private Query query;
    private IEntityFabric _fabric;
    private IPool<SpreadWeapon> weapons;
    private IPool<BonusShot> bonusShots;
    public void OnCreate(World world) {
        query = world.GetQuery()
            .WithAll<SpreadWeapon, OnAttackEvent, BonusShot>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var weapon = ref weapons.Get(ref entity);
            ref var shot = ref bonusShots.Get(ref entity);
            _fabric.Instantiate(shot.prefab, weapon.firePoint.position, weapon.firePoint.rotation);
        }
    }
}

sealed class ProjectileCollisionSystem : ISystem {
    private Query Query;
    private IPool<Translation> translations;
    private World World;
    public void OnCreate(World world) {
        Query = world.GetQuery()
            .WithAll<Translation,IsProjectile>()
            .Without<StaticTag>();
    }
    
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in Query) {
            ref var translation = ref translations.Get(ref entity);

            var hit = Physics2D.Raycast(translation.position, translation.right);
            if(hit.collider==null) return;
            if (hit.collider.TryGetComponent(out IEntityLink link)) {
                World.CreateEntity().Add(new OnTriggerEnter {
                    From = entity,
                    To = link.Entity
                });
            }
        }
    }
}


sealed class SetDamageSystem : ISystem, IClearBeforeUpdate<TakeHitEvent> {
    private Query query;
    private IPool<CollidedWith> collisions;
    private IPool<Damage> damages;
    private IPool<Pooled> pooleds;
    private IPool<ViewLink> views;
    private IObjectPool pool;
    
    public void OnCreate(World world) {
        query = world.GetQuery()
            .WithAll<CollidedWith,Pooled,Damage>()
            .Without<Dead>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var collision = ref collisions.Get(ref entity);
            ref var damage = ref damages.Get(ref entity);
            if (!collision.entity.IsNull()) {
                collision.entity.Add(new TakeHitEvent {
                    from = entity,
                    to = collision.entity,
                    amount = damage.value
                });
            }
            ref var view = ref pooleds.Get(ref entity);
            pool.Release(views.Get(ref entity).Link, view.id);
        }
    }
}

sealed class OnTakeDamageHitReactionSystem : ISystem {
    public void OnCreate(World world) {
        query = world.GetQuery().WithAll<Translation,TakeHitEvent,Health>().Without<Dead>();
    }

    private Query query;
    private IPool<Translation> translations;
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            // entity.doScale(1,0.7f, 0.1f)
            //     .WithLoop(2, LoopType.Yoyo)
            //     .WithEasing(Easings.EasingType.BounceEaseIn);

        }
    }
}

public struct Dead : IComponent {}
sealed class SetDamageToHealthSystem : ISystem, IClearBeforeUpdate<Dead> {
    private Query query;
    private IPool<Health> healths;
    private IPool<TakeHitEvent> damageEvents;
    public void OnCreate(World world) {
        query = world.GetQuery().With<Health>().With<TakeHitEvent>().Without<Dead>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var h = ref healths.Get(ref entity);
            ref var d = ref damageEvents.Get(ref entity);
            h.current -= d.amount;
            h.current = Mathf.Clamp(h.current,0, h.max);
            if(h.current<=0)
                entity.Add<Dead>();
        }
    }
}

sealed class OnDeadSystem : ISystem {
    public void OnCreate(World world) {
        query = world.GetQuery().With<Dead>().With<Translation>();
    }
    private World world;
    private Query query;
    public void OnUpdate(float deltaTime) {
        foreach (var entity in query) {
            entity.doScale(2f, 0.8f, 0.2f)
                .WithLoop(4, LoopType.Yoyo)
                .WithEasing(Easings.EasingType.BounceEaseOut)
                .OnComplete(() => {
                    entity.Destroy();
                });
            Debug.Log("DEAD");
            entity.Remove<Health>();
        }
    }
}
public struct SpreadWeapon : IComponent {
    public EntityLink projectile;
    public EntityLink flash;
    public float delay;
    public float spread;
    public int count;
    public Transform firePoint;

    public Quaternion Spread() {
        return  Quaternion.Euler(0, 0,firePoint.eulerAngles.z + Random.Range(-spread, spread));
    }
}



sealed class SpreadWeaponSystem : ISystem, IClearBeforeUpdate<OnAttackEvent> {
    private Query query;
    private IPool<SpreadWeapon> weapons;
    private IPool<InputData> inputs;
    private IEntityFabric fabric;
    public void OnCreate(World world) {
        query = world.GetQuery()
            .WithAll(typeof(SpreadWeapon), typeof(InputData))
            .Without<Dead>().Without<Cooldown>();
    }

    public void OnUpdate(float deltaTime) {

        foreach (ref var entity in query) {
            ref var weapon = ref weapons.Get(ref entity);
            ref var input = ref inputs.Get(ref entity);

            //entity.Add<SpreadWeapon, InputData>();
            if (input.fire) {
                fabric.Instantiate(weapon.flash, weapon.firePoint.position, weapon.firePoint.rotation);
                //flash.Get<Translation>().scale = Extensions.Random(0.8f, 1.8f);
                //flash.Get<Translation>().rotation = Extensions.RandomZ(0f, 90f);
                for (int i = 0; i < weapon.count; i++) {
                    fabric.Instantiate(weapon.projectile, weapon.firePoint.position, weapon.Spread())
                        .SetOwner(entity);
                }
                
                entity.Add<OnAttackEvent>();
                entity.Add(new Cooldown{Value = weapon.delay});
            }
        }
    }
}



struct ReleaseEvent : IComponent {
    public int index;
}
sealed class PoolObjectReleaseSystem : ISystem {
    private IObjectPool pool;
    private IPool<Pooled> pooleds;
    private IPool<ViewLink> views;
    private Query query;
    public void OnCreate(World world) {
        query = world.GetQuery()
            .With<Pooled>()
            .With<ReleaseEvent>()
            .Without<StaticTag>();
    }
    
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            // ref var pooled = ref pooleds.Get(ref entity);
            // ref var link = ref views.Get(ref entity);
            // pool.Release(link.Link, pooled.id);
            // entity.Remove<ReleaseEvent>();
        }
    }
}
sealed class CharacterAnimationSystem : ISystem {
    private Query query;
    private IPool<SpriteAnimation> animtions;
    private IPool<Translation> translations;
    private IPool<Direction> directions;
    public void OnCreate(World world) {
        query = world.GetQuery().WithAll(typeof(SpriteAnimation),typeof(Translation));
    }

    public void OnUpdate(float deltaTime) {
        foreach (var entity in query) {
            ref var translation = ref translations.Get(entity.Index);
            ref var animation = ref animtions.Get(entity.Index);
            ref var direction = ref directions.Get(entity.Index);
            
            
        }
    }
}
