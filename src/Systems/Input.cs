using MoonTools.ECS;
using MoonWorks.Input;
using GGJ2024.Components;
using System;

namespace GGJ2024.Systems;

public struct InputState
{
	public ButtonState Left { get; set; }
	public ButtonState Right { get; set; }
	public ButtonState Up { get; set; }
	public ButtonState Down { get; set; }
	public ButtonState Interact { get; set; }
}

public class ControlSet
{
	public VirtualButton Left { get; set; } = new EmptyButton();
	public VirtualButton Right { get; set; } = new EmptyButton();
	public VirtualButton Up { get; set; } = new EmptyButton();
	public VirtualButton Down { get; set; } = new EmptyButton();
	public VirtualButton Interact { get; set; } = new EmptyButton();
}

public class Input : MoonTools.ECS.System
{
	Inputs Inputs { get; }

	Filter PlayerFilter { get; }

	ControlSet PlayerOneKeyboard = new ControlSet();
	ControlSet PlayerOneGamepad = new ControlSet();
	ControlSet PlayerTwoKeyboard = new ControlSet();
	ControlSet PlayerTwoGamepad = new ControlSet();

	public Input(World world, Inputs inputs) : base(world)
	{
		Inputs = inputs;
		PlayerFilter = FilterBuilder.Include<Player>().Build();

		PlayerOneKeyboard.Up = Inputs.Keyboard.Button(KeyCode.W);
		PlayerOneKeyboard.Down = Inputs.Keyboard.Button(KeyCode.S);
		PlayerOneKeyboard.Left = Inputs.Keyboard.Button(KeyCode.A);
		PlayerOneKeyboard.Right = Inputs.Keyboard.Button(KeyCode.D);
		PlayerOneKeyboard.Interact = Inputs.Keyboard.Button(KeyCode.Space);

		PlayerOneGamepad.Up = Inputs.GetGamepad(0).LeftYDown;
		PlayerOneGamepad.Down = Inputs.GetGamepad(0).LeftYUp;
		PlayerOneGamepad.Left = Inputs.GetGamepad(0).LeftXLeft;
		PlayerOneGamepad.Right = Inputs.GetGamepad(0).LeftXRight;
		PlayerOneGamepad.Interact = Inputs.GetGamepad(0).A;

		PlayerTwoKeyboard.Up = Inputs.Keyboard.Button(KeyCode.Up);
		PlayerTwoKeyboard.Down = Inputs.Keyboard.Button(KeyCode.Down);
		PlayerTwoKeyboard.Left = Inputs.Keyboard.Button(KeyCode.Left);
		PlayerTwoKeyboard.Right = Inputs.Keyboard.Button(KeyCode.Right);
		PlayerTwoKeyboard.Interact = Inputs.Keyboard.Button(KeyCode.Return);

		PlayerTwoGamepad.Up = Inputs.GetGamepad(1).LeftYDown;
		PlayerTwoGamepad.Down = Inputs.GetGamepad(1).LeftYUp;
		PlayerTwoGamepad.Left = Inputs.GetGamepad(1).LeftXLeft;
		PlayerTwoGamepad.Right = Inputs.GetGamepad(1).LeftXRight;
		PlayerTwoGamepad.Interact = Inputs.GetGamepad(1).A;
	}

	public override void Update(TimeSpan timeSpan)
	{
		foreach (var playerEntity in PlayerFilter.Entities)
		{
			var index = Get<Player>(playerEntity).Index;
			var controlSet = index == 0 ? PlayerOneKeyboard : PlayerTwoKeyboard;
			var altControlSet = index == 0 ? PlayerOneGamepad : PlayerTwoGamepad;

			InputState inputState = InputState(controlSet, altControlSet);

			Set(playerEntity, inputState);
		}
	}

	private static InputState InputState(ControlSet controlSet, ControlSet altControlSet)
	{
		return new InputState
		{
			Left = controlSet.Left.State | altControlSet.Left.State,
			Right = controlSet.Right.State | altControlSet.Right.State,
			Up = controlSet.Up.State | altControlSet.Up.State,
			Down = controlSet.Down.State | altControlSet.Down.State,
			Interact = controlSet.Interact.State | altControlSet.Interact.State
		};
	}
}
