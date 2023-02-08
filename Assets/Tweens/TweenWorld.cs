using UnityEngine;

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