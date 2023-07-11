using Animation2D;
using UnityEngine;
using Wargon.Ecsape;

namespace Rogue {
    sealed class WeaponRotationSystem : ISystem {
        private Camera Camera;
        private readonly Vector3 right = new Vector3(1, 1, 1);
        private readonly Vector3 left = new Vector3(1, -1, 1);
        private IPool<SpriteRender> spriteRenders;
        private IPool<WeaponParent> weaponParents;
        private Query Query;
        
        public void OnCreate(World world) {
            Query = world.GetQuery().WithAll(typeof(SpriteRender),typeof(WeaponParent));
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in Query) {
                ref var weaponParent = ref weaponParents.Get(ref entity);
                ref var render = ref spriteRenders.Get(ref entity);
                weaponParent.difference = MousePosition(Camera) - weaponParent.Transform.position;
                weaponParent.difference.Normalize();

                float rotZ = Mathf.Atan2(weaponParent.difference.y, weaponParent.difference.x) * Mathf.Rad2Deg;
                weaponParent.Transform.rotation = Quaternion.Slerp(weaponParent.Transform.rotation,
                    Quaternion.Euler(0, 0, rotZ), 1.4f);
                SetSide(rotZ, weaponParent.weaponRender, render.value, weaponParent.Transform);
            }
        }
        
        private void SetSide(float rotZ, SpriteRenderer weapon, SpriteRenderer ownder, Transform transform)
        {

            if (rotZ > 0)
            {
                var pos = weapon.transform.localPosition;
                pos.z = 1f;
                weapon.transform.localPosition = pos;
            }
            if (rotZ < 0)
            {
                var pos = weapon.transform.localPosition;
                pos.z = -1f;
                weapon.transform.localPosition = pos;
            }

            if (rotZ is < 90 and > -90)
            {
                transform.localScale = right;
                ownder.flipX = false;
            }

            else
            {
                transform.localScale = left;
                ownder.flipX = true;
            }
        }
        private static readonly Vector3 Offset = new Vector3(0, 0, 10);

        private static Vector3 MousePosition(Camera camera)
        {
            return camera.ScreenToWorldPoint(Input.mousePosition) + Offset;
        }
    }
}