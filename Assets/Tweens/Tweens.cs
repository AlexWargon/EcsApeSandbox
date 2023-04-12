using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Wargon.Ecsape.Tween {
    internal struct Target : IComponent {
        public Entity entity;
    }

    internal struct Duration : IComponent {
        public float value;
    }

    internal struct TweenProgress : IComponent {
        public float time;
        public float normalizedTime;
        public bool targetDestroyed;
    }

    internal struct Delay : IComponent {
        public float value;
    }

    internal struct TweenLoop : IComponent {
        public int count;
        public LoopType type;
    }

    public enum LoopType {
        Restart,
        Yoyo
    }

    internal struct Easing : IComponent {
        public Easings.EasingType EasingType;
    }

    public sealed class TweenProgressSystem : ISystem {
        private Query _query;
        private IPool<Duration> durations;
        private IPool<TweenProgress> progresses;
        private IPool<Target> targets;

        public void OnCreate(World world) {
            _query = world.GetQuery()
                .With<TweenProgress>()
                .With<Duration>()
                .With<Target>()
                .Without<TweenLoop>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (_query.IsEmpty) return;
            foreach (ref var entity in _query) {
                ref var progress = ref progresses.Get(ref entity);
                ref var duration = ref durations.Get(ref entity);
                ref var target = ref targets.Get(ref entity);
                if (target.entity.IsNull()) {
                    progress.targetDestroyed = true;
                    entity.Destroy();
                    continue;
                }

                progress.time += deltaTime;
                progress.normalizedTime = progress.time / duration.value;
                if (progress.normalizedTime > 1) {
                    progress.normalizedTime = 1;
                    entity.Destroy();
                }
            }
        }
    }

    internal sealed class TweenLoopProgressSystem : ISystem {
        private Query _query;
        private IPool<Duration> durations;
        private IPool<TweenLoop> loops;
        private IPool<TweenProgress> progresses;
        private IPool<Target> targets;

        public void OnCreate(World world) {
            _query = world.GetQuery()
                .With<Duration>()
                .With<TweenLoop>()
                .With<TweenProgress>()
                .With<Target>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (_query.IsEmpty) return;
            foreach (ref var entity in _query) {
                ref var progress = ref progresses.Get(ref entity);
                ref var target = ref targets.Get(ref entity);

                if (target.entity.IsNull()) {
                    progress.targetDestroyed = true;
                    entity.Destroy();
                    continue;
                }

                ref var duration = ref durations.Get(ref entity);
                ref var loop = ref loops.Get(ref entity);
                progress.time += deltaTime;

                var timeInLoop = progress.time % duration.value;
                var loopIndex = (int) (progress.time / duration.value);

                progress.normalizedTime = timeInLoop / duration.value;

                if (loop.count >= 0 && loopIndex >= loop.count) {
                    if (loop.type == LoopType.Yoyo)
                        progress.normalizedTime = loopIndex % 2 == 1 ? 1 : 0;
                    else
                        progress.normalizedTime = 1;
                    entity.Destroy();
                    continue;
                }

                if (loop.type == LoopType.Yoyo && loopIndex % 2 == 1)
                    progress.normalizedTime = 1 - progress.normalizedTime;
            }
        }
    }

    internal sealed class TweenDelaySystem : ISystem {
        private Query _query;
        private IPool<Delay> delays;
        private IPool<TweenProgress> tweenProgress;

        public void OnCreate(World world) {
            _query = world.GetQuery(typeof(Delay), typeof(TweenProgress));
        }

        public void OnUpdate(float deltaTime) {
            if (_query.IsEmpty) return;
            foreach (ref var entity in _query) {
                ref var progress = ref tweenProgress.Get(ref entity);
                ref var delay = ref delays.Get(ref entity);
                progress.time += deltaTime;
                if (progress.time >= delay.value) {
                    progress.time -= delay.value;
                    entity.Remove<Delay>();
                }
            }
        }
    }

    internal sealed class TweenEasingSystem : ISystem {
        private Query _query;
        private IPool<Easing> easings;
        private IPool<TweenProgress> progresses;

        public void OnCreate(World world) {
            _query = world.GetQuery()
                .With<TweenProgress>()
                .With<Easing>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (_query.IsEmpty) return;
            foreach (ref var entity in _query) {
                ref var progress = ref progresses.Get(ref entity);
                ref var easing = ref easings.Get(ref entity);

                progress.normalizedTime = Easings.Interpolate(progress.normalizedTime, easing.EasingType);
            }
        }
    }


    internal struct NextTween : IComponent {
        public Entity entity;
    }

    internal struct OnTweenComplete : IComponent { }

    internal struct OnTweenCopleteEntityAction : IComponent {
        public EntityAction action;
    }

    internal sealed class StartNextTweenSystem : ISystem {
        private Query _query;
        private IPool<DestroyEntity> destoyedEntity;
        private IPool<NextTween> nextTweens;

        public void OnCreate(World world) {
            _query = world.GetQuery().With<NextTween>().With<DestroyEntity>();
        }

        public void OnUpdate(float deltaTime) {
            if (_query.IsEmpty) return;
            foreach (ref var entity in _query) {
                ref var next = ref nextTweens.Get(ref entity);
                next.entity.Add<TweenProgress>();
            }
        }
    }

    internal sealed class ClearTransformsEntitySystem : ISystem {
        private Query query;
        private IPool<TransformUnityTweenTag> callbacks;
        private IPool<TweenProgress> progresses;
        private IPool<Target> targets;

        public void OnCreate(World world) {
            query = world.GetQuery().With<DestroyEntity>().With<TransformUnityTweenTag>().With<Target>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (ref var entity in query) {
                ref var target = ref targets.Get(ref entity);
                ref var progress = ref progresses.Get(ref entity);
                if (!progress.targetDestroyed) {
                    ref var tw = ref target.entity.Get<TweeningObject>();
                    tw.tweensCount--;
                    if (tw.tweensCount == 0) {
                        TransformTweenExtensions.Remove(target.entity.Get<TransformReferenceTween>().instanceID);
                        progress.targetDestroyed = true;
                        target.entity.Destroy();
                    }
                }
            }
        }
    }

    internal sealed class EntityCallbackTweenSystem : ISystem {
        private Query query;
        private IPool<OnTweenCopleteEntityAction> pool;
        private IPool<Target> targets;
        public void OnCreate(World world) {
            query = world.GetQuery().With<DestroyEntity>().With<OnTweenCopleteEntityAction>().With<Target>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var action = ref pool.Get(ref entity);
                ref var target = ref targets.Get(ref entity);
                action.action.Invoke(target.entity);
            }
        }
    }
    internal sealed class CallbackTweenSystem : ISystem {
        private static readonly Dictionary<int, Action> callbacksMap = new();
        private Query query;

        public void OnCreate(World world) {
            query = world.GetQuery().With<DestroyEntity>().With<OnTweenComplete>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (ref var entity in query) {
                callbacksMap[entity.Index]?.Invoke();
                callbacksMap[entity.Index] = null;
                callbacksMap.Remove(entity.Index);
            }
        }

        public static void AddCallback(int id, Action callback) {
            if (callbacksMap.ContainsKey(id)) {
                callbacksMap[id] = callback;
                return;
            }

            callbacksMap.Add(id, callback);
        }
    }

    internal struct TranslationTween : IComponent {
        public Vector3 endValue;
        public Vector3 startValue;
    }

    internal struct TransformUnityTweenTag : IComponent { }

    internal struct TransformReferenceTween : IComponent {
        public Transform value;
        public int instanceID;
    }

    internal struct TweeningObject : IComponent {
        public int tweensCount;
    }

    internal struct BlockedTween : IComponent { }

    internal sealed class ScaleTranslationTweenSystem : ISystem {
        private Query query;
        private IPool<TweenProgress> progresses;
        private IPool<TranslationTween> scaleTweens;
        private IPool<Target> targets;

        public void OnCreate(World world) {
            query = world.GetQuery()
                .With<TweenProgress>()
                .With<Target>()
                .With<TranslationTween>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (ref var entity in query) {
                ref var progress = ref progresses.Get(ref entity);
                if (progress.targetDestroyed) return;

                ref var targetEntity = ref targets.Get(ref entity).entity;
                ref var translation = ref targetEntity.Get<Translation>();
                ref var scaleTween = ref scaleTweens.Get(ref entity);
                translation.scale = Vector3.Lerp(scaleTween.startValue, scaleTween.endValue, progress.normalizedTime);
            }
        }
    }

    internal struct TranslationRotationTween : IComponent {
        public Vector3 endValue;
        public Vector3 startValue;
    }

    internal sealed class RotationTranslationTweenSystem : ISystem {
        private Query query;
        private IPool<TweenProgress> progresses;
        private IPool<TranslationRotationTween> scaleTweens;
        private IPool<Target> targets;

        public void OnCreate(World world) {
            query = world.GetQuery()
                .With<TweenProgress>()
                .With<Target>()
                .With<TranslationRotationTween>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (ref var entity in query) {
                ref var progress = ref progresses.Get(ref entity);
                if (progress.targetDestroyed) return;

                ref var targetEntity = ref targets.Get(ref entity).entity;
                ref var translation = ref targetEntity.Get<Translation>();
                ref var scaleTween = ref scaleTweens.Get(ref entity);
                translation.rotation = Quaternion.Euler(Vector3.Lerp(scaleTween.startValue, scaleTween.endValue,
                    progress.normalizedTime));
            }
        }
    }

    internal struct TranslationMoveTween : IComponent {
        public Vector3 endValue;
        public Vector3 startValue;
    }

    internal sealed class MoveTranslationTweenSystem : ISystem {
        private Query query;
        private IPool<TweenProgress> progresses;
        private IPool<TranslationMoveTween> moveTweens;
        private IPool<Target> targets;
        private IPool<Translation> translations;
        public void OnCreate(World world) {
            query = world.GetQuery()
                .With<TweenProgress>()
                .With<Target>()
                .With<TranslationMoveTween>()
                .Without<Delay>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (ref var entity in query) {
                ref var progress = ref progresses.Get(ref entity);
                if (progress.targetDestroyed) return;
            
                ref var targetEntity = ref targets.Get(ref entity).entity;
                ref var translation = ref targetEntity.Get<Translation>();
                ref var moveTween = ref moveTweens.Get(ref entity);
                translation.position =
                    Vector3.Lerp(moveTween.startValue, moveTween.endValue, progress.normalizedTime);
            }

            // var tweenJob = new MoveTweenJob{
            //     Query = query.AsNative(),
            //     progresses = progresses.AsNative(),
            //     moveTweens = moveTweens.AsNative(),
            //     targets = targets.AsNative(),
            //     translations = translations.AsNative(),
            // };
            // var handle =  tweenJob.Schedule(query.Count, 64);
            // handle.Complete();
        }
        
        private struct MoveTweenJob : IJobParallelFor {
            public NativeQuery Query;
            public NativePool<TweenProgress> progresses;
            public NativePool<TranslationMoveTween> moveTweens;
            public NativePool<Target> targets;
            public NativePool<Translation> translations;
            public void Execute(int index) {
                var e = Query.GetEntity(index);
                
                ref var progress = ref progresses.Get(e);
                if (progress.targetDestroyed) return;
                ref var targetEntity = ref targets.Get(e).entity;
                ref var translation = ref translations.Get(targetEntity.Index);
                ref var moveTween = ref moveTweens.Get(e);
                translation.position =
                    Vector3.Lerp(moveTween.startValue, moveTween.endValue, progress.normalizedTime);
            }
        }
    }

    internal struct PauseTween : IComponent {
        public float time;
        public float incertTime;
    }

    internal sealed class PauseTweenSystem : ISystem {
        private Query query;
        private IPool<PauseTween> pool;
        private IPool<TweenProgress> progresses;

        public void OnCreate(World world) {
            query = world.GetQuery().With<PauseTween>().With<TweenProgress>();
            pool = world.GetPool<PauseTween>();
            progresses = world.GetPool<TweenProgress>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var progress = ref progresses.Get(ref entity);
                ref var pause = ref pool.Get(ref entity);

                if (progress.time >= pause.incertTime) {
                    entity.Add(new Delay {value = pause.time});
                    entity.Remove<PauseTween>();
                }
            }
        }
    }

    internal sealed class SyncTransformsTweenSystem : ISystem {
        private Query query;
        private IPool<TransformReferenceTween> transforms;
        private IPool<Translation> translations;

        public void OnCreate(World world) {
            query = world.GetQuery()
                .With<Translation>()
                .With<TransformReferenceTween>()
                .Without<StaticTag>();
        }

        public void OnUpdate(float deltaTime) {
            if (query.IsEmpty) return;
            foreach (var entity in query) {
                ref var transform = ref transforms.Get(entity.Index);
                ref var translation = ref translations.Get(entity.Index);
                transform.value.position = translation.position;
                transform.value.rotation = translation.rotation;
                transform.value.localScale = translation.scale;
            }
            //Debug.Log(_query.Count);
        }
    }

    public class TweenAnimation : Systems.Group {
        public TweenAnimation() : base("TweenSystems") {
                Add<TweenDelaySystem>()
                .Add<TweenProgressSystem>()
                .Add<TweenLoopProgressSystem>()
                .Add<TweenEasingSystem>()
                .Add<PauseTweenSystem>()
                .Add<ScaleTranslationTweenSystem>()
                .Add<RotationTranslationTweenSystem>()
                .Add<MoveTranslationTweenSystem>()
                .Add<StartNextTweenSystem>()
                .Add<EntityCallbackTweenSystem>()
                .Add<CallbackTweenSystem>()
                .Add<ClearTransformsEntitySystem>()
                ;
        }
    }
}