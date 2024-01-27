using System.Collections.Generic;
using MoonWorks.Input;
using MoonTools.ECS;
using GGJ2024.Messages;
using GGJ2024.Utility;
using System.Text;
using GGJ2024.Components;
using GGJ2024.Content;

namespace GGJ2024.Systems;

public enum Actions
{
    MoveX,
    MoveY,
    Interact,
    ChangeCard,
}

public enum ActionState
{
    Off,
    Pressed,
    Held,
    Released
}


public class GenericAxis
{
    public float Value;
    public List<GenericInputs> Positive = new List<GenericInputs>();
    public List<GenericInputs> Negative = new List<GenericInputs>();
}

public class Input : MoonTools.ECS.System
{
    Inputs Inputs;
    public static Dictionary<Actions, GenericAxis> ActionBindings;
    Filter PlayerFilter;

    public static string GetActionNames(Actions action)
    {
        var axis = ActionBindings[action];
        var results = new StringBuilder();

        int i = 0;
        results.Append("[");

        foreach (var input in axis.Positive)
        {
            if (axis.Negative.Count > 0)
            {
                results.Append("+");
            }
            results.Append(input);
            if ((i < axis.Positive.Count - 1 || axis.Negative.Count > 0))
            {
                results.Append(",");
            }
            i++;
        }
        i = 0;


        foreach (var input in axis.Negative)
        {
            results.Append("-");
            results.Append(input);
            if (i < axis.Negative.Count - 1)
            {
                results.Append(",");
            }
            i++;
        }
        results.Append("]");

        return results.ToString();
    }

    Dictionary<Actions, ActionState> ActionStates = new Dictionary<Actions, ActionState>();

    public static void ResetActions()
    {
        ActionBindings = new Dictionary<Actions, GenericAxis>()
            {
                {Actions.MoveY, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.S},
                    Negative = new List<GenericInputs>(){GenericInputs.W, GenericInputs.LeftY}
                }},
                {Actions.MoveX, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.D},
                    Negative = new List<GenericInputs>(){GenericInputs.A, GenericInputs.LeftX}
                }},
                {Actions.Interact, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.Space, GenericInputs.AButton}
                }},
                {Actions.ChangeCard, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.E, GenericInputs.RightShoulder},
                    Negative = new List<GenericInputs>(){GenericInputs.Q, GenericInputs.LeftShoulder}
                }},
            };
    }

    public Input(World world, Inputs inputContext) : base(world)
    {
        Inputs = inputContext;
        ResetActions();

        foreach (var n in (Actions[])System.Enum.GetValues(typeof(Actions)))
        {
            ActionStates[n] = ActionState.Off;
        }

        PlayerFilter = FilterBuilder.Include<Player>().Build();
    }

    public override void Update(System.TimeSpan delta)
    {
        if (Inputs.Keyboard.IsPressed(KeyCode.A))
        {
            Send(new PlayStaticSoundMessage(StaticAudio.AirHorn));
        }

        foreach (var player in PlayerFilter.Entities)
        {
            foreach (var (action, axis) in ActionBindings)
            {
                var value = 0.0f;

                foreach (var input in axis.Positive)
                {
                    var v = InputHelper.Poll(Inputs, input, Get<Player>(player).Index);
                    if (System.MathF.Abs(v) > 0.0f)
                    {
                        value = v;

                        switch (ActionStates[action])
                        {
                            case ActionState.Off:
                                ActionStates[action] = ActionState.Pressed;
                                break;
                            case ActionState.Pressed:
                                ActionStates[action] = ActionState.Held;
                                break;
                            case ActionState.Held:
                                break;
                            case ActionState.Released:
                                ActionStates[action] = ActionState.Pressed;
                                break;
                        }
                        break;
                    }
                    else
                    {
                        switch (ActionStates[action])
                        {
                            case ActionState.Off:
                                break;
                            case ActionState.Pressed:
                                ActionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Held:
                                ActionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Released:
                                ActionStates[action] = ActionState.Off;
                                break;
                        }
                    }
                }

                foreach (var input in axis.Negative)
                {
                    var v = InputHelper.Poll(Inputs, input, Get<Player>(player).Index) * -1.0f;
                    if (System.MathF.Abs(v) > 0.0f)
                    {
                        value += v;
                        switch (ActionStates[action])
                        {
                            case ActionState.Off:
                                ActionStates[action] = ActionState.Pressed;
                                break;
                            case ActionState.Pressed:
                                ActionStates[action] = ActionState.Held;
                                break;
                            case ActionState.Held:
                                break;
                            case ActionState.Released:
                                ActionStates[action] = ActionState.Pressed;
                                break;
                        }
                        break;
                    }
                    else
                    {
                        switch (ActionStates[action])
                        {
                            case ActionState.Off:
                                break;
                            case ActionState.Pressed:
                                ActionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Held:
                                ActionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Released:
                                ActionStates[action] = ActionState.Off;
                                break;
                        }
                    }
                }

                if (System.MathF.Abs(value) > 0.0f)
                {
                    Send(new Action(value, action, ActionStates[action], Get<Player>(player).Index));
                }
            }
        }
    }
}