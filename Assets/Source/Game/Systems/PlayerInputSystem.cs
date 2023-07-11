using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
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
                input.fire = true;
            }
        }
    }
}