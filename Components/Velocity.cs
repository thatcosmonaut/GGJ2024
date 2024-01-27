using MoonWorks.Math.Float;

public readonly record struct Velocity
{
    private readonly Vector2 Value;

    public readonly float X
    {
        get
        {
            return Value.X;
        }
    }

    public readonly float Y
    {
        get
        {
            return Value.Y;
        }
    }

    public static implicit operator Vector2(Velocity v) => v.Value;

}