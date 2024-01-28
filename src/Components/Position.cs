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

	public Position SetX(int x)
	{
		return new Position((float) x, RealPosition.Y);
	}

	public Position SetY(int y)
	{
		return new Position(RealPosition.X, (float) y);
	}

    public static Position operator +(Position a, Position b)
    {
        return new Position(a.RealPosition + b.RealPosition);
    }

    public static Vector2 operator -(Position a, Position b)
    {
        return a.RealPosition - b.RealPosition;
    }

    public static Position operator +(Position a, Vector2 b)
    {
        return new Position(a.RealPosition + b);
    }

}