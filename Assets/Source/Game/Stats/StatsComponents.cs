using System.Collections.Generic;
using Unity.Collections;
using Wargon.Ecsape;

namespace Rogue {
    /// <summary>
    /// The Stat-Entity marked with this tag applies the effect 
    /// to the <see cref="StatReceiverTag">Stat-Receiver</see> using the addition/subtraction operation
    /// </summary>
    public struct AdditiveStatTag : IComponent { }
    /// <summary>
    /// The Stat-Entity marked with this tag applies the effect 
    /// to the <see cref="StatReceiverTag">Stat-Receiver</see> using the multiply/divide operation
    /// </summary>
    public struct MultiplyStatTag : IComponent { }

    /// <summary>
    /// Request of applying Stat-Entity (to the <see cref="StatReceiverTag">Stat-Receiver</see>) or changing it's value
    /// </summary>
    public struct StatsChangedRequest : IComponent
    {
        public Entity StatContext;
        public Entity StatReceiver;
    }

    /// <summary>
    /// Request of removing Stat-Entity (from the <see cref="StatReceiverTag">Stat-Receiver</see>)
    /// </summary>
    public struct StatsRemovedRequest : IComponent
    {
        public Entity StatContext;
    }

    /// <summary>
    /// Event of applying Stat-Entity (to the <see cref="StatReceiverTag">Stat-Receiver</see>) or changing it's value
    /// </summary>
    public struct StatsChangedEvent : IComponent { }
    /// <summary>
    /// Event of removing Stat-Entity (from the <see cref="StatReceiverTag">Stat-Receiver</see>)
    /// </summary>
    public struct StatsRemovedEvent : IComponent { }

    /// <summary>
    /// An event that any of the Stat-Entity of <see cref="StatReceiverTag">Stat-Receiver</see> has changed. Always placed on the <see cref="StatReceiverTag">Stat-Receiver</see> entity
    /// </summary>
    public struct StatRecievedElementEvent : IComponent 
    {
        public int StatType;

        /// <summary>
        /// Defines the type of Stat that has changed
        /// </summary>
        /// <typeparam name="TStatComponent">Type of Stat that has changed</typeparam>
        /// <returns></returns>
        public bool Is<TStatComponent>() where TStatComponent : struct, IComponent
            => StatType == Component<TStatComponent>.Index;
    }

    /// <summary>
    /// Reference to the <see cref="StatReceiverTag">Stat-Receiver</see> entity to which this effect will be applied
    /// </summary>
    public struct StatReceiverLink : IComponent
    {
        public Entity Value;
    }

    /// <summary>
    /// This tag determines the entity as a "Stat-Receiver". This means that the entity 
    /// may accumulate stats values from it's children Stat-Entities. 
    /// As soon as the value of the Stat-Entity changes, the value of the Stat-Receiver also changes.
    /// </summary>
    public struct StatReceiverTag : IComponent { }

    /// <summary>
    /// Stat-Entity - is the entity which holds one or several Stat-Components (e.g. Strength, Max-Health, etc.) 
    /// It persists as a StatElement (dynamic buffer element) of a "Stat-Context" Entity. Stat-Context Entity can be anything, say an item (sword, gun, potion etc.) or a buff, an ability. 
    /// Stat-Entity also stores a reference
    /// to a Stat-Receiver entity to which this effect will be applied.
    /// </summary>
    public struct StatElement : IComponent
    {
        public Entity Value;
    }

    public struct StatEntities : IComponent, IOnAddToEntity {
        public List<Entity> Value;

        public void OnAdd() {
            Value = new List<Entity>();
        }
    }
    
    /// <summary>
    /// Used only configuration. For runtime see <see cref="AdditiveStatTag">AdditiveStatTag</see> and <see cref="MultiplyStatTag">MultiplyStatTag</see>
    /// </summary>
    public enum StatOpType
    {
        Additive,
        Multiply
    }
}