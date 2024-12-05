using MoonWorks.Graphics;
using RollAndCash.Systems;
using RollAndCash.Data;
using RollAndCash.Messages;

namespace RollAndCash.Components;

public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Right => X + Width;
    public int Top => Y;
    public int Bottom => Y + Height;

    public bool Intersects(Rectangle other)
    {
        return
            other.Left < Right &&
            Left < other.Right &&
            other.Top < Bottom &&
            Top < other.Bottom;
    }

    public static Rectangle Union(Rectangle a, Rectangle b)
    {
        var x = int.Min(a.X, a.X);
        var y = int.Min(a.Y, b.Y);
        return new Rectangle(
            x,
            y,
            int.Max(a.Right, b.Right) - x,
            int.Max(a.Bottom, b.Bottom) - y
        );
    }

    public Rectangle Inflate(int horizontal, int vertical)
    {
        return new Rectangle(
            X - horizontal,
            Y - vertical,
            Width + horizontal * 2,
            Height + vertical * 2
        );
    }
}

public readonly record struct GameTimer(float Time);
public readonly record struct Player(int Index);
public readonly record struct Orientation(float Angle);
public readonly record struct CanInteract();
public readonly record struct CanInspect();
public readonly record struct CanBeHeld();
public readonly record struct TryHold();
public readonly record struct CanHold();
public readonly record struct Solid();
public readonly record struct TouchingSolid();
public readonly record struct Name(int TextID);

public readonly record struct Score(int Value);
public readonly record struct DisplayScore(int Value);

public readonly record struct Price(float Value);
public readonly record struct TickerText(float Width);
public readonly record struct ColorBlend(Color Color);
public readonly record struct CanFillOrders();
public readonly record struct CanGiveOrders();
public readonly record struct IsOrder();

public readonly record struct ColorSpeed(float RedSpeed, float GreenSpeed, float BlueSpeed);

public readonly record struct Depth(float Value);
public readonly record struct DrawAsRectangle();

public readonly record struct TextDropShadow(int OffsetX, int OffsetY);
public readonly record struct ForceIntegerMovement();
public readonly record struct MaxSpeed(float Value);

public readonly record struct AdjustFramerateToSpeed();
public readonly record struct FunnyRunTimer(float Time); //Scooby doo style quick run when starting to move
public readonly record struct CanFunnyRun();

public readonly record struct LastDirection(System.Numerics.Vector2 Direction);
public readonly record struct SlowDownAnimation(int BaseSpeed, int step);

public readonly record struct IsPopupBox(); // jank because we cant check relation type count
public readonly record struct SpawnCategory(Category Category);
public readonly record struct CanSpawn(int Width, int Height);
public readonly record struct FallSpeed(float Speed);
public readonly record struct DestroyAtScreenBottom();

public readonly record struct IsScoreScreen(); // sorry
public readonly record struct GameInProgress(); // yaaargh

public readonly record struct DirectionalSprites(
    SpriteAnimationInfoID Up,
    SpriteAnimationInfoID UpRight,
    SpriteAnimationInfoID Right,
    SpriteAnimationInfoID DownRight,
    SpriteAnimationInfoID Down,
    SpriteAnimationInfoID DownLeft,
    SpriteAnimationInfoID Left,
    SpriteAnimationInfoID UpLeft
    );

public readonly record struct CanTalk();
public readonly record struct DontSpawnNPCs();
public readonly record struct StoreExit();
public readonly record struct AccelerateToPosition(Position Target, float Acceleration, float MotionDampFactor);
public readonly record struct DestroyAtGameEnd();

public readonly record struct CanBeStolenFrom();
public readonly record struct CanStealProducts();
public readonly record struct CanTargetProductSpawner();
public readonly record struct DestroyWhenOutOfBounds();

public readonly record struct WaitingForProductRestock();
public readonly record struct DestroyForDebugTestReasons();
public readonly record struct ColorFlicker(int ElapsedFrames, Color Color);
public readonly record struct MotionDamp(float Damping);
public readonly record struct SpriteScale(System.Numerics.Vector2 Scale);
public readonly record struct LastValue(int value);
public readonly record struct PlaySoundOnTimerEnd(PlayStaticSoundMessage PlayStaticSoundMessage);
