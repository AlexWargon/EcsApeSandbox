using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Animation2D {
    [CreateAssetMenu(fileName = "AnimationsHolder", menuName = "ScriptableObjects/AnimationsHolder", order = 1)]
    public class AnimationsHolder : ScriptableObject {
        public List<AnimationList> Animations;
        public float FrameTime = 0.1f;
        private static AnimationsHolder instance;
        public static AnimationsHolder Instance {
            get {
                if (instance == null) {
                    instance = Resources.FindObjectsOfTypeAll<AnimationsHolder>()[0];
                    instance.Init();
                }

                return instance;
            }
        }
        [SerializeField] private TextAsset AnimationsConstFile;
        [SerializeField][HideInInspector] private List<string> names;
        private HashSet<string> toGenerate = new ();
        private string top = @"
namespace Animation2D {
    public static partial class Animations
    {
";

        private string bot = @"    
    }
}";
        public void Init() {
            foreach (var animationList in Animations) {
                animationList.Init();
                foreach (var animation2DFrames in animationList.List) {
                    toGenerate.Add(animation2DFrames.State);
                }
            }
            instance = this;
        }

        private void Awake() {
            Instance.RegenerateFile();
        }

        public void RegenerateFile() {

            var source = string.Empty;
            source += top;
            foreach (var s in toGenerate) {
                source += GenerateField(s);
                source += Environment.NewLine;
            }
            
            source += bot;
#if UNITY_EDITOR
            File.WriteAllText(AssetDatabase.GetAssetPath(AnimationsConstFile), source);
#endif
            Debug.Log("[Animations const Re-Generated]");

        }

        private string GenerateField(string state) {
            const string quote = "\"";
            return @$"      public const string {state} = {quote}{state}{quote};";
        }
    }
    
}