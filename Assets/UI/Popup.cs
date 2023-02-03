using System;
using UnityEngine;
using Wargon.Ecsape.Tween;

namespace Wargon.UI {
    public class Popup : UIElement {
        public override void PlayShowAnimation(Action callback = null) {
            transform
                .doScale(Vector3.zero, Vector3.one, 0.5f).WithEasing(Easings.EasingType.BounceEaseOut)
                .OnComplete(() => {
                    callback?.Invoke();
                });
        }

        public override void PlayHideAnimation(Action callback = null) {
            transform
                .doScale(Vector3.one, Vector3.zero, 0.5f)
                .OnComplete(() => {
                    callback?.Invoke();
                });
        }
    }
}