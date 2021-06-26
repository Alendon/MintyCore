using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.SDL2;

namespace MintyCore.Systems.Client
{
	[ExecuteInSystemGroup(typeof(InitializationSystemGroup))]
	[ExecutionSide(GameType.Client)]
	class InputSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Input;

		private ComponentQuery componentQuery = new();

		public override void Dispose()
		{

		}
		public override void Setup()
		{
			componentQuery.WithComponents(ComponentIDs.Input);
			componentQuery.Setup(this);
		}

		public override void Execute()
		{
			foreach (var item in componentQuery)
			{
				ref var inputComp = ref item.GetComponent<Input>(ComponentIDs.Input);

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
