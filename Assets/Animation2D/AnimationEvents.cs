using System;
using System.Collections.Generic;

namespace Animation2D {
    public static class AnimationEvents {
        private static Dictionary<(string, int), Action> _actions =
            new Dictionary<(string, int), Action>();

        public static void Sub(Animation2DFrames frames, int frame, Action callback) {
            var key = (frames.State, frame);
            if (_actions.ContainsKey(key)) {
                _actions[key] += callback;
            }
            else {
                _actions.Add(key, callback);
            }
        }

        public static void Invoke(string state, int frameNumber) {
            var key = (state, frameNumber);
            if (_actions.ContainsKey(key)){
                _actions[key].Invoke();
            }
        }
    }
}