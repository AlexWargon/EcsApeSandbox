using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.UI;

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
        systems = new Systems(world);
    }

    void Update()
    {
        systems.Update(Time.deltaTime);
    }
}

struct OnHitAbility : IComponent { }
struct ModList : IComponent {
    public List<Entity> value;
}

struct Owner : IComponent {
    public Entity Entity;
}

struct OnHitEvent : IComponent {
    public Vector2 Position;
    public int Damage;
}
sealed class TriggerModsSystem : ISystem {
    private Query Query;
    private IPool<ModList> mods;
    public void OnCreate(World world) {
        Query = world.GetQuery().With<ShotEvent>().With<ModList>();
    }
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in Query) {
            ref var mod = ref mods.Get(ref entity);
            for (var i = 0; i < mod.value.Count; i++) {
                mod.value[i].Add<ShotEvent>();
            }
        }
    }
}

sealed class OnHitAbilityTriggerSystem : ISystem {
    private Query Query;
    private IPool<OnHitAbility> mods;
    public void OnCreate(World world) {
        Query = world.GetQuery().With<OnHitAbility>().With<OnHitEvent>();
    }
    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in Query) {
            ref var mod = ref mods.Get(ref entity);
        }}}