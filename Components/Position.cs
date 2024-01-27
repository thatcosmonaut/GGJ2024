using MoonWorks.Math.Float;

public readonly record struct Position
{
    private readonly Vector2 RealPosition;
    public readonly int X
    {
        get
        {
            return (int)RealPosition.X;
        }
    }

    public readonly int Y
    {
        get
        {
            return (int)RealPosition.Y;
        }
    }

    public Position(float X, float Y)
    {
        RealPosition = new Vector2(X, Y);
    }

    public Position(int X, int Y)
    {
        RealPosition = new Vector2(X, Y);
    }

    public Position(Vector2 v)
    {
        RealPosition = v;
    }

    public static Position operator +(Position a, Position b)
    {
        return new Position(a.RealPosition + b.RealPosition);
    }

    public static Position operator -(Position a, Position b)
    {
        return new Position(a.RealPosition - b.RealPosition);
    }

    public static Position operator +(Position a, Vector2 b)
    {
        return new Position(a.RealPosition + b);
    }

    public static Position operator -(Position a, Vector2 b)
    {
        return new Position(a.RealPosition - b);
    }
}