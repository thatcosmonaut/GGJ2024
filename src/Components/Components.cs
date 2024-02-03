using MoonWorks.Math.Float;
using MoonWorks.Graphics;
using RollAndCash.Systems;
using RollAndCash.Data;

namespace RollAndCash.Components;

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

public readonly record struct LastDirection(MoonWorks.Math.Float.Vector2 Direction);
public readonly record struct SlowDownAnimation(int BaseSpeed, int step);

public readonly record struct IsPopupBox(); // jank because we cant check relation type count
public readonly record struct SpawnCategory(Category Category);
public readonly record struct CanSpawn(int Width, int Height);
public readonly record struct FallSpeed(float Speed);
public readonly record struct DestroyAtScreenBottom();

public readonly record struct IsTitleScreen(); // bleeeh
public readonly record struct IsScoreScreen(); // sorry

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

public readonly record struct HiResArt();
