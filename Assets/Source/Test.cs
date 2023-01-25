using System;
using System.Collections.Generic;
using Animation2D;
using UnityEngine;
using UnityEngine.Pool;
using Wargon.Ecsape;
using Wargon.Ecsape.Tweens;
using Wargon.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class TestText {
    public string text;
}
public partial class Test : WorldHolder {
    public static int cubeID;
    [SerializeField] private int fps;
    [SerializeField] private Transform cube;
    private AnimationsHolder _animationsHolder;
    private Transform activeCube;
    private Systems _systems;
    private IUIService _uiService;
    private void Awake() {
        var container = DI.GetOrCreateContainer();
        container.Build(this);

        Application.targetFrameRate = fps;
        _animationsHolder.Init();
        world = Worlds.Get(Worlds.Default);
        Fabric.Init(world);
        _systems = new Systems(world);
        _systems
            .AddInjector(container)
            .Add(new Systems.Group("updates")
                //.Add<TestSystem>()
                //.Add<TestEntitySystem>()
                //.Add<ChangeSpeedSystem>()
                //.Add<ChangeSizeSystem>()
                //.Add(new ShotSystem(cube))
                .Add<ConvertEntitySystem>()
                .Add<TestInputSystem>()
                .Add<TestPlayerAnimationSystem>()
                .Add<Animation2DSystem>()
                //.Add<TweensTestSystem>()
                .Add<SyncTransformsSystem>()
                //.Add<TestData1System>()
                //.Add<TestFilterSystem1>()
                //.Add<TestFilterSystem2>()
            )
            
            .Init();

        SpawnCubes();
        //SpawnFilterTestEntities();
        
        // for (int i = 0; i < 1000; i++) {
        //     var pos = new Vector3(Random.Range(-10F, 10F), Random.Range(-10F, 10F), Random.Range(-10F, 10F));
        //     var s = Instantiate(cube, pos, Quaternion.identity);
        //     cubes.Add(s);
        // }

    }

    private List<Transform> cubes = new List<Transform>();
    void SpawnCubes() {
        foreach (var _ in 1000) {
            var pos = new Vector3(Random.Range(-10F, 10F), Random.Range(-10F, 10F), Random.Range(-10F, 10F));
            Instantiate(cube, pos, Quaternion.identity);
            
            //var go = GameObjectPool.Instance.Spawn(cube, pos, Quaternion.identity);
            // e.Add(new TransformReference { value = go.transform, instanceID = cube.GetInstanceID()});
            // e.Add(new ViewID{value = cube.GetInstanceID()});

        }
    }
    private void Update() {
        _systems.Update(Time.deltaTime);
        
        if(Input.GetKeyDown(KeyCode.Space))
            _uiService.Show<Popup>((() => Debug.Log("UI WORK")));
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
    private static World _world;
    public static void Init(World world) => _world = world;
    public static Entity Bullet(GameObject view, Vector3 pos, Quaternion rotation, float speed, Vector3 dir) {
        var e = _world.CreateEntity();
        var go = Object.Instantiate(view, pos, rotation);
        e.Get<TransformReference>().value = go.transform;
        e.Add(new Translation{position = pos, rotation = rotation});
        e.Get<MoveSpeed>().value = speed;
        e.Get<MoveDirection>().value = dir;
        return e;
    }
}
struct ClearTestComponent : IComponent, IClearOnEndOfFrame { }

struct ViewID : IComponent {
    public int value;
}
[Serializable]
public struct TransformReference : IComponent, IDisposable {
    public Transform value;
    public int instanceID;
    
    public void Dispose() {
        GameObjectPool.Instance.Release(value, instanceID);
    }
}
[Serializable]
struct MoveSpeed : IComponent {
    public float value;
}

[Serializable] struct MoveDirection : IComponent {
    public Vector3 value;
}
[Serializable]
struct ChangeSize : IComponent {
    public float minScale;
    public float maxScale;
    public float speed;
    public bool grow;
}


public static class Service {
    private static Dictionary<string, object> instances;

    static Service() {
        instances = new Dictionary<string, object>();
    }

    public static T GetOrCreate<T>() where T : class, new() {
        var name = nameof(T);
        if (instances.TryGetValue(name, out var item))
            return (T)item;
        instances.Add(name, new T());
        return (T)instances[name];
    }
}
    
    
public sealed class GameObjectPool {
    public static GameObjectPool Instance;
    static GameObjectPool() {
        Instance = new GameObjectPool();
    }
    private Dictionary<int, IObjectPool<Transform>> _pools = new Dictionary<int, IObjectPool<Transform>>();
    private Vector2 UnActivePosition = new Vector2(100000, 100000);
    public Transform Spawn(Transform prefab, Vector3 position, Quaternion rotation) {
        var id = prefab.GetInstanceID();
        if (_pools.TryGetValue(id, out var pool)) {
            var item = pool.Get();
            item.position = position;
            item.rotation = rotation;
            return item;
        }
        var newPool = new ObjectPool<Transform>(
            () => Object.Instantiate(prefab), 
            x => x.gameObject.SetActive(true), 
            x=> x.gameObject.SetActive(false));
        _pools.Add(id, newPool);
        return Spawn(prefab,position, rotation);
    }

    public int GetID(Transform prefab) => prefab.GetInstanceID();
    public void Release(Transform view, int id) {
        //view.gameObject.SetActive(false);
        _pools[id].Release(view);
    }
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
public struct Crit : IComponent, INew {
    public int size;
    public float chance;

    void INew.New() {
        
    }
}
[Serializable]
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

sealed class SetDamageToHealthSystem : ISystem {
    private Query query;
    private IPool<Health> healths;
    private IPool<DamageEvent> damageEvents;
    public void OnCreate(World world) {
        query = world.GetQuery<Health, DamageEvent>();
        healths = world.GetPool<Health>();
        damageEvents = world.GetPool<DamageEvent>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in query) {
            ref var h = ref healths.Get(ref entity);
            ref var d = ref damageEvents.Get(ref entity);
            h.current -= d.amount;
            h.current = Mathf.Clamp(h.current,0, h.max);
        }
    }
}

sealed class TestInputSystem : ISystem {

    private IPool<InputData> inputs;
    private Query _query;
    private TestText text;
    public void OnCreate(World world) {
        _query = world.GetQuery().With<InputData>();
    }

    public void OnUpdate(float deltaTime) {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        var r = Input.GetKeyDown(KeyCode.R);

        foreach (ref var entity in _query) {
            ref var input = ref inputs.Get(ref entity);
            input.horizontal = h;
            input.vertical = v;
            input.r = r;
        }
    }
}

sealed class TweensTestSystem : ISystem {
    private Query _query;
    private IPool<Translation> translations;
    public void OnCreate(World world) {
        _query = world.GetQuery().With<Translation>();
        translations = world.GetPool<Translation>();
    }

    public void OnUpdate(float deltaTime) {
        if (Input.GetKeyDown(KeyCode.Space)) {
            foreach (var entity in _query) {

                // e.doScale(1, 5, 2f)
                //     .WithEasing(Easings.Type.QuadraticEaseOut)
                //     .Then(e.doRotation(new Vector3(0, 720, 0), 2f)
                //         .WithEasing(Easings.Type.QuadraticEaseOut)
                //         .Then(e.doScale(5, 1, 2f)
                //             .WithEasing(Easings.Type.QuadraticEaseOut)
                //             .OnComplete(() => {
                //                 e.Add<StaticTag>();
                //             })));

                ref var transform1 = ref translations.Get(entity.Index);
                entity.doScale(Vector3.one, new Vector3(2, 1, 2), .5f)
                    .WithLoop(2, LoopType.Yoyo)
                    .WithEasing(Easings.EasingType.ExponentialEaseInOut);

                entity.doRotation(Vector3.zero, new Vector3(0, 0, Random.value > .5f ? 360 : -360), 0.2f).WithLoop(4, LoopType.Restart);

                entity.doMove(transform1.position, transform1.position + Vector3.up*3, .5f)
                    .WithLoop(2,LoopType.Yoyo)
                    .WithEasing(Easings.EasingType.ExponentialEaseInOut);
            }
        }
    }
}
sealed class SyncTransformsSystem : ISystem {
    Query _query;
    IPool<TransformReference> transforms;
    IPool<Translation> translations;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<Translation>()
            .With<TransformReference>()
            .Without<StaticTag>();
            
        transforms = world.GetPool<TransformReference>();
        translations = world.GetPool<Translation>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (var entity in _query) {
            ref var transform = ref transforms.Get(entity.Index);
            ref var translation = ref translations.Get(entity.Index);
            transform.value.position = translation.position;
            transform.value.rotation = translation.rotation;
            transform.value.localScale = translation.scale;
        }
        //Debug.Log(_query.Count);
    }
}

public static class Animations
{
    public const string Idle = "Idle";
    public const string Run = "Run";
}
sealed class TestPlayerAnimationSystem : ISystem {
    private IPool<SpriteAnimation> animations;
    private IPool<InputData> inputs;
    private Query _query;
    public void OnCreate(World world) {
        _query = world.GetQuery().With<SpriteAnimation>().With<InputData>();
        animations = world.GetPool<SpriteAnimation>();
        inputs = world.GetPool<InputData>();
    }
    
    public void OnUpdate(float deltaTime) {
        
        foreach (ref var entity in _query) {
            ref var input = ref inputs.Get(ref entity);
            ref var animation = ref animations.Get(ref entity);

            if (input.horizontal < .1F && input.vertical < .1F && input.horizontal > -.1F && input.vertical > -.1F)
                animation.Play(Animations.Idle);
            else 
                animation.Play(Animations.Run);
            if (input.r) {
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

public struct Direction : IComponent {
    public float value;
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
