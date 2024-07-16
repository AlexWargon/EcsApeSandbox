using Unity.Burst;
using UnityEngine;

namespace Wargon.NEZS.Tests {
    
    public class NativeMainTest : MonoBehaviour {
        private Systems _systems;
        private World _world;
        [SerializeField] private Transform cube;
        [SerializeField] private Transform bullet;
        void Start() {
            var config = WorldConfig.Default;
            config.DirtyEntitiesCache = 100000;
            _world = Worlds.Create(in config);
            _systems = new Systems(ref _world);
            _systems
                .Add(new SyncTransforms())
                .Add(new InputSystem())
                .Add(new PositionSystem(), new SystemRunner<PositionSystem>.JobSystemParallel())
                .Add(new LifeTimeSystem(), new SystemRunner<LifeTimeSystem>.JobSystemParallel())
                ;
            SpawnCubesEntities(10000);
        }
        void Update()
        {
            _systems.Update(Time.deltaTime);
        }

        void SpawnCubesEntities(int amount) {
            for (int i = 0; i < amount; i++) {
                ref Entity e = ref _world.CreateEntity();
                var spawnerCube = Instantiate(cube);
                spawnerCube.name = $"e:{e.Index}";
                e.Add(new TransformReference{value = spawnerCube});
                e.Add(new Position{value = RandomUtility.Vector3(-10f,10f)});
                e.Add(new MoveSpeed{value = 12});
                e.Add(new Input{value = Vector2.zero});
                e.Add(new Lifetime{value = Random.Range(5f,15f)});
            }
        }
        private void OnDestroy() {
            _systems.Dispose();
            _world.Dispose();
            Worlds.Dispose();
        }
    }

    public static class RandomUtility {
        public static Vector3 Vector3(float min, float max) {
            return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
        }
    }

    public class SyncTransforms : ISystemMainThread {
        public Query Query { get; set; }

        public void OnCreate(ref World world) {
            Query = world.GetQuery().With<Position>().With<TransformReference>();
        }
        public void OnUpdate(ref State state, int index) {
            ref var e = ref Query.GetEntity(index);
            ref var pos = ref e.Get<Position>();
            e.Get<TransformReference>().value.position = pos.value;
        }
    }

    public class InputSystem : ISystemMainThread {
        public Query Query { get; set; }

        public void OnCreate(ref World world) {
            Query = world.GetQuery().With<Input>();
        }

        public void OnUpdate(ref State state, int index) {
            ref var entity = ref Query.GetEntity(index);
            ref var input = ref entity.Get<Input>();
            if (UnityEngine.Input.GetKey(KeyCode.D)) input.value.x = 1;
            else if (UnityEngine.Input.GetKey(KeyCode.A)) input.value.x = -1;
            else input.value.x = 0;
        }
    }
    [BurstCompile]
    public struct PositionSystem : ISystem {
        public Query Query { get; set; }

        public void OnCreate(ref World world) {
            Query = world.GetQuery().With<Position>().With<MoveSpeed>().With<Input>().None<Inactive>();
        }
        [BurstCompile]
        public void OnUpdate(ref State state, int index) {
            ref var e = ref Query.GetEntity(index);
            ref var pos = ref e.Get<Position>();
            ref var moveSpeed = ref e.Get<MoveSpeed>();
            ref var input = ref e.Get<Input>();
            pos.value.x += moveSpeed.value * state.DeltaTime * input.value.x;
            if(pos.value.x > 15F) e.Remove<Input>();
        }
    }
    [BurstCompile]
    public struct LifeTimeSystem : ISystem {
        public Query Query { get; set; }
        public void OnCreate(ref World world) {
            Query = world.GetQuery().With<Lifetime>().None<Inactive>();
        }
        [BurstCompile]
        public void OnUpdate(ref State state, int index) {
            ref var e = ref Query.GetEntity(index);
            ref var lifeTime = ref e.Get<Lifetime>();
            if (lifeTime.value > 0) {
                lifeTime.value -= state.DeltaTime;
            }
            else {
                e.Add(new Inactive());
            }
        }
    }
    
    public struct Position {
        public Vector3 value;
    }

    public struct Input {
        public Vector2 value;
    }
    public struct MoveSpeed {
        public float value;
    }
    public struct StaticTag{}

    public struct TransformReference {
        public Transform value;
    }
    public struct Bullet{}
    public struct Inactive{}

    public struct Lifetime {
        public float value;
    }
}

