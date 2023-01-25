using UnityEngine;

namespace Animation2D {
    [CreateAssetMenu(fileName = "Animation2D", menuName = "ScriptableObjects/Animation2D", order = 1)]
    public class Animation2DFrames : ScriptableObject {
        public string State;
        public Sprite[] Frames;
        
        private void OnValidate() {
            State = name;
        }
    }
}