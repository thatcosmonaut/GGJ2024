using MoonWorks.Graphics;

namespace GGJ2024.Components;

public readonly record struct GameTimer(float Time);
public readonly record struct Player(int Index, int Score);
public readonly record struct Orientation(float Angle);
public readonly record struct CanBeHeld();
public readonly record struct TryHold();
public readonly record struct CanHold();
public readonly record struct Solid();
public readonly record struct TouchingSolid();
public readonly record struct Name(int TextID);

public readonly record struct Price(float Value);
public readonly record struct TickerText(float Width);
public readonly record struct ColorBlend(Color Color);
public readonly record struct CanFillOrders();
public readonly record struct CanGiveOrders();

public readonly record struct ColorSpeed(float RedSpeed, float GreenSpeed, float BlueSpeed);

public readonly record struct Depth(int Value);
public readonly record struct DrawAsRectangle();

public readonly record struct TextDropShadow(int OffsetX, int OffsetY);
public readonly record struct ForceIntegerMovement();