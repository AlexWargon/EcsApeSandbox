using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Wargon.Ecsape.Components;

namespace Wargon.Ecsape.Tween {
    public delegate void EntityAction(Entity entity);

    public ref struct TweenBuilder {
        internal Entity entity;
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
        public TweenBuilder OnComplete(EntityAction action) {
            entity.Add(new OnTweenCopleteEntityAction{action = action});
            return this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TweenBuilder OnComplete(Action callback) {
            CallbackTweenSystem.AddCallback(entity.Index, callback);
            entity.Add<OnTweenComplete>();
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
        private static World world;

        private static World World {
            get {
                if (world == null) {
                    world = World.Get(World.TweenIndex);
                }

                return world;
            }
        }
        private static TweenBuilder AddTween(this in Entity entity, float duration) {
            var tween = World.CreateEntity(new Duration { value = duration },
                new Target { entity = entity },
                new TweenProgress());
            return new TweenBuilder(tween);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScale(in this  Entity entity, Vector3 start, Vector3 end, float duration) {
            if (!entity.Has<Translation>()) return default;

            var e = World
                .CreateEntity(
                    new Duration {value = duration}, 
                    new Target {entity = entity},
                    new TweenProgress(), 
                    new TranslationTween {endValue = end, startValue = start}
                    );
            
            return new TweenBuilder(e);
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
            //
            // var builder = entity.AddTween(duration);
            // builder.Add(new TranslationRotationTween { endValue = end, startValue = start });
            //
            // var e = World.Get(World.TweenIndex).GetArchetype(stackalloc int[4]{
            //     Component<Duration>.Index, 
            //     Component<Target>.Index,
            //     Component<TweenProgress>.Index, 
            //     Component<TranslationRotationTween>.Index
            // }).CreateEntity();
            // e.Get<Duration>().value = duration;
            // e.Get<Target>().entity = entity;
            //
            // ref var translationTween = ref e.Get<TranslationRotationTween>();
            // translationTween.endValue = end; translationTween.startValue = start;
            //
            var e = World
                .CreateEntity(
                    new Duration {value = duration}, 
                    new Target {entity = entity},
                    new TweenProgress(), 
                    new TranslationRotationTween {endValue = end, startValue = start}
                );
            
            
            return new TweenBuilder(e);
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

            var e = World
                .CreateEntity(
                    new Duration {value = duration}, 
                    new Target {entity = entity},
                    new TweenProgress(), 
                    new TranslationMoveTween {endValue = end, startValue = start}
                );
            
            return new TweenBuilder(e);
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

        public static TweenBuilder doMoveY(this ref Entity entity, float start, float end, float duration) {
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

        public static TweenBuilder doMoveZ(this ref Entity entity, float start, float end, float duration) {
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
        private static readonly Dictionary<int, int> transforms_entities_map = new ();
        public static void Remove(int id) => transforms_entities_map.Remove(id);
        
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Entity TransformToEntity(Transform transform) {
            var transformId = transform.GetInstanceID();
            
            var world = World.Get(World.TweenIndex);
            
            Entity entity;
            
            if (transforms_entities_map.ContainsKey(transformId)) 
            {
                entity = world.GetEntity(transforms_entities_map[transformId]);
                if(transform.parent is not null) entity.Add(new ParentTransform{Transform = transform.parent});
                ref var to = ref entity.Get<TweeningObject>();
                to.tweensCount++;
            }
            else 
            {
                entity = world.CreateEntity();
                transforms_entities_map.Add(transformId,entity.Index);
                entity.Add(new TweeningObject{tweensCount = 1});
                
                entity.Add(new TransformReferenceTween {
                     value = transform,
                     instanceID = transformId
                });
                
                entity.Add(new Translation {
                     scale = transform.localScale,
                     rotation = transform.rotation,
                     position = transform.position
                });
                if(transform.parent is not null) entity.Add(new ParentTransform{Transform = transform.parent});
            }

            return entity;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScale(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doScale(start, end, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScaleX(this Transform transform, float start, float end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doScaleX(start, end, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScaleY(this Transform transform, float start, float end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doScaleY(start, end, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doScaleZ(this Transform transform, float start, float end, float duration) {
            var entity = TransformToEntity(transform);
            
            return entity.doScaleZ(start, end, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doRotation(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            
            return entity.doRotation(start, end, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doRotationZ(this Transform transform, float start, float end, float duration) {
            var entity = TransformToEntity(transform);
            var startRot = transform.rotation.eulerAngles;
            startRot.z = start;
            var endRot = startRot;
            endRot.z = end;
            return entity.doRotation(startRot, endRot, duration).Add(new TransformUnityTweenTag());
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public static TweenBuilder doMove(this Transform transform, Vector3 start, Vector3 end, float duration) {
            var entity = TransformToEntity(transform);
            return entity.doMove(start, end, duration).Add(new TransformUnityTweenTag());
        }
    }
}
