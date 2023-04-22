using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    [CreateAssetMenu(fileName = nameof(AbilitiesSO), menuName = "ScriptableObjects/AbilitiesSO", order = 1)]
    public sealed class AbilitiesSO : ScriptableObject {
        public List<EntityLink> List;
    }
}