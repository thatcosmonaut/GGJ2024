using GGJ2024.Utility;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class Orders : Manipulator
{
    public Orders(World world) : base(world)
    {
    }

    public void GetNewOrder()
    {

        if (Rando.Value < 0.5f)
        { // require category

        }
        else
        { // require ingredient

        }
    }
}