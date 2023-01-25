using UnityEngine;
using Wargon.Ecsape;

namespace Wargon.Ecsape.Tweens {
    public class TweensWorld : WorldHolder {
        private Systems _systems;
        private Systems _fixedSystems;
        private void Awake() {
            world = Worlds.Get(Worlds.Tween);
            _systems = new Systems(world);
            _systems
                .Add(new TweenAnimation())
                
                .Init();
        
            _fixedSystems = new Systems(world).Add<SyncTransformsTweenSystem>().Init();
        }

        private void Update() {
            _systems.Update(Time.deltaTime);
        }

        private void FixedUpdate() {
            _fixedSystems.Update(Time.fixedDeltaTime);
        }
    }
}

public abstract class WorldHolder : MonoBehaviour {
    protected World world;
    [SerializeField] private int entitiesCount;

    private void LateUpdate() {
        entitiesCount = world.ActiveEntitiesCount;
    }
}