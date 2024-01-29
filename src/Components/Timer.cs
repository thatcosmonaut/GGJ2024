

namespace RollAndCash.Components;

public readonly record struct Timer(float Time, float Max)
{
    public float Remaining => Time / Max;
    public Timer(float time) : this(time, time) { }
}
