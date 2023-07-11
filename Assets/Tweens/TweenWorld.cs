using UnityEngine;

namespace Wargon.Ecsape.Tween {
    public class TweenWorld : WorldHolder {
        private Systems systems;
        private void Awake() {
            world = World.GetOrCreate(World.TWEEN);

            world
                .AddGroup(new TweenAnimation())
                //.Add<SyncTransformsTweenSystem>()
                .Init();
            //_fixedSystems = new Systems(world).Add<SyncTransformsTweenSystem>().Init();
        }

        private void Update() {
            world.OnUpdate(Time.unscaledDeltaTime);
        }
    }
}