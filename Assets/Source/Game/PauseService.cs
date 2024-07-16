using System.Linq;
using UnityEngine;

namespace Rogue {
    public sealed class PauseService : IPauseSerivce {
        private IPauseble[] _pausebles;

        public PauseService() {
            _pausebles = Object.FindObjectsOfType<MonoBehaviour>(true).OfType<IPauseble>().ToArray();
            SceneAPI.OnLoad(((scene, mode) => {
                _pausebles = Object.FindObjectsOfType<MonoBehaviour>(true).OfType<IPauseble>().ToArray();
            }));
        }
        
        void IPauseSerivce.Pause() {
            Time.timeScale = 0f;
            foreach (var pauseble in _pausebles) {
                pauseble.Pause(true);
            }
        }

        void IPauseSerivce.Unpause() {
            Time.timeScale = 1f;
            foreach (var pauseble in _pausebles) {
                pauseble.Pause(false);
            }
        }
    }
    public interface IPauseble {
        public bool Paused { get; set; }
        public void Pause(bool value) {
            Paused = value;
        }
    }

    public interface IPauseSerivce {
        void Pause();
        void Unpause();
    }
}