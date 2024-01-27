using GGJ2024.Systems;

namespace GGJ2024.Messages;

public readonly record struct Action(float Value, Actions ActionType, ActionState ActionState, int index);
