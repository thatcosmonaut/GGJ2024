using System.Numerics;

public readonly record struct Velocity
{
    public static Velocity Zero = new Velocity(0f, 0f);
    public static Velocity One = new Velocity(1f, 1f);

    public readonly Vector2 Value;

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

    public Velocity(Vector2 v)
    {
        Value = v;
    }

    public Velocity(float x, float y)
    {
        Value = new Vector2(x, y);
    }

    public static implicit operator Vector2(Velocity v) => v.Value;


}
