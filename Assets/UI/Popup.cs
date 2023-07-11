using System;
using UnityEngine;
using Wargon.Ecsape.Tween;

namespace Wargon.UI {
    public class Popup : UIElement {
        public override void PlayShowAnimation(Action callback = null) {
            Transform
                .doScale(Vector3.zero, Vector3.one, 0.3f).WithEasing(Easings.EasingType.CircularEaseOut)
                .OnComplete(() => {
                    callback?.Invoke();
                });
        }

        public override void PlayHideAnimation(Action callback = null) {
            Transform
                .doScale(Vector3.one, Vector3.zero, 0.3f)
                .OnComplete(() => {
                    callback?.Invoke();
                });
        }
    }
}