using System;
using System.Numerics;

public readonly record struct Position
{
    private readonly Vector2 RawPosition;
    public readonly int X { get; }
    public readonly int Y { get; }

    public Position(float x, float y)
    {
        RawPosition = new Vector2(x, y);
        X = (int)MathF.Round(x);
        Y = (int)MathF.Round(y);
    }

    public Position(int x, int y)
    {
        RawPosition = new Vector2(x, y);
        X = x;
        Y = y;
    }

    public Position(Vector2 v)
    {
        RawPosition = v;
        X = (int)MathF.Round(v.X);
        Y = (int)MathF.Round(v.Y);
    }

    public Position SetX(int x)
    {
        return new Position((float)x, RawPosition.Y);
    }

    public Position SetY(int y)
    {
        return new Position(RawPosition.X, (float)y);
    }

    public static Position operator +(Position a, Position b)
    {
        return new Position(a.RawPosition + b.RawPosition);
    }

    public static Vector2 operator -(Position a, Position b)
    {
        return a.RawPosition - b.RawPosition;
    }

    public static Position operator +(Position a, Vector2 b)
    {
        return new Position(a.RawPosition + b);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

}
