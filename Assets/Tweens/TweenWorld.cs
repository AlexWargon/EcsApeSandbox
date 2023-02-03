using UnityEngine;
using Wargon.Ecsape;

namespace Wargon.Ecsape.Tween {
    public class TweenWorld : WorldHolder {
        private Systems systems;
        private void Awake() {
            world = World.GetOrCreate(World.TWEEN);
            systems = new Systems(world);
            systems
                .Add(new TweenAnimation())
                .Add<SyncTransformsTweenSystem>()
                .Init();
            //_fixedSystems = new Systems(world).Add<SyncTransformsTweenSystem>().Init();
        }

        private void Update() {
            systems.Update(Time.deltaTime);
        }
    }
}

public abstract class WorldHolder : MonoBehaviour {
    protected World world;
    [SerializeField] private int entitiesCount;
    [SerializeField] private int archetypesCount;

    private void LateUpdate() {
        entitiesCount = world.ActiveEntitiesCount;
        archetypesCount = world.ArchetypesCountInternal();
    }
}