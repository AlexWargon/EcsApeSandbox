using System;
using System.Collections.Generic;
using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    public struct WeaponParent : IComponent {
        public Transform Transform;
        public Vector3 difference;
        public SpriteRenderer weaponRender;
    }

    public struct WeaponAnimation : IComponent {
        public bool reverse;
        public Animator Animator;
    }

    public struct CurrentWeapons : IComponent , IOnAddToEntity{
        public List<EntityLink> Entities;
        public void OnAdd() {
            Entities ??= new List<EntityLink>();
        }
    }
    public struct AbilityList : IComponent, IOnAddToEntity {
        public List<Entity> AbilityEntities;
        public void OnAdd() {
            AbilityEntities = new List<Entity>();
        }
    }

    public struct OnKillEvent : IComponent {
        public Entity killedTarget;
    }

    public struct OnHitPhysicsEvent : IComponent {
        public Vector2 Position;
        public int Damage;
    }

    public struct OnTakeDamageEvent : IComponent {}
    public struct OnHitWithDamageEvent : IComponent { }
    public struct OnCritEvent : IComponent { }
    public struct OnAttackAbility : IComponent { }
    public struct OnHitAbility : IComponent { }
    public struct OnDamageAbility : IComponent { }
    public struct OnKillAbility : IComponent { }
    public struct OnCritAbility : IComponent { }
    public struct OnTriggerAbilityEvent : IComponent {}
    public struct OnTakeDamageAbility : IComponent{}

    public struct BonusShotAbility : IComponent {
        public EntityLink Shot;
        public int Amount;
    }
    
    public struct Attack : IComponent {
        public EntityLink viewPrefab;
        public float delay;
        public float radius;
    }

    public struct DamageColliderCreateRequest : IComponent {
        public Entity owner;
        public Vector3 pos;
        public float radius;
        public int amount;
    }
    public struct DeathEvent : IComponent { }
    
    public struct AttackTarget : IComponent {
        public Entity value;
    }

    public struct TargetSearchType : IComponent {
        public TargetSearch value;
    }

    public enum TargetSearch {
        SameAsPlayer,
        RandomAround,
        Nearest
    }
    
    struct Cooldown : IComponent {
        public float Value;
    }
    
    [Serializable]
    public struct MoveSpeed : IComponent {
        public float value;
    }

    [Serializable] 
    struct MoveDirection : IComponent {
        public Vector3 value;
    }

    [Serializable]
    public struct Health : IComponent {
        public int current;
        public int max;

        public void Damage(int dmg) => current -= dmg;
    }

    [Serializable]
    public struct TakeHitEvent : IComponent {
        public int amount;
        public Entity from;
        public Entity to;
    }
    [Serializable]
    public struct Crit : IComponent{
        public int size;
        public float chance;
    }
    
    [Serializable]
    public struct Direction : IComponent {
        public Vector3 value;
    }

    [Serializable]
    public struct IsProjectile : IComponent {
        public LayerMask Mask;
    }
    [Serializable]
    public struct Damage : IComponent {
        public int value;
    }
    
    
    public struct OnAttackEvent : IComponent { }
    public struct Player : IComponent { }
    public struct EquipedTag : IComponent { }
    public struct EnemyTag : IComponent { }
}