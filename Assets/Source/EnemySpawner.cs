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
                var offset = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                var e = EntityLink.Spawn(prefab, points[Random.Range(0, points.Length)].position + offset, Quaternion.identity, World.Default);

                // if (Random.value > .95f) {
                //     e.Get<Wargon.Ecsape.Components.Translation>().scale = Vector3.one * 3;
                //     ref var h = ref e.Get<Health>();
                //     h.max = 400;
                //     h.current = 400;
                // }
            }
        }
    }
}

