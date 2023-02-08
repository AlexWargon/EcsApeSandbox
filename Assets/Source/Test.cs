using System;
using System.Collections.Generic;
using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;
using Wargon.Ecsape.Tween;
using Wargon.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Test : WorldHolder {
    public static int cubeID;
    [SerializeField] private int fps;
    [SerializeField] private Transform cube;
    private AnimationsHolder animationsHolder;
    private Transform activeCube;
    private Systems systems;
    private IUIService uiService;
    private IObjectPool objectPool;
    private void Awake() {
        World.ENTITIES_CACHE = 256;
        DI.Build(this);
        
        DI.Register<IEntityFabric>().From(new EntityFabric().Init(world));
        
        Application.targetFrameRate = fps;
        animationsHolder.Init();
        world = World.GetOrCreate(World.DEFAULT);
        (DI.Get<IObjectPool>() as MyObjectPool)?.SetWorld(world);
        Fabric.Init(world);
        systems = new Systems(world);
        systems
            .AddInjector(DI.GetOrCreateContainer())
            .Add(new ShmupGame())
            .Add(new Systems.Group("updates")
                //.Add<TestSystem>()
                //.Add<TestEntitySystem>()
                //.Add<ChangeSpeedSystem>()
                //.Add<ChangeSizeSystem>()
                //.Add(new ShotSystem(cube))
                .Add<ConvertEntitySystem>()
                
                //.Add<PlayerInputSystem>()
                .Add<TestPlayerAnimationSystem>()
                .Add<Animation2DSystem>()
                .Add<TweensTestSystem>()

                .Add<OnTriggerSystem>()
                .Add<SyncTransformsSystem>()
                //.Add<TestData1System>()
                //.Add<TestFilterSystem1>()
                //.Add<TestFilterSystem2>()
            )
            
            .Init();

        //SpawnCubes();
        //SpawnFilterTestEntities();

        //world.EntityManager.Test(666);
    }   

    private List<Transform> cubes = new List<Transform>();
    void SpawnCubes() {
        foreach (var _ in 1000) {
            var pos = new Vector3(Random.Range(-16F, 16F), Random.Range(-16F, 16F), Random.Range(-16F, 16F));
            objectPool.Spawn(cube, pos, Quaternion.identity);
            
            //var go = GameObjectPool.Instance.Spawn(cube, pos, Quaternion.identity);
            // e.Add(new TransformReference { value = go.transform, instanceID = cube.GetInstanceID()});
            // e.Add(new ViewID{value = cube.GetInstanceID()});

        }
    }
    private void Update() {
        systems.Update(Time.deltaTime);
        
        if(Input.GetKeyDown(KeyCode.Space))
            uiService.Show<Popup>(() =>
            {
                uiService.Hide<Popup>();
            });
    }

}

sealed class ShotSystem : ISystem {
    private readonly GameObject bullet;
    public ShotSystem(GameObject bullet) => this.bullet = bullet;
    public void OnCreate(World world) {}
    public void OnUpdate(float deltaTime) {
        if (Input.GetKey(KeyCode.Space)) {
            Fabric.Bullet(bullet,Vector3.zero, Quaternion.identity, 20f, Vector3.up);
        }
    }
}

public static class Fabric {
    private static World world;
    public static void Init(World world) => Fabric.world = world;
    public static Entity Bullet(GameObject view, Vector3 pos, Quaternion rotation, float speed, Vector3 dir) {
        var e = world.CreateEntity();
        var go = Object.Instantiate(view, pos, rotation);
        e.Get<TransformReference>().value = go.transform;
        e.Add(new Translation{position = pos, rotation = rotation});
        e.Get<MoveSpeed>().value = speed;
        e.Get<MoveDirection>().value = dir;
        return e;
    }
}

[Serializable]
public struct MoveSpeed : IComponent {
    public float value;
}

[Serializable] struct MoveDirection : IComponent {
    public Vector3 value;
}

[Serializable]
public struct Health : IComponent {
    public int current;
    public int max;
}
[Serializable]
public struct DamageEvent : IComponent {
    public int amount;
    public Entity from;
    public Entity to;
}
[Serializable]
public struct Crit : IComponent{
    public int size;
    public float chance;
}

sealed class SetCritDamageSystem : ISystem {
    private Query query;
    private IPool<Crit> crit;
    private IPool<DamageEvent> damage;

    public void OnCreate(World world) {
        query = world.GetQuery<DamageEvent>();
        crit = world.GetPool<Crit>();
        damage = world.GetPool<DamageEvent>();
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

struct TestEvent : IComponent { }
sealed class TestEventReactSystem : ISystem {
    private World world;
    private Query query;
    public void OnCreate(World world) {
        this.world = world;
        query = this.world.GetQuery<TestEvent>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (var entity in query) {
            //Debug.Log($"entity {entity.Index} triggered with {nameof(TestEvent)}");
        }
    }
}
sealed class TestEventSystem : ISystem, IEventSystem<TestEvent> {
    private World world;
    public void OnCreate(World world) {
        this.world = world;
    }

    public void OnUpdate(float deltaTime) {
        if (Input.GetKeyDown(KeyCode.E)) {
            for (int i = 0; i < 10000; i++) {
                world.CreateEntity().Add<TestEvent>();
            }
        }
    }
}

sealed class TweensTestSystem : ISystem {
    private Query query;
    private IPool<Translation> translations;
    public void OnCreate(World world) {
        query = world.GetQuery().With<Translation>().Without<BlockJump>();
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



sealed class TestPlayerAnimationSystem : ISystem {
    private IPool<SpriteAnimation> animations;
    private IPool<InputData> inputs;
    private Query query;
    public void OnCreate(World world) {
        query = world.GetQuery().With<SpriteAnimation>().With<InputData>();
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
[Serializable]
public struct Direction : IComponent {
    public Vector3 value;
}

[Serializable]
public struct IsProjectile : IComponent{}
[Serializable]
public struct Damage : IComponent {
    public int value;
}

sealed class ShmupGame : Systems.Group {
    public ShmupGame() : base() {
            Add<PlayerInputSystem>()
            .Add<ShootProjectileSystem>()
            .Add<ProjectileMoveSystem>()
            .Add<SetDamageSystem>()
            .Add<OnTakeDamageHitReactionSystem>()
            .Add<SetDamageToHealthSystem>()
            .Add<OnDeadSystem>()
            .Add<PoolObjectLifeTimeSystem>();
    }
}
sealed class ProjectileMoveSystem : ISystem {
    private Query query;
    private IPool<Direction> directions;
    private IPool<MoveSpeed> speeds;
    private IPool<Translation> translations;
    public void OnCreate(World world) {
        query = world.GetQuery()
            .With<IsProjectile>()
            //.With<Direction>()
            .With<MoveSpeed>()
            .With<Translation>().With<Active>();
    }

    public void OnUpdate(float deltaTime) {
        
        foreach (ref var entity in query) {
            ref var translation = ref translations.Get(ref entity);
            ref var speed = ref speeds.Get(ref entity);
            //ref var direction = ref directions.Get(ref entity);

            translation.position += translation.rotation*Vector3.right * speed.value * deltaTime;
        }
    }
}

sealed class SetDamageSystem : ISystem, IEventSystem<DamageEvent> {
    public void OnCreate(World world) {
        query = world.GetQuery().With<CollidedWith>().With<Damage>().Without<Dead>();;
    }

    private IPool<CollidedWith> collisions;
    private IPool<Damage> damages;
    private Query query;
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var collision = ref collisions.Get(ref entity);
            ref var damage = ref damages.Get(ref entity);
            
            collision.entity.Add(new DamageEvent {
                from = entity,
                to = collision.entity,
                amount = damage.value
            });
        }
    }
}

sealed class OnTakeDamageHitReactionSystem : ISystem {
    public void OnCreate(World world) {
        query = world.GetQuery().With<Translation>().With<DamageEvent>().Without<Dead>();;
    }

    private Query query;
    private IPool<Translation> translations;
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var translation = ref translations.Get(ref entity);
            entity.doScale(translation.scale, translation.scale * 0.8f, 0.2f)
                .WithLoop(2, LoopType.Yoyo)
                .WithEasing(Easings.EasingType.BounceEaseIn);
        }
    }
}
public struct Dead : IComponent {}
sealed class SetDamageToHealthSystem : ISystem {
    private Query query;
    private IPool<Health> healths;
    private IPool<DamageEvent> damageEvents;
    public void OnCreate(World world) {
        query = world.GetQuery<Health, DamageEvent>().Without<Dead>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var h = ref healths.Get(ref entity);
            ref var d = ref damageEvents.Get(ref entity);
            h.current -= d.amount;
            h.current = Mathf.Clamp(h.current,0, h.max);
            if(h.current==0)
                entity.Add<Dead>();
        }
    }
}

sealed class OnDeadSystem : ISystem {
    public void OnCreate(World world) {
        query = world.GetQuery().With<Dead>().With<Translation>();
    }

    private Query query;
    public void OnUpdate(float deltaTime) {
        foreach (ref Entity entity in query) {
            entity.doScale(1f, 0.8f, 0.5f)
                .WithLoop(4, LoopType.Yoyo)
                .WithEasing(Easings.EasingType.BounceEaseOut)
                .OnComplete(e=>e.Destroy());
        }
    }
}
[Serializable]
public struct SpreadWeapon : IComponent {
    public EntityLink projectile;
    public float delay;
    public float delayCounter;
    public float spread;
    public int count;
    public Transform firePoint;
}

public struct ShotEvent : IComponent { }
sealed class ShootProjectileSystem : ISystem, IEventSystem<ShotEvent> {
    private Query query;
    private IPool<SpreadWeapon> weapons;
    private IPool<InputData> inputs;
    private IEntityFabric fabric;
    public void OnCreate(World world) {
        query = world.GetQuery().With<SpreadWeapon>().With<InputData>().Without<Dead>();
    }

    public void OnUpdate(float deltaTime) {

        foreach (ref var entity in query) {
            ref var weapon = ref weapons.Get(ref entity);
            ref var input = ref inputs.Get(ref entity);

            if (input.fire) {
                
                if (weapon.delayCounter > weapon.delay) {
                    
                    for (int i = 0; i < weapon.count; i++) {

                        var z = weapon.firePoint.eulerAngles.z + Random.Range(-weapon.spread, weapon.spread);
                        var rotation = Quaternion.Euler(0, 0,z);
                        Debug.Log(rotation.eulerAngles.z);
                        fabric.Instantiate(weapon.projectile, weapon.firePoint.position, rotation);
                        //b.Get<Direction>().value = rotation * Vector3.right;
                    }
                    weapon.delayCounter = 0;
                }
            }

            weapon.delayCounter += deltaTime;
            entity.Add<ShotEvent>();
        }
    }
}

sealed class PoolObjectLifeTimeSystem : ISystem {
    private IObjectPool pool;
    private Query query;
    private IPool<Active> views;
    public void OnCreate(World world) {
        query = world.GetQuery().With<Active>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var view = ref views.Get(ref entity);
            view.lifeTime -= deltaTime;
            if (view.lifeTime < 0) {
                pool.Release(view.view, view.id);
                entity.Remove<Active>();
            }
        }
    }
}
sealed class CharacterAnimationSystem : ISystem {
    private Query query;
    private IPool<SpriteAnimation> animtions;
    private IPool<Translation> translations;
    private IPool<Direction> directions;
    public void OnCreate(World world) {
        query = world.GetQuery().With<SpriteAnimation>().With<Translation>();
        
    }

    public void OnUpdate(float deltaTime) {
        foreach (var entity in query) {
            ref var translation = ref translations.Get(entity.Index);
            ref var animation = ref animtions.Get(entity.Index);
            ref var direction = ref directions.Get(entity.Index);
            
            
        }
    }
}
