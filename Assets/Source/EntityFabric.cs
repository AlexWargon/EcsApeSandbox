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
        var e = world.CreateEntity(new TransformReference {value = go},
            new Translation {position = position, rotation = rotation, scale = go.localScale});
        return e;
    }

    public Entity Instantiate(EntityLink view, Vector3 position, Quaternion rotation) {
        var go = pool.Spawn(view, position, rotation);
        //go.Entity.Get<Translation>().rotation = rotation;
        // return go.Entity;
        //world.CreateEntity().Add(new PoolCommandSpawn{Prefab = view, position = position, rotation = rotation});
        return go.Entity; 
    }

}
public interface IEntityFabric {
    IEntityFabric Init(World world);
    Entity Instantiate(Transform view, Vector3 position, Quaternion rotation);

    Entity Instantiate(EntityLink view, Vector3 position, Quaternion rotation);

}