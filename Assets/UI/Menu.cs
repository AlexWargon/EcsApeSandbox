using System;

namespace Wargon.UI {
    public class Menu : UIElement {
        public override void PlayShowAnimation(Action callback = null) {
            callback?.Invoke();
        }

        public override void PlayHideAnimation(Action callback = null) {
            callback?.Invoke();
        }
    }
}