using System.Collections.Generic;
using UnityEngine;

namespace Wargon.UI {
    [CreateAssetMenu(fileName = "ListOfElements", menuName = "ScriptableObjects/UI/ListOfElements", order = 1)]
    public class UIElementsList : ScriptableObject {
        public List<UIElement> elements;
    }
}