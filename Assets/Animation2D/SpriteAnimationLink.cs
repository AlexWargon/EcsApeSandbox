using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Wargon.Ecsape;

namespace Animation2D {
    public class SpriteAnimationLink : ComponentLink<SpriteAnimation> {
        
    }
    public partial struct AnimationState {
        public const int Idle = 0;
        
    }

    public partial struct AnimationState {
        public const int Run = 1;
    }
    public enum AnimationStateEnum {
        Idle = 0,
        Run = 1,
    }

    [Serializable]
    public struct SpriteAnimation : IComponent {
        public AnimationList AnimationList;
        public AnimationStateEnum currentState;
        public AnimationStateEnum nextState;
        public int frame;
        public int times;
    }

    public static class SpriteAnimationExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Play(this ref SpriteAnimation animation, AnimationStateEnum newState, int timesToPlay = Int32.MaxValue) {
            if (animation.currentState != newState) {
                animation.nextState = animation.currentState;
                animation.currentState = newState;
                animation.times = timesToPlay;
            }
        }

        public static void Sub(this ref SpriteAnimation animation, AnimationStateEnum state, int frame, Action callback) {
            AnimationEvents.Sub(animation.AnimationList.GetState((int)animation.currentState), frame, callback);
        }

    }
    [Serializable]
    public struct SpriteRender : IComponent {
        public SpriteRenderer value;

        public Sprite sprite {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => value.sprite;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.value.sprite = value;
        }
        
        public UnityEngine.Color color {
            get => value.color;
            set => this.value.color = value;
        }
        
        public bool flipX {
            get => value.flipX;
            set => this.value.flipX = value;
        }

        public bool flipY {
            get => value.flipY;
            set => this.value.flipY = value;
        }
    }
}
