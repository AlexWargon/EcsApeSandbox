using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Wargon.NEZS {
    public class Systems {
        public JobHandle Dependencies;
        public World World;
        public ISystemRunner[] ParallelSystems;
        public SystemRunnerMainThread[] MainThreadSystems;
        public int parallelSystemsCount;
        public int mainThreadSystemsCount;
        public State state;
        public Systems(ref World world) {
            Dependencies = default;
            World = world;
            ParallelSystems = new ISystemRunner[32];
            MainThreadSystems = new SystemRunnerMainThread[32];
            parallelSystemsCount = 0;
            mainThreadSystemsCount = 0;
        }
        public unsafe Systems Add<TSystem>() where TSystem : unmanaged, ISystem {
            TSystem* system = (TSystem*)UnsafeUtility.Malloc(sizeof(TSystem), UnsafeUtility.AlignOf<TSystem>(), World.Allocator);
            system->OnCreate(ref World);
            SystemRunner<TSystem> systemRunner = new SystemRunner<TSystem> {
                SystemPtr = system
            };
            ParallelSystems[parallelSystemsCount++] = systemRunner;
            return this;
        }
        public unsafe Systems Add<TSystem>(in TSystem systemInstance, in SystemRunner<TSystem>.JobSystemParallel jobSystemParallel) where TSystem : unmanaged, ISystem {
            TSystem* systemPtr = (TSystem*)UnsafeUtility.Malloc(sizeof(TSystem), UnsafeUtility.AlignOf<TSystem>(), World.Allocator);
            Marshal.StructureToPtr(systemInstance, (IntPtr)systemPtr, true);
            var query = CreateRootQuery<TSystem>();
            systemPtr->OnCreate(ref World);
            SystemRunner<TSystem> systemRunner = new SystemRunner<TSystem> {
                SystemPtr = systemPtr,
                Query = query,
                Runner = jobSystemParallel
            };
            ParallelSystems[parallelSystemsCount++] = systemRunner;
            return this;
        }
        public Systems Add<TSystem>(TSystem systemInstance) where TSystem : class, ISystemMainThread{
            systemInstance.OnCreate(ref World);
            var query = CreateRootQuery(typeof(TSystem));
            SystemRunnerMainThread runner = new SystemRunnerMainThread();
            runner.query = query;
            runner.system = systemInstance;
            MainThreadSystems[mainThreadSystemsCount++] = runner;
            return this;
        }
        private Query CreateRootQuery(Type systemType){
            var onUpdateMethod = systemType.GetMethod("OnUpdate");
            var parameters = onUpdateMethod.GetParameters();
            var query = World.GetQuery();
            foreach (var parameterInfo in parameters) {
                if (parameterInfo.ParameterType == typeof(Query)) {
                    var paramAttribute = parameterInfo.GetCustomAttributes(true);
                    // foreach (var attribute in paramAttribute) {
                    //     if (attribute is WithAttribute withAttribute) {
                    //         foreach (var type in withAttribute.TypesArray) {
                    //             query = query.With(type);
                    //         }
                    //     }
                    //     else
                    //     if (attribute is NoneAttribute noneAttribute) {
                    //         foreach (var type in noneAttribute.TypesArray) {
                    //             query = query.None(type);
                    //         }
                    //     }
                    //     else
                    //     if (attribute is AnyAttribute anyAttribute) {
                    //         foreach (var type in anyAttribute.TypesArray) {
                    //             query = query.Any(type);
                    //         }
                    //     }
                    // }
                }
            }

            return query;
        }
        private Query CreateRootQuery<TSystem>() where TSystem : unmanaged, ISystem {
            var systemType = typeof(TSystem);
            var onUpdateMethod = systemType.GetMethod("OnUpdate");
            var parameters = onUpdateMethod.GetParameters();
            var query = World.GetQuery();
            foreach (var parameterInfo in parameters) {
                if (parameterInfo.ParameterType == typeof(Query)) {
                    var paramAttribute = parameterInfo.GetCustomAttributes(true);
                    foreach (var attribute in paramAttribute) {
                        // if (attribute is WithAttribute withAttribute) {
                        //     foreach (var type in withAttribute.TypesArray) {
                        //         query = query.With(type);
                        //     }
                        // }
                        // else
                        // if (attribute is NoneAttribute noneAttribute) {
                        //     foreach (var type in noneAttribute.TypesArray) {
                        //         query = query.None(type);
                        //     }
                        // }
                        // else
                        // if (attribute is AnyAttribute anyAttribute) {
                        //     foreach (var type in anyAttribute.TypesArray) {
                        //         query = query.Any(type);
                        //     }
                        // }
                    }
                }
            }

            return query;
        }

        public void Update(float deltaTime) {
            Dependencies = default;
            state.World = this.World;
            state.DeltaTime = deltaTime;
            for (var i = 0; i < parallelSystemsCount; i++) {
                ref var runner = ref ParallelSystems[i];
                runner.Prepare(ref state);
                Dependencies = runner.Schedule(Dependencies);
            }
            Dependencies.Complete();
            for (var i = 0; i < mainThreadSystemsCount; i++) {
                ref var runner = ref MainThreadSystems[i];
                runner.Prepare(ref state);
                runner.Execute();
            }
        }

        public void Dispose() {
            for (int i = 0; i < parallelSystemsCount; i++) {
                ParallelSystems[i].Dispose();
            }
        }
    }
    [BurstCompile]
    public unsafe struct FilterDirtyEntitiesJob : IJob {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<DirtyEntity>* commands;
        public void Execute() {
            if (commands->IsEmpty) {
                //Debug.Log("BUFFER EMPTY");
                return;
            }
            //Debug.Log($"BUFFER SIZE : {commands->Length}");
            for (int index = 0; index < commands->Length; index++) {
                ref var dirty = ref commands->ElementAt(index);
                dirty.Edge.Execute(dirty.Entity);
            }
            commands->Clear();
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class WithAttribute : Attribute {
        public ComponentType[] TypesArray;

        public WithAttribute(params ComponentType[] types) {
            TypesArray = types;
        }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public class NoneAttribute : Attribute {
        public Type[] TypesArray;

        public NoneAttribute(params Type[] types) {
            TypesArray = types;
        }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AnyAttribute : Attribute {
        public Type[] TypesArray;

        public AnyAttribute(params Type[] types) {
            TypesArray = types;
        }
    }
    public interface ISystem {
        Query Query { get; set; }
        void OnCreate(ref World world);
        void OnUpdate(ref State state ,int index);
    }
    public interface ISystemMainThread : ISystem{}
    public interface ISystemRunner : IDisposable {
        void Prepare(ref State state);
        JobHandle Schedule(JobHandle handle);
    }


    public class SystemRunnerMainThread {
        internal ISystemMainThread system;
        internal State state;
        internal Query query;
        public void Prepare(ref State state) {
            this.state = state;
        }
        public void Execute() {
            query = system.Query;
            for (var i = 0; i < query.Count; i++) {
                system.OnUpdate(ref state, i);
            }
        }
    }
    public class SystemRunner<TSystem> : ISystemRunner where TSystem : unmanaged, ISystem {
        internal JobSystemParallel Runner;
        internal FilterDirtyEntitiesJob FilterJob;
        internal State State;
        internal Query Query;
        internal unsafe TSystem* SystemPtr;
        public void Prepare(ref State state) {
            this.State = state;
        }
        public JobHandle Schedule(JobHandle handle) {
            unsafe {
                Runner = new JobSystemParallel {
                    system = SystemPtr,
                    state = State,
                    query = SystemPtr->Query
                };
                FilterJob = new FilterDirtyEntitiesJob {
                    commands = State.World.worldInternal->dirtyEntities
                };
                Query = SystemPtr->Query;
            }
            var localHandle = Runner.Schedule(Query.Count, 1, handle);
            localHandle = FilterJob.Schedule(localHandle);
            return localHandle;
        }

        public void Dispose() {
            unsafe {
                UnsafeUtility.Free(SystemPtr, Allocator.Persistent);
            }
        }
        [BurstCompile]
        public unsafe struct JobSystemParallel : IJobParallelFor {
            [NativeDisableUnsafePtrRestriction]
            internal TSystem* system;
            internal State state;
            internal Query query;
            public void Execute(int index) {
                system->OnUpdate(ref state, index);
            }
        }
    }
    public struct State {
        public World World;
        public float DeltaTime;
    }
}