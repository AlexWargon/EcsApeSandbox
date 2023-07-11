using Animation2D;
using Wargon.Ecsape;

namespace Rogue {
    sealed class PlayerAnimationSystem : ISystem {
        private IPool<SpriteAnimation> animations;
        private IPool<InputData> inputs;
        private Query query;
        public void OnCreate(World world) {
            query = world.GetQuery().WithAll<SpriteAnimation,InputData>();
        }
    
        public void OnUpdate(float deltaTime) {
        
            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                ref var animation = ref animations.Get(ref entity);

                if (input.horizontal < .1F && input.vertical < .1F && input.horizontal > -.1F && input.vertical > -.1F)
                    animation.Play(Animations.Idle);
                else 
                    animation.Play(Animations.Run);
                // if (input.fire) {
                //     animation.Play(Animations.Run,4);
                // }
            }
        }
    }
}