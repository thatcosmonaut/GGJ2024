using System;

namespace RollAndCash.Utility;

public ref struct IntegerEnumerator
{
    private int i;
    private int End;
    private int Increment;

    public IntegerEnumerator GetEnumerator() => this;

    public IntegerEnumerator(int start, int end)
    {
        i = start;
        End = end;
        if (end >= start)
        {
            Increment = 1;
        }
        else if (end < start)
        {
            Increment = -1;
        }
        else
        {
            Increment = 0;
        }
    }

    // does not include a, but does include b.
    public static IntegerEnumerator IntegersBetween(int a, int b)
    {
        return new IntegerEnumerator(a, b);
    }

    public bool MoveNext()
    {
        i += Increment;
        return (Increment > 0 && i <= End) || (Increment < 0 && i >= End);
    }

    public int Current
    {
        get => i;
    }
}