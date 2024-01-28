using GGJ2024.Components;
using GGJ2024.Messages;
using MoonTools.ECS;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024.Systems;

public class PlayerController : MoonTools.ECS.System
{
    MoonTools.ECS.Filter PlayerFilter;
    float Speed = 32f;

    public PlayerController(World world) : base(world)
    {
        PlayerFilter =
        FilterBuilder
        .Include<Player>()
        .Include<Position>()
        .Build();
    }

    public void SpawnPlayer(int index)
    {
        var player = World.CreateEntity();
        World.Set(player, new Position(Dimensions.GAME_W * 0.5f, Dimensions.GAME_H * 0.5f + index * 32.0f));
        World.Set(player, new Rectangle(0, 0, 16, 16));
        World.Set(player, new Player(index, 0));
        World.Set(player, new CanHold());
        World.Set(player, new Solid());
        World.Set(player, index == 0 ? Color.Green : Color.Blue);
    }

    public override void Update(System.TimeSpan delta)
    {
        if (!Some<Player>())
        {
            SpawnPlayer(0);
            SpawnPlayer(1);
        }

        foreach (var entity in PlayerFilter.Entities)
        {
            var player = Get<Player>(entity).Index;
            var velocity = Vector2.Zero;
            if (Has<TryHold>(entity))
                Remove<TryHold>(entity);

            foreach (var action in ReadMessages<Action>())
            {
                if (action.Index == player)
                {
                    if (action.ActionType == Actions.MoveX)
                    {
                        velocity.X += action.Value;
                    }
                    else if (action.ActionType == Actions.MoveY)
                    {
                        velocity.Y += action.Value;
                    }

                    if (action.ActionType == Actions.Interact && action.ActionState == ActionState.Pressed)
                    {
                        Set(entity, new TryHold());
                    }
                }
            }

            Set(entity, new Velocity(velocity * Speed));
        }
    }
}