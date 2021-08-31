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
                ref var inputComp = ref item.GetInput();

                inputComp.Backward.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Backward.Key));
                inputComp.Forward.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Forward.Key));
                inputComp.Left.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Left.Key));
                inputComp.Right.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Right.Key));
                inputComp.Up.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Up.Key));
                inputComp.Down.ApplyKeyPress(InputHandler.GetKeyEvent(inputComp.Down.Key));
            }
        }
    }
}