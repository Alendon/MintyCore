using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Systems.Common
{
    internal partial class MovementSystem : ASystem
    {
        [ComponentQuery] private readonly ComponentQuery<Position, Input> _componentQuery = new();

        public override Identification Identification => SystemIDs.Movement;

        public override void Dispose()
        {
        }

        protected override void Execute()
        {
            foreach (var item in _componentQuery)
            {
                var input = item.GetInput();
                ref var position = ref item.GetPosition();

                float movementSpeed = 2;
                
                if (World.IsServerWorld && input.Right.LastKeyValid)
                {
                    
                }

                float changedX = 0, changedY = 0, changedZ = 0;
                changedX += input.Right.LastKeyValid ? 1 : 0;
                changedX += input.Left.LastKeyValid ? -1 : 0;
                changedY += input.Up.LastKeyValid ? 1 : 0;
                changedY += input.Down.LastKeyValid ? -1 : 0;
                changedZ += input.Forward.LastKeyValid ? -1 : 0;
                changedZ += input.Backward.LastKeyValid ? 1 : 0;
                changedX *= MintyCore.DeltaTime * movementSpeed;
                changedY *= MintyCore.DeltaTime * movementSpeed;
                changedZ *= MintyCore.DeltaTime * movementSpeed;

                Vector3 change = new(changedX, changedY, changedZ);

                position.Value += change;
                position.Dirty = 1;
            }
        }

        public override void Setup()
        {
            _componentQuery.Setup(this);
        }
    }
}