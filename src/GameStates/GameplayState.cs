using System;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Relations;
using RollAndCash.Systems;

namespace RollAndCash.GameStates;

public class GameplayState : GameState
{
    RollAndCashGame Game;

    Renderer Renderer;
    World World = new World();
    Input Input;
    Motion Motion;
    Audio Audio;
    Hold Hold;
    ProductSpawner ProductSpawner;
    ShelfSpawner ShelfSpawner;
    Ticker Ticker;
    Systems.GameTimer GameTimer;
    Timing Timing;
    Orders Orders;
    SetSpriteAnimationSystem SetSpriteAnimationSystem;
    DirectionalAnimation DirectionalAnimation;
    UpdateSpriteAnimationSystem UpdateSpriteAnimationSystem;
    ColorAnimation ColorAnimation;
    NPCController NPCController;
    DroneController DroneController;
    PlayerController PlayerController;

    GameLoopManipulator GameLoopManipulator;

    public GameplayState(RollAndCashGame game)
    {
        Game = game;

        GameLoopManipulator = new GameLoopManipulator(World);

        GameTimer = new(World);
        Timing = new(World);
        Input = new Input(World, Game.Inputs);
        Motion = new Motion(World);
        Audio = new Audio(World, Game.AudioDevice);
        PlayerController = new PlayerController(World);
        Hold = new Hold(World);
        Orders = new Orders(World);
        ProductSpawner = new ProductSpawner(World);
        ShelfSpawner = new ShelfSpawner(World);
        SetSpriteAnimationSystem = new SetSpriteAnimationSystem(World);
        UpdateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(World);
        ColorAnimation = new ColorAnimation(World);
        DirectionalAnimation = new DirectionalAnimation(World);
        NPCController = new NPCController(World);
        DroneController = new DroneController(World);

        CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
        cats.Initialize(World);

        Ticker = new Ticker(World, cats);

        Renderer = new Renderer(World, Game.GraphicsDevice, Game.MainWindow.SwapchainFormat);
    }

    public override void Start()
    {
        NPCController.SpawnNPC();

        var topBorder = World.CreateEntity();
        World.Set(topBorder, new Position(0, 65));
        World.Set(topBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        World.Set(topBorder, new Solid());

        var leftBorder = World.CreateEntity();
        World.Set(leftBorder, new Position(-10, 0));
        World.Set(leftBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        World.Set(leftBorder, new Solid());

        var rightBorder = World.CreateEntity();
        World.Set(rightBorder, new Position(Dimensions.GAME_W, 0));
        World.Set(rightBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        World.Set(rightBorder, new Solid());

        var bottomBorder = World.CreateEntity();
        World.Set(bottomBorder, new Position(0, Dimensions.GAME_H));
        World.Set(bottomBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        World.Set(bottomBorder, new Solid());

        var background = World.CreateEntity();
        World.Set(background, new Position(0, 0));
        World.Set(background, new Depth(999));
        World.Set(background, new SpriteAnimation(Content.SpriteAnimations.BG, 0));

        var uiTickerBackground = World.CreateEntity();
        World.Set(uiTickerBackground, new Position(0, 0));
        World.Set(uiTickerBackground, new Depth(1));
        World.Set(uiTickerBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Ticker, 0));

        var uiBottomBackground = World.CreateEntity();
        World.Set(uiBottomBackground, new Position(0, Dimensions.GAME_H - 40));
        World.Set(uiBottomBackground, new Depth(9));
        World.Set(uiBottomBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Bottom, 0));

        Orders.InitializeOrders();

		var cashRegisterLeft = World.CreateEntity();
        World.Set(cashRegisterLeft, new Position(8, 77));
        World.Set(cashRegisterLeft, new Rectangle(0, 0, 80, 90));
        World.Set(cashRegisterLeft, new CanInteract());
        World.Set(cashRegisterLeft, new CanFillOrders());
        World.Set(cashRegisterLeft, Color.ForestGreen);

        var cashRegisterRight = World.CreateEntity();
        World.Set(cashRegisterRight, new Position(Dimensions.GAME_W - 88, 77));
        World.Set(cashRegisterRight, new Rectangle(0, 0, 80, 90));
        World.Set(cashRegisterRight, new CanInteract());
        World.Set(cashRegisterRight, new CanFillOrders());
        World.Set(cashRegisterRight, Color.ForestGreen);

        var exit = World.CreateEntity();
        World.Set(exit, new Position(Dimensions.GAME_W * 0.5f - 44, 0));
        World.Set(exit, new Rectangle(0, 0, 80, 88));
        World.Set(exit, new StoreExit());
        World.Set(exit, new CanInteract());

        var timer = World.CreateEntity();
        World.Set(timer, new Components.GameTimer(90));
        World.Set(timer, new Position(Dimensions.GAME_W * 0.5f, 38));
        World.Set(timer, new TextDropShadow(1, 1));

        var scoreOne = World.CreateEntity();
        World.Set(scoreOne, new Position(80, 345));
        World.Set(scoreOne, new Score(0));
        World.Set(scoreOne, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var scoreTwo = World.CreateEntity();
        World.Set(scoreTwo, new Position(560, 345));
        World.Set(scoreTwo, new Score(0));
        World.Set(scoreTwo, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var playerOne = PlayerController.SpawnPlayer(0);
        var playerTwo = PlayerController.SpawnPlayer(1);

        World.Relate(playerOne, scoreOne, new HasScore());
        World.Relate(playerTwo, scoreTwo, new HasScore());

        ShelfSpawner.SpawnShelves();
        ProductSpawner.SpawnAllProducts();

        GameLoopManipulator.ShowTitleScreen();
    }

    public override void Update(TimeSpan dt)
    {
        Timing.Update(dt);
        UpdateSpriteAnimationSystem.Update(dt);
        GameTimer.Update(dt);
        Ticker.Update(dt);
        Input.Update(dt);
        PlayerController.Update(dt);
        NPCController.Update(dt);
        DroneController.Update(dt);
        Motion.Update(dt);
        Hold.Update(dt);
        Orders.Update(dt);
        DirectionalAnimation.Update(dt);
        SetSpriteAnimationSystem.Update(dt);
        ColorAnimation.Update(dt);
        Audio.Update(dt);

        World.FinishUpdate();
    }

    public override void Draw(Window window, double alpha)
    {
		Renderer.Render(Game.MainWindow);
    }

    public override void End()
    {

    }
}