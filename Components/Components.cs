using MoonWorks.Graphics;

namespace GGJ2024.Components;

public readonly record struct AABB(int X, int Y, int W, int H);
public readonly record struct Player(int Index);