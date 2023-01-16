using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wargon.Ecsape.Tweens {
    struct Target : IComponent {
    public Entity entity;
}

struct Duration : IComponent {
    public float value;
}

struct TweenProgress : IComponent {
    public float time;
    public float normalizedTime;
    public bool targedDestroyed;
}

struct Delay : IComponent {
    public float value;
}

struct TweenLoop : IComponent {
    public int count;
    public LoopType type;
}
public enum LoopType {
    Rastart,
    Yoyo
}

struct Easing : IComponent {
    public Easings.EasingType EasingType;
}

public sealed class TweenProgressSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Duration> durations;
    private IPool<Target> targets;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<TweenProgress>()
            .With<Duration>()
            .With<Target>()
            .Without<TweenLoop>()
            .Without<Delay>();
        progresses = world.GetPool<TweenProgress>();
        durations = world.GetPool<Duration>();
        targets = world.GetPool<Target>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            
            ref var progress = ref progresses.Get(ref entity);
            ref var duration = ref durations.Get(ref entity);
            ref var target = ref targets.Get(ref entity);
            if (target.entity.IsNull()) {
                progress.targedDestroyed = true;
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

sealed class TweenLoopProgressSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Duration> durations;
    private IPool<Target> targets;
    private IPool<TweenLoop> loops;
    public void OnCreate(World world) {
        _query = world.GetQuery(typeof(TweenProgress),typeof(Duration),typeof(TweenLoop),typeof(Target))
            .Without<Delay>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            ref var progress = ref progresses.Get(ref entity);
            ref var target = ref targets.Get(ref entity);
            
            if (target.entity.IsNull()) {
                progress.targedDestroyed = true;
                entity.Destroy();
                continue;
            }
            
            ref var duration = ref durations.Get(ref entity);
            ref var loop = ref loops.Get(ref entity);
            progress.time += deltaTime;

            var timeInLoop = progress.time % duration.value;
            var loopIndex = (int)(progress.time / duration.value);

            progress.normalizedTime = timeInLoop / duration.value;

            if (loop.count >= 0 && loopIndex >= loop.count) {
                
                if (loop.type == LoopType.Yoyo) {
                    progress.normalizedTime = loopIndex % 2 == 1 ? 1 : 0;
                }
                else {
                    progress.normalizedTime = 1;
                }
                entity.Destroy();
                continue;
            }

            if (loop.type == LoopType.Yoyo && loopIndex % 2 == 1) {
                progress.normalizedTime = 1 - progress.normalizedTime;
            }
        }
    }
}

sealed class TweenDelaySystem : ISystem {
    public void OnCreate(World world) {
        _query = world.GetQuery(typeof(Delay),typeof(TweenProgress));
    }

    private Query _query;
    private IPool<Delay> delays;
    private IPool<TweenProgress> tweenProgress;
    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
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

sealed class TweenEasingSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Easing> easings;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<TweenProgress>()
            .With<Easing>()
            .Without<Delay>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            ref var progress = ref progresses.Get(ref entity);
            ref var easing = ref easings.Get(ref entity);

            progress.normalizedTime = Easings.Interpolate(progress.normalizedTime, easing.EasingType);
        }
    }
}



struct NextTween : IComponent {
    public Entity entity;
}

struct OnTweenComplete : IComponent { }
sealed class StartNextTweenSystem : ISystem {
    private Query _query;
    private IPool<NextTween> nextTweens;
    private IPool<DestroyEntity> destoyedEntity;
    public void OnCreate(World world) {
        _query = world.GetQuery().With<NextTween>().With<DestroyEntity>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            ref var next = ref nextTweens.Get(ref entity);
            next.entity.Add<TweenProgress>();
        }
    }
}

sealed class ClearTransformsEntitySystem : ISystem {
    private Query _query;
    private IPool<TransformUnityTweenTag> callbacks;
    private IPool<Target> targets;
    private IPool<TweenProgress> progresses;

    public void OnCreate(World world) {
        _query = world.GetQuery().With<DestroyEntity>().With<TransformUnityTweenTag>().With<Target>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            ref var target = ref targets.Get(ref entity);
            ref var progress = ref progresses.Get(ref entity);
            if (!progress.targedDestroyed) {
                ref var tw = ref target.entity.Get<TweeningObject>();
                if (tw.tweensCount == 1) {
                    TransformTweenExtensions.Remove(target.entity.Get<TransformReferenceTween>().hashCode);
                    tw.tweensCount--;
                    progress.targedDestroyed = true;
                    target.entity.Destroy();
                }
                else {
                    tw.tweensCount--;
                }
            }
        }
    }
}
sealed class CallbackTweenSystem : ISystem {
    private Query _query;

    private static readonly Dictionary<int, Action> callbacksMap = new Dictionary<int, Action>();

    public static void AddCallback(int id, Action callback) {
        if (callbacksMap.ContainsKey(id)) {
            callbacksMap[id] = callback;
            return;
        }
        callbacksMap.Add(id, callback);
    }
    
    public void OnCreate(World world) {
        _query = world.GetQuery().With<DestroyEntity>().With<OnTweenComplete>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {
            callbacksMap[entity.Index].Invoke();
            callbacksMap.Remove(entity.Index);
        }
    }
}

struct TranslationTween : IComponent {
    public Vector3 endValue;
    public Vector3 startValue;
}

struct TransformUnityTweenTag : IComponent {

}

struct TransformReferenceTween : IComponent {
    public Transform value;
    public int hashCode;
}

struct TweeningObject : IComponent {
    public int tweensCount;
}

struct BlockedTween : IComponent { }
sealed class ScaleTranslationTweenSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Target> targets;
    private IPool<TranslationTween> scaleTweens;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<TweenProgress>()
            .With<Target>()
            .With<TranslationTween>()
            .Without<Delay>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {

            ref var progress = ref progresses.Get(ref entity);
            if(progress.targedDestroyed) return;
            
            ref var targetEntity = ref targets.Get(ref entity).entity;
            ref var translation = ref targetEntity.Get<Translation>();
            ref var scaleTween = ref scaleTweens.Get(ref entity);
            translation.scale = Vector3.Lerp(scaleTween.startValue, scaleTween.endValue, progress.normalizedTime);
        }
    }
}

struct TranslationRotationTween : IComponent {
    public Vector3 endValue;
    public Vector3 startValue;
}

sealed class RotationTranslationTweenSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Target> targets;
    private IPool<TranslationRotationTween> scaleTweens;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<TweenProgress>()
            .With<Target>()
            .With<TranslationRotationTween>()
            .Without<Delay>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {

            ref var progress = ref progresses.Get(ref entity);
            if(progress.targedDestroyed) return;
            
            ref var targetEntity = ref targets.Get(ref entity).entity;
            ref var translation = ref targetEntity.Get<Translation>();
            ref var scaleTween = ref scaleTweens.Get(ref entity);
            translation.rotation = Quaternion.Euler(Vector3.Lerp(scaleTween.startValue, scaleTween.endValue, progress.normalizedTime));
        }
    }
}

struct TranslationMoveTween : IComponent {
    public Vector3 endValue;
    public Vector3 startValue;
}

sealed class MoveTranslationTweenSystem : ISystem {
    private Query _query;
    private IPool<TweenProgress> progresses;
    private IPool<Target> targets;
    private IPool<TranslationMoveTween> scaleTweens;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<TweenProgress>()
            .With<Target>()
            .With<TranslationMoveTween>()
            .Without<Delay>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (ref var entity in _query) {

            ref var progress = ref progresses.Get(ref entity);
            if(progress.targedDestroyed) return;
            
            ref var targetEntity = ref targets.Get(ref entity).entity;
            ref var translation = ref targetEntity.Get<Translation>();
            ref var scaleTween = ref scaleTweens.Get(ref entity);
            translation.position = Vector3.Lerp(scaleTween.startValue, scaleTween.endValue, progress.normalizedTime);
        }
    }
}

struct PauseTween : IComponent {
    public float time;
    public float incertTime;
}

sealed class PauseTweenSystem : ISystem {
    private Query _query;
    private IPool<PauseTween> pool;
    private IPool<TweenProgress> progresses;
    public void OnCreate(World world) {
        _query = world.GetQuery().With<PauseTween>().With<TweenProgress>();
        pool = world.GetPool<PauseTween>();
        progresses = world.GetPool<TweenProgress>();
    }

    public void OnUpdate(float deltaTime) {
        foreach (ref var entity in _query) {
            ref var progress = ref progresses.Get(ref entity);
            ref var pause = ref pool.Get(ref entity);

            if (progress.time >= pause.incertTime) {
                entity.Add(new Delay{value = pause.time});
                entity.Remove<PauseTween>();
            }
        }
    }
}
sealed class SyncTransformsTweenSystem : ISystem {
    Query _query;
    IPool<TransformReferenceTween> transforms;
    IPool<Translation> translations;
    public void OnCreate(World world) {
        _query = world.GetQuery()
            .With<Translation>()
            .With<TransformReferenceTween>()
            .Without<StaticTag>();

        transforms = world.GetPool<TransformReferenceTween>();
        translations = world.GetPool<Translation>();
    }

    public void OnUpdate(float deltaTime) {
        if(_query.IsEmpty) return;
        foreach (var entity in _query) {
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
        this
            .Add<TweenDelaySystem>()
            .Add<TweenProgressSystem>()
            .Add<TweenLoopProgressSystem>()
            .Add<TweenEasingSystem>()
            .Add<PauseTweenSystem>()
            
            .Add<ScaleTranslationTweenSystem>()
            .Add<RotationTranslationTweenSystem>()
            .Add<MoveTranslationTweenSystem>()
            
            
            .Add<StartNextTweenSystem>()
            .Add<CallbackTweenSystem>()
            .Add<ClearTransformsEntitySystem>()
            ;
    }
}
}
