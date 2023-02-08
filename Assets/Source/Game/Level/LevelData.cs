using System;
using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;

namespace Game {
    public class LevelData : ScriptableObject {
        [SerializeField] private List<Wave> Waves;
        private Wave current;
        private int currentIndex;
        private float delay;
        private int spawnedInWave;
        private IObjectPool pool;
        public void OnUpdate(float deltaTime) {
            if (current == null) {
                current = Waves[0];
            }

            delay += deltaTime;
            if (delay > current.SpawnDelay) {
                pool.Spawn(current.Enemy.transform, Vector3.back, Quaternion.identity);

                spawnedInWave++;
                if (current.Amount == spawnedInWave) {
                    currentIndex++;
                    current = Waves[currentIndex];
                }
                delay = 0;
            }
        }
    }

    [Serializable]
    public class Wave {
        public EntityLink Enemy;
        public int Amount;
        public float SpawnDelay;
    }
}

