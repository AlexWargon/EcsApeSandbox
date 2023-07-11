using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    public class EnemySpawner : MonoBehaviour
    {
        public EntityLink prefab;
        public Transform[] points;
        public float TIME = 0.5f;
        private float time;
        
        void Update() {
            if (time > 0) {
                time -= Time.deltaTime;
            }
            else {
                time = TIME;
                EntityLink.Spawn(prefab, points[Random.Range(0, points.Length - 1)].position, Quaternion.identity, World.Default);
            }
        }
    }
}

