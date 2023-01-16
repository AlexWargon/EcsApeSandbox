using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Wargon.Ecsape.Tweens {
    public ref struct TweenBuilder {
        private readonly Entity entity;
        private bool offest;
        
        internal TweenBuilder(Entity tween) {
            entity = tween;
            offest = false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder WithDelay(float delay) {
            entity.Add(new Delay{value = delay});
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder WithLoop(int count, LoopType loopType) {
            entity.Add(new TweenLoop{count = count, type = loopType});
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder WithEasing(Easings.EasingType easingType) {
            entity.Add(new Easing{EasingType = easingType});
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder Then(TweenBuilder next) {
            next.entity.Remove<TweenProgress>();
            entity.Add(new NextTween{entity = next.entity});
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder OnComplete(Action callback) {
            CallbackTweenSystem.AddCallback(entity.Index, callback);
            entity.Add(new OnTweenComplete());
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TweenBuilder Add<T>(T tween) where T : struct, IComponent {
            entity.Add(tween);
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder SetOffest(bool isOffset) {
            if (entity.IsNull() || offest == isOffset) return this;
            ref var tween = ref entity.Get<TranslationTween>();
            offest = isOffset;
            if (offest) {
                tween.endValue += tween.startValue;
            }
            else {
                tween.endValue -= tween.startValue;
            }
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder From(Vector3 value) {
            if (entity.IsNull()) return this;
            ref var tween = ref entity.Get<TranslationTween>();
            tween.startValue = value;
            tween.endValue = offest ? value + tween.endValue : tween.endValue;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder Pause(float incertTime, float time) {
            if (entity.IsNull()) return this;
            entity.Add(new PauseTween{incertTime = incertTime, time = time});
            return this;
        }
    }
    public static class TranslationTweenBuilding {
        private static TweenBuilder AddTween(this in Entity entity, float duration) {
            var tween = Worlds.Get(Worlds.Tween).CreateEntity();
            tween.Add(new Duration{value = duration});
            tween.Add<TweenProgress>();
            tween.Add(new Target{entity = entity});
            return new TweenBuilder(tween);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScale(this in Entity entity, Vector3 start, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationTween{endValue = end, startValue = start});
            return builder;
        }
        
        public static TweenBuilder doScale(this in Entity entity, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationTween{endValue = end, startValue = entity.Get<Translation>().scale});
            return builder;
        }
        
        public static TweenBuilder doScale(this in Entity entity, float end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationTween{endValue = new Vector3(end,end,end), startValue = entity.Get<Translation>().scale});
            return builder;
        }
        
        public static TweenBuilder doScale(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationTween
            {
                endValue = new Vector3(end,end,end), 
                startValue = new Vector3(start,start,start)
            });
            return builder;
        }
        
        public static TweenBuilder doScaleX(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            builder.Add(new TranslationTween
            {
                endValue = new Vector3(end,translation.scale.y,translation.scale.z), 
                startValue = new Vector3(start,translation.scale.y,translation.scale.z)
            });
            return builder;
        }

        public static TweenBuilder doScaleY(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            builder.Add(new TranslationTween
            {
                endValue = new Vector3(translation.scale.x,end,translation.scale.z), 
                startValue = new Vector3(translation.scale.x,start,translation.scale.z)
            });
            return builder;
        }
        
        public static TweenBuilder doScaleZ(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            builder.Add(new TranslationTween
            {
                endValue = new Vector3(translation.scale.x,translation.scale.y,end), 
                startValue = new Vector3(translation.scale.x,translation.scale.y,start)
            });
            return builder;
        }

        public static TweenBuilder doRotation(this in Entity entity, Quaternion start, Quaternion end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationRotationTween { endValue = end.eulerAngles, startValue = start.eulerAngles });
            return builder;
        }
        
        public static TweenBuilder doRotation(this in Entity entity, Vector3 start, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationRotationTween { endValue = end, startValue = start });
            return builder;
        }

        public static TweenBuilder doRotation(this in Entity entity, Quaternion end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var builder = entity.AddTween(duration);
            builder.Add(new TranslationRotationTween { endValue = end.eulerAngles, startValue = entity.Get<Translation>().rotation.eulerAngles });
            return builder;
        }

        public static TweenBuilder doRotation(this in Entity entity, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            builder.Add(new TranslationRotationTween { endValue = end, startValue = entity.Get<Translation>().rotation.eulerAngles });
            return builder;
        }

        public static TweenBuilder doMove(this in Entity entity, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            builder.Add(new TranslationMoveTween { endValue = end, startValue = entity.Get<Translation>().position });
            return builder;
        }

        public static TweenBuilder doMove(this in Entity entity, Vector3 start, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            builder.Add(new TranslationMoveTween { endValue = end, startValue = start });
            return builder;
        }

        public static TweenBuilder doMoveX(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            builder.Add(new TranslationMoveTween
            {
                endValue = new Vector3(end, translation.position.y, translation.position.z), 
                startValue = new Vector3(start, translation.position.y, translation.position.z)
            });
            return builder;
        }

        public static TweenBuilder doMoveY(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            builder.Add(new TranslationMoveTween
            {
                endValue = new Vector3(translation.position.x, end, translation.position.z), 
                startValue = new Vector3(translation.position.x, start, translation.position.z)
            });
            return builder;
        }

        public static TweenBuilder doMoveZ(this in Entity entity, float start, float end, float duration) {
            if (!entity.Has<Translation>()) return default;
            
            var builder = entity.AddTween(duration);
            ref var translation = ref entity.Get<Translation>();
            
            builder.Add(new TranslationMoveTween
            {
                endValue = new Vector3(translation.position.x, translation.position.y, end), 
                startValue = new Vector3(translation.position.x, translation.position.y, start)
            });
            return builder;
        }
    }
    public static class TransformTweenExtensions {
        private static readonly Dictionary<int, int> transforms_entities_map = new Dictionary<int, int>();
        public static void Remove(int id) => transforms_entities_map.Remove(id);
        
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Entity TransformToEntity(Transform transform) {
            var transformHashCode = transform.GetHashCode();
            
            var world = Worlds.Get(Worlds.Tween);
            
            Entity entity;
            
            if (transforms_entities_map.ContainsKey(transformHashCode)) 
            {
                entity = world.GetEntity(transforms_entities_map[transformHashCode]);
                ref var to = ref entity.Get<TweeningObject>();
                to.tweensCount++;
            }
            else 
            {
                entity = world.CreateEntity();
                transforms_entities_map.Add(transformHashCode,entity.Index);
                entity.Add(new TweeningObject{tweensCount = 1});

                
                entity.Add(new TransformReferenceTween {
                    value = transform,
                    hashCode = transformHashCode
                });

                entity.Add(new Translation {
                    scale = transform.localScale,
                    rotation = transform.rotation,
                    position = transform.position
                });
            }

            return entity;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScale(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doScale(start, end, duration).Add(new TransformUnityTweenTag());
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doRotation(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doRotation(start, end, duration).Add(new TransformUnityTweenTag());
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doMove(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doMove(start, end, duration).Add(new TransformUnityTweenTag());
        }
    }
}
