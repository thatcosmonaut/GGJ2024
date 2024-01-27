using GGJ2024.Data;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024.Components;

public readonly record struct Player(int Index);
public readonly record struct Orientation(float Angle);
public readonly record struct CanBeHeld();
public readonly record struct TryHold();
public readonly record struct CanHold();
public readonly record struct Solid();