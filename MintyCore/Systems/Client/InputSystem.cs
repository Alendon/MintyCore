using MintyCore.Components.Client;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(InitializationSystemGroup))]
    [ExecutionSide(GameType.CLIENT)]
    internal partial class InputSystem : ASystem
    {
        [ComponentQuery] private readonly ComponentQuery<Input> _componentQuery = new();

        public override Identification Identification => SystemIDs.Input;

        public override void Dispose()
        {
        }

        public override void Setup()
        {
            _componentQuery.Setup(this);
        }

        protected override void Execute()
        {
            foreach (var item in _componentQuery)
            {
                if(World is not null && World.EntityManager.GetEntityOwner(item.Entity) != MintyCore.LocalPlayerGameId) continue;
                
                ref var inputComp = ref item.GetInput();

                bool notChanged = true;

                notChanged &= !inputComp.Backward.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Backward.Key));
                notChanged &= !inputComp.Forward.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Forward.Key));
                notChanged &= !inputComp.Left.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Left.Key));
                notChanged &= !inputComp.Right.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Right.Key));
                notChanged &= !inputComp.Up.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Up.Key));
                notChanged &= !inputComp.Down.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Down.Key));

                inputComp.Dirty = notChanged ? (byte)0 : (byte)1;
            }
        }
    }
}