using GGJ2024.Components;
using GGJ2024.Messages;
using MoonTools.ECS;
using MoonWorks.Math.Float;

namespace GGJ2024.Systems;

public class PlayerController : MoonTools.ECS.System
{
    Filter PlayerFilter;
    float Speed = 64.0f;

    public PlayerController(World world) : base(world)
    {
        PlayerFilter =
        FilterBuilder
        .Include<Player>()
        .Include<Position>()
        .Build();
    }

    public override void Update(System.TimeSpan delta)
    {
        foreach (var entity in PlayerFilter.Entities)
        {
            var player = Get<Player>(entity).Index;
            var velocity = Vector2.Zero;

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
                }
            }

            Set(entity, new Velocity(velocity * Speed * (float)delta.TotalSeconds));
        }
    }
}