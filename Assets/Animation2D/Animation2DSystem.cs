using System;
using System.Collections.Generic;
using Wargon.Ecsape;

namespace Animation2D {
    struct SpriteAnimationAspect : IAspect {
        public IEnumerable<Type> Link() {
            return new[] { typeof(SpriteAnimation), typeof(SpriteRender) };
        }
    }
    public sealed class Animation2DSystem : ISystem {
        private float frameTime => AnimationsHolder.Instance.FrameTime;
        private float time;
        private Query _query;
        private IPool<SpriteAnimation> animations;
        private IPool<SpriteRender> renders;
        public void OnCreate(World world) {
            _query = world.GetQuery().Aspect<SpriteAnimationAspect>();
        }

        public void OnUpdate(float deltaTime) {

            if ((time += deltaTime) < frameTime) return;
            time -= frameTime;

            foreach (var entity in _query) {
                ref var animation = ref animations.Get(entity.Index);
                ref var render = ref renders.Get(entity.Index);
                ref var frames = ref animation.AnimationList.GetState((int)animation.currentState).Frames;

                if (animation.frame >= frames.Length) {
                    if (--animation.times <= 0) {
                        if (animation.currentState != animation.nextState) {
                            animation.Play(animation.nextState);
                            frames = ref animation.AnimationList.GetState((int)animation.currentState).Frames;
                        }
                    }
                    animation.frame = 0;
                }
                
                AnimationEvents.Invoke(animation.currentState, animation.frame);
                render.value.sprite = frames[animation.frame++];
            }
        }
    }
}