using System;
using UnityEngine;
using Wargon.Ecsape.Tweens;

namespace Wargon.UI {
    public class Popup : UIElement {
        public override void PlayShowAnimation(Action callback = null) {
            Debug.Log($"UIService Created {UIService != null}");
            transform
                .doScale(Vector3.zero, Vector3.one, 0.5f)
                .OnComplete(() => callback?.Invoke());
        }

        public override void PlayHideAnimation(Action callback = null) {
            transform
                .doScale(Vector3.one, Vector3.zero, 0.5f)
                .OnComplete(() => callback?.Invoke());
        }
    }
}