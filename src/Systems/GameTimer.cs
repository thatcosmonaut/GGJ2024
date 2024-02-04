using System;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Input;

namespace RollAndCash.Systems;

public class GameTimer : MoonTools.ECS.System
{
    GameLoopManipulator GameLoopManipulator;

    public GameTimer(World world) : base(world)
    {
        GameLoopManipulator = new GameLoopManipulator(world);
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Components.GameTimer>())
        {
            return;
        }

        var timerEntity = GetSingletonEntity<Components.GameTimer>();
        var time = Get<Components.GameTimer>(timerEntity).Time;

        var timeBefore = time;

        time -= (float)delta.TotalSeconds;

        Set(timerEntity, new Components.GameTimer(time));

        var timeSpan = new TimeSpan(0, 0, (int)time);
        var timeString = timeSpan.ToString(@"m\:ss"); // this is really bad for string memory usage but whatevsies lol -evan

        Set(timerEntity, new Text(Fonts.KosugiID, 16, timeString, MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (time <= 0)
        {
			Set(timerEntity, new Components.GameTimer(5));
            GameLoopManipulator.AdvanceGameState();
        }
    }

    public static bool OnTime(float time, float triggerTime, float dt, float loopTime)
    {
        if (loopTime == 0)
        {
            return false;
        }

        var t = time % loopTime;
        return (
            (t <= triggerTime && t + dt >= triggerTime) ||
            (t <= triggerTime + loopTime && t + dt >= triggerTime + loopTime)
            );
    }
}
