using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024.Components;

public readonly record struct AABB(int W, int H);

public readonly record struct Player(int Index);



public readonly record struct Velocity(Vector2 Value);