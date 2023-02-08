using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;

public sealed class EntityFabric : IEntityFabric {
    private World world;
    private IObjectPool pool;
    public IEntityFabric Init(World world) {
        this.world = world;
        DI.Build(this);
        return this;
    }
    public Entity Instantiate(Transform view, Vector3 position, Quaternion rotation) {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.localScale});
        return e;
    }

    public Entity Instantiate<TC1>(Transform view, Vector3 position, Quaternion rotation, TC1 component1) 
        where TC1 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.localScale}, in component1);
        return e;
    }

    public Entity Instantiate<TC1, TC2>(Transform view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2) 
        where TC1 : struct, IComponent 
        where TC2 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.localScale}, in component1, in component2);
        return e;
    }

    public Entity Instantiate<TC1, TC2, TC3>(Transform view, Vector3 position, Quaternion rotation, TC1 component1,
        TC2 component2, TC3 component3) 
        where TC1 : struct, IComponent 
        where TC2 : struct, IComponent 
        where TC3 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.localScale}, in component1, in component2, in component3);
        return e;
    }

    public Entity Instantiate(EntityLink view, Vector3 position, Quaternion rotation) {
        var go = pool.Spawn(view, position, rotation);
        return go.Entity;
    }

    public Entity Instantiate<TC1>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1) 
        where TC1 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = go.Entity;
        e.Add(component1);

        return e;
    }

    public Entity Instantiate<TC1, TC2>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2) 
        where TC1 : struct, IComponent 
        where TC2 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go.transform, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.transform.localScale}, in component1, in component2);
        return e;
    }

    public Entity Instantiate<TC1, TC2, TC3>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2,
        TC3 component3) 
        where TC1 : struct, IComponent 
        where TC2 : struct, IComponent 
        where TC3 : struct, IComponent {
        
        var go = pool.Spawn(view, position, rotation);
        var e = world.CreateEntity(new TransformReference {value = go.transform, instanceID = view.GetInstanceID()},
            new Translation {position = position, rotation = rotation, scale = go.transform.localScale}, in component1, in component2, in component3);
        return e;
    }
}
public interface IEntityFabric {
    IEntityFabric Init(World world);
    Entity Instantiate(Transform view, Vector3 position, Quaternion rotation);
    Entity Instantiate<TC1>(Transform view, Vector3 position, Quaternion rotation, TC1 component1) 
        where TC1 : struct, IComponent;
    Entity Instantiate<TC1, TC2>(Transform view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2) 
        where TC1 : struct, IComponent
        where TC2 : struct, IComponent;
    Entity Instantiate<TC1, TC2, TC3>(Transform view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2, TC3 component3) 
        where TC1 : struct, IComponent
        where TC2 : struct, IComponent
        where TC3 : struct, IComponent;
    
    Entity Instantiate(EntityLink view, Vector3 position, Quaternion rotation);
    Entity Instantiate<TC1>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1) 
        where TC1 : struct, IComponent;
    Entity Instantiate<TC1, TC2>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2) 
        where TC1 : struct, IComponent
        where TC2 : struct, IComponent;
    Entity Instantiate<TC1, TC2, TC3>(EntityLink view, Vector3 position, Quaternion rotation, TC1 component1, TC2 component2, TC3 component3) 
        where TC1 : struct, IComponent
        where TC2 : struct, IComponent
        where TC3 : struct, IComponent;
}