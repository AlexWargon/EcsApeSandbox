using UnityEngine;

namespace Animation2D {
    [CreateAssetMenu(fileName = "Animation2D", menuName = "ScriptableObjects/Animation2D", order = 1)]
    public class Animation2DFrames : ScriptableObject {
        public AnimationStateEnum State;
        public Sprite[] Frames;
    }
}