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

    Dictionary<Actions, ActionState>[] ActionStateDictionaries = new[]
    {
        new Dictionary<Actions, ActionState>(),
        new Dictionary<Actions, ActionState>()
    };

    public static void ResetActions()
    {
        ActionBindings = new Dictionary<Actions, GenericAxis>()
            {
                {Actions.MoveY, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.S,  GenericInputs.LeftY},
                    Negative = new List<GenericInputs>(){GenericInputs.W}
                }},
                {Actions.MoveX, new GenericAxis{
                    Positive = new List<GenericInputs>(){GenericInputs.D, GenericInputs.LeftX},
                    Negative = new List<GenericInputs>(){GenericInputs.A}
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

        foreach (var dict in ActionStateDictionaries)
        {
            foreach (var n in (Actions[])System.Enum.GetValues(typeof(Actions)))
            {
                dict[n] = ActionState.Off;
            }
        }

        PlayerFilter = FilterBuilder.Include<Player>().Build();
    }

    public override void Update(System.TimeSpan delta)
    {
        foreach (var player in PlayerFilter.Entities)
        {
            var index = Get<Player>(player).Index;
            var actionStates = ActionStateDictionaries[index];

            foreach (var (action, axis) in ActionBindings)
            {
                var value = 0.0f;

                foreach (var input in axis.Positive)
                {
                    var v = InputHelper.Poll(Inputs, input, index);
                    if (System.MathF.Abs(v) > 0.0f)
                    {
                        value = v;

                        switch (actionStates[action])
                        {
                            case ActionState.Off:
                                actionStates[action] = ActionState.Pressed;
                                break;
                            case ActionState.Pressed:
                                actionStates[action] = ActionState.Held;
                                break;
                            case ActionState.Held:
                                break;
                            case ActionState.Released:
                                actionStates[action] = ActionState.Pressed;
                                break;
                        }
                        break;
                    }
                    else
                    {
                        switch (actionStates[action])
                        {
                            case ActionState.Off:
                                break;
                            case ActionState.Pressed:
                                actionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Held:
                                actionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Released:
                                actionStates[action] = ActionState.Off;
                                break;
                        }
                    }
                }

                foreach (var input in axis.Negative)
                {
                    var v = InputHelper.Poll(Inputs, input, index) * -1.0f;
                    if (System.MathF.Abs(v) > 0.0f)
                    {
                        value += v;
                        switch (actionStates[action])
                        {
                            case ActionState.Off:
                                actionStates[action] = ActionState.Pressed;
                                break;
                            case ActionState.Pressed:
                                actionStates[action] = ActionState.Held;
                                break;
                            case ActionState.Held:
                                break;
                            case ActionState.Released:
                                actionStates[action] = ActionState.Pressed;
                                break;
                        }
                        break;
                    }
                    else
                    {
                        switch (actionStates[action])
                        {
                            case ActionState.Off:
                                break;
                            case ActionState.Pressed:
                                actionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Held:
                                actionStates[action] = ActionState.Released;
                                break;
                            case ActionState.Released:
                                actionStates[action] = ActionState.Off;
                                break;
                        }
                    }
                }

                if (System.MathF.Abs(value) > 0.0f)
                {
                    Send(new Action(value, action, actionStates[action], index));
                }
            }
        }
    }
}