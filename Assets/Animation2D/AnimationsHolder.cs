using System.Collections.Generic;
using UnityEngine;

namespace Animation2D {
    [CreateAssetMenu(fileName = "AnimationsHolder", menuName = "ScriptableObjects/AnimationsHolder", order = 1)]
    public class AnimationsHolder : ScriptableObject {
        public List<AnimationList> Animations;
        public float FrameTime = 0.1f;
        public static AnimationsHolder Instance;
        public void Init() {
            foreach (var animationList in Animations) {
                animationList.Init();
            }

            Instance = this;
        }
    }
}