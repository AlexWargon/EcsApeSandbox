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

public struct AbilityList : IComponent {
    public List<Entity> AbilityEntities;
}

public struct OnKillEvent : IComponent {
    public Entity killedTarget;
}

public struct OnHitEvent : IComponent {
    public Vector2 Position;
    public int Damage;
}

public struct OnTakeDamageEvent : IComponent {
    
}
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
    public void OnCreate(World world) {
        OnShotEvents = world.GetQuery().WithAll<ShotEvent, AbilityList>();
        OnHitEvents = world.GetQuery().WithAll<OnHitEvent, AbilityList>();
        OnDamageEvents = world.GetQuery().WithAll<OnDamageEvent, AbilityList>();
        OnKillEvents = world.GetQuery().WithAll<OnKillEvent, AbilityList>();
        OnCritEvents = world.GetQuery().WithAll<OnCritEvent, AbilityList>();
        OnTakeDamageEvents = world.GetQuery().WithAll<OnTakeDamageEvent, AbilityList>();
    }
    public void OnUpdate(float deltaTime) {
        if(!OnShotEvents.IsEmpty)
            foreach (ref var entity in OnShotEvents) {
                ref var list = ref abilities.Get(ref entity);
                for (var i = 0; i < list.AbilityEntities.Count; i++) {
                    list.AbilityEntities[i].Add<ShotEvent>();
                }
            }
        if(!OnHitEvents.IsEmpty)
            foreach (ref var entity in OnHitEvents) {
                ref var mod = ref abilities.Get(ref entity);
                for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                    mod.AbilityEntities[i].Add<OnHitEvent>();
                }
            }
        if(!OnDamageEvents.IsEmpty)
            foreach (ref var entity in OnDamageEvents) {
                ref var mod = ref abilities.Get(ref entity);
                for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                    mod.AbilityEntities[i].Add<OnDamageEvent>();
                }
            }
        if(!OnKillEvents.IsEmpty)
            foreach (ref var entity in OnKillEvents) {
                ref var mod = ref abilities.Get(ref entity);
                for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                    mod.AbilityEntities[i].Add<OnKillEvent>();
                }
            }
        if(!OnCritEvents.IsEmpty)
            foreach (ref var entity in OnCritEvents) {
                ref var mod = ref abilities.Get(ref entity);
                for (var i = 0; i < mod.AbilityEntities.Count; i++) {
                    mod.AbilityEntities[i].Add<OnCritEvent>();
                }
            }
        if(!OnTakeDamageEvents.IsEmpty)
            foreach (ref var entity in OnTakeDamageEvents) {
                ref var mod = ref abilities.Get(ref entity);
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
        