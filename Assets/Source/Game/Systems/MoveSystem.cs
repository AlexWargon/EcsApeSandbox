using Animation2D;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.Ecsape.Components;

namespace Rogue {
    sealed class MoveSystem : ISystem {
        Query query;
        IPool<InputData> inputs;
        IPool<Translation> translations;
        IPool<MoveSpeed> moveSpeeds;
        IPool<SpriteRender> spriteRender;
        public void OnCreate(World world) {
            query = world.GetQuery().WithAll<InputData, Translation, MoveSpeed>()
            .With<SpriteRender>();
            inputs = world.GetPool<InputData>();
            translations = world.GetPool<Translation>();
            moveSpeeds = world.GetPool<MoveSpeed>();
        }

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var input = ref inputs.Get(ref entity);
                ref var translation = ref translations.Get(ref entity);
                ref var moveSpeed = ref moveSpeeds.Get(ref entity);
        
                translation.position += new Vector3(input.horizontal, input.vertical) * deltaTime * moveSpeed.value;
                ref var render = ref spriteRender.Get(ref entity);
                render.flipX = input.horizontal < 0F;
            }
        }
    }

    sealed class PlayerMoveSystem : ISystem {
        
        [With(typeof(InputData), typeof(Translation), typeof(MoveSpeed), typeof(Player))] 
        Query query;
        private SaveService _saveService;

        public void OnUpdate(float deltaTime) {
            foreach (ref var entity in query) {
                ref var input = ref entity.Get<InputData>();
                ref var translation = ref entity.Get<Translation>();
                ref var moveSpeed = ref entity.Get<MoveSpeed>();
                var movementDirection = new Vector3(input.horizontal, 0f, input.vertical);
                translation.position += movementDirection * deltaTime * moveSpeed.value;
                if(movementDirection != Vector3.zero)
                    translation.rotation = Quaternion.LookRotation (movementDirection);
                if (Input.GetKey(KeyCode.P)) {
                    _saveService.SaveEntityBinary(entity);
                }
                if (Input.GetKey(KeyCode.O)) {
                    _saveService.LoadEntityBinary(ref entity);
                }
            }
        }
    }
    
    
    
}