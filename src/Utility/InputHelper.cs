using System;
using System.Numerics;
using MoonWorks.Input;
using MoonWorks.Math;

namespace GGJ2024.Utility;

public enum GenericInputs
{
    //KEYS
    Unknown,
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
    D1,
    D2,
    D3,
    D4,
    D5,
    D6,
    D7,
    D8,
    D9,
    D0,
    Return,
    Escape,
    Backspace,
    Tab,
    Space,
    Minus,
    Equals,
    LeftBracket,
    RightBracket,
    Backslash,
    NonUSHash,
    Semicolon,
    Apostrophe,
    Grave,
    Comma,
    Period,
    Slash,
    CapsLock,
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
    PrintScreen,
    ScrollLock,
    Pause,
    Insert,
    Home,
    PageUp,
    Delete,
    End,
    PageDown,
    Right,
    Left,
    Down,
    Up,
    NumLockClear,
    KeypadDivide,
    KeypadMultiply,
    KeypadMinus,
    KeypadPlus,
    KeypadEnter,
    Keypad1,
    Keypad2,
    Keypad3,
    Keypad4,
    Keypad5,
    Keypad6,
    Keypad7,
    Keypad8,
    Keypad9,
    Keypad0,
    KeypadPeriod,
    NonUSBackslash,
    LeftControl,
    LeftShift,
    LeftAlt,
    LeftMeta,
    RightControl,
    RightShift,
    RightAlt,
    RightMeta
,

    //GAMEPAD BUTTONS
    AButton,
    BButton,
    XButton,
    YButton,
    BackButton,
    GuideButton,
    StartButton,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,

    //ANALOG STICKS
    LeftX,
    LeftY,
    RightX,
    RightY,

    //MOUSE AXES
    MouseX,
    MouseY,

    //MOUSE BUTTONS
    LeftMouse,
    RightMouse,
    Middle,
    X1,
    X2,

}

public static class InputHelper
{
    static Vector2 MouseDelta = default;
    static Vector2 LastMousePosition = default;

    static bool PollButton(Inputs inputs, GamepadButtonCode btn, int index)
    {

        if (index == 0 && inputs.GamepadExists(0) && !inputs.GamepadExists(1))
            return false;

        if (index == 1 && inputs.GamepadExists(0) && !inputs.GamepadExists(1))
            index = 0;

        if (inputs.GamepadExists(index))
        {
            return inputs.GetGamepad(index).Button(btn).IsDown;
        }
        return false;
    }

    static bool PollMouseButton(Inputs inputs, MouseButtonCode btn)
    {
        return inputs.Mouse.ButtonState(btn).IsDown;
    }

    static float PollStickAxis(Inputs inputs, AxisCode axis, int index)
    {
        var gamepadIndex = index;

        if (index == 0 && inputs.GamepadExists(0) && !inputs.GamepadExists(1))
            return 0.0f;

        if (index == 1 && inputs.GamepadExists(0) && !inputs.GamepadExists(1))
            gamepadIndex = 0;


        if (inputs.GamepadExists(gamepadIndex))
        {
            var value = inputs.GetGamepad(gamepadIndex).AxisValue(axis);
            if (MathF.Abs(value) > 0.5f)
            {
                return value;
            }
        }

        return 0.0f;
    }

    static bool PollKey(Inputs inputs, KeyCode key, int index)
    {
        if (!inputs.GamepadExists(1))
        {
            if (index == 0)
                return inputs.Keyboard.IsDown(key);
        }
        return false;
    }


    public static void PollMouse(Inputs inputs)
    {
        MouseDelta.X = inputs.Mouse.DeltaX;
        MouseDelta.Y = inputs.Mouse.DeltaY;
    }

    public static float Poll(Inputs inputs, GenericInputs input, int index)
    {
        switch (input)
        {
            case GenericInputs.Unknown:
                if (PollKey(inputs, KeyCode.Unknown, index)) return 1.0f;
                break;

            case GenericInputs.A:
                if (PollKey(inputs, KeyCode.A, index)) return 1.0f;
                break;

            case GenericInputs.B:
                if (PollKey(inputs, KeyCode.B, index)) return 1.0f;
                break;

            case GenericInputs.C:
                if (PollKey(inputs, KeyCode.C, index)) return 1.0f;
                break;

            case GenericInputs.D:
                if (PollKey(inputs, KeyCode.D, index)) return 1.0f;
                break;

            case GenericInputs.E:
                if (PollKey(inputs, KeyCode.E, index)) return 1.0f;
                break;

            case GenericInputs.F:
                if (PollKey(inputs, KeyCode.F, index)) return 1.0f;
                break;

            case GenericInputs.G:
                if (PollKey(inputs, KeyCode.G, index)) return 1.0f;
                break;

            case GenericInputs.H:
                if (PollKey(inputs, KeyCode.H, index)) return 1.0f;
                break;

            case GenericInputs.I:
                if (PollKey(inputs, KeyCode.I, index)) return 1.0f;
                break;

            case GenericInputs.J:
                if (PollKey(inputs, KeyCode.J, index)) return 1.0f;
                break;

            case GenericInputs.K:
                if (PollKey(inputs, KeyCode.K, index)) return 1.0f;
                break;

            case GenericInputs.L:
                if (PollKey(inputs, KeyCode.L, index)) return 1.0f;
                break;

            case GenericInputs.M:
                if (PollKey(inputs, KeyCode.M, index)) return 1.0f;
                break;

            case GenericInputs.N:
                if (PollKey(inputs, KeyCode.N, index)) return 1.0f;
                break;

            case GenericInputs.O:
                if (PollKey(inputs, KeyCode.O, index)) return 1.0f;
                break;

            case GenericInputs.P:
                if (PollKey(inputs, KeyCode.P, index)) return 1.0f;
                break;

            case GenericInputs.Q:
                if (PollKey(inputs, KeyCode.Q, index)) return 1.0f;
                break;

            case GenericInputs.R:
                if (PollKey(inputs, KeyCode.R, index)) return 1.0f;
                break;

            case GenericInputs.S:
                if (PollKey(inputs, KeyCode.S, index)) return 1.0f;
                break;

            case GenericInputs.T:
                if (PollKey(inputs, KeyCode.T, index)) return 1.0f;
                break;

            case GenericInputs.U:
                if (PollKey(inputs, KeyCode.U, index)) return 1.0f;
                break;

            case GenericInputs.V:
                if (PollKey(inputs, KeyCode.V, index)) return 1.0f;
                break;

            case GenericInputs.W:
                if (PollKey(inputs, KeyCode.W, index)) return 1.0f;
                break;

            case GenericInputs.X:
                if (PollKey(inputs, KeyCode.X, index)) return 1.0f;
                break;

            case GenericInputs.Y:
                if (PollKey(inputs, KeyCode.Y, index)) return 1.0f;
                break;

            case GenericInputs.Z:
                if (PollKey(inputs, KeyCode.Z, index)) return 1.0f;
                break;

            case GenericInputs.D1:
                if (PollKey(inputs, KeyCode.D1, index)) return 1.0f;
                break;

            case GenericInputs.D2:
                if (PollKey(inputs, KeyCode.D2, index)) return 1.0f;
                break;

            case GenericInputs.D3:
                if (PollKey(inputs, KeyCode.D3, index)) return 1.0f;
                break;

            case GenericInputs.D4:
                if (PollKey(inputs, KeyCode.D4, index)) return 1.0f;
                break;

            case GenericInputs.D5:
                if (PollKey(inputs, KeyCode.D5, index)) return 1.0f;
                break;

            case GenericInputs.D6:
                if (PollKey(inputs, KeyCode.D6, index)) return 1.0f;
                break;

            case GenericInputs.D7:
                if (PollKey(inputs, KeyCode.D7, index)) return 1.0f;
                break;

            case GenericInputs.D8:
                if (PollKey(inputs, KeyCode.D8, index)) return 1.0f;
                break;

            case GenericInputs.D9:
                if (PollKey(inputs, KeyCode.D9, index)) return 1.0f;
                break;

            case GenericInputs.D0:
                if (PollKey(inputs, KeyCode.D0, index)) return 1.0f;
                break;

            case GenericInputs.Return:
                if (PollKey(inputs, KeyCode.Return, index)) return 1.0f;
                break;

            case GenericInputs.Escape:
                if (PollKey(inputs, KeyCode.Escape, index)) return 1.0f;
                break;

            case GenericInputs.Backspace:
                if (PollKey(inputs, KeyCode.Backspace, index)) return 1.0f;
                break;

            case GenericInputs.Tab:
                if (PollKey(inputs, KeyCode.Tab, index)) return 1.0f;
                break;

            case GenericInputs.Space:
                if (PollKey(inputs, KeyCode.Space, index)) return 1.0f;
                break;

            case GenericInputs.Minus:
                if (PollKey(inputs, KeyCode.Minus, index)) return 1.0f;
                break;

            case GenericInputs.Equals:
                if (PollKey(inputs, KeyCode.Equals, index)) return 1.0f;
                break;

            case GenericInputs.LeftBracket:
                if (PollKey(inputs, KeyCode.LeftBracket, index)) return 1.0f;
                break;

            case GenericInputs.RightBracket:
                if (PollKey(inputs, KeyCode.RightBracket, index)) return 1.0f;
                break;

            case GenericInputs.Backslash:
                if (PollKey(inputs, KeyCode.Backslash, index)) return 1.0f;
                break;

            case GenericInputs.NonUSHash:
                if (PollKey(inputs, KeyCode.NonUSHash, index)) return 1.0f;
                break;

            case GenericInputs.Semicolon:
                if (PollKey(inputs, KeyCode.Semicolon, index)) return 1.0f;
                break;

            case GenericInputs.Apostrophe:
                if (PollKey(inputs, KeyCode.Apostrophe, index)) return 1.0f;
                break;

            case GenericInputs.Grave:
                if (PollKey(inputs, KeyCode.Grave, index)) return 1.0f;
                break;

            case GenericInputs.Comma:
                if (PollKey(inputs, KeyCode.Comma, index)) return 1.0f;
                break;

            case GenericInputs.Period:
                if (PollKey(inputs, KeyCode.Period, index)) return 1.0f;
                break;

            case GenericInputs.Slash:
                if (PollKey(inputs, KeyCode.Slash, index)) return 1.0f;
                break;

            case GenericInputs.CapsLock:
                if (PollKey(inputs, KeyCode.CapsLock, index)) return 1.0f;
                break;

            case GenericInputs.F1:
                if (PollKey(inputs, KeyCode.F1, index)) return 1.0f;
                break;

            case GenericInputs.F2:
                if (PollKey(inputs, KeyCode.F2, index)) return 1.0f;
                break;

            case GenericInputs.F3:
                if (PollKey(inputs, KeyCode.F3, index)) return 1.0f;
                break;

            case GenericInputs.F4:
                if (PollKey(inputs, KeyCode.F4, index)) return 1.0f;
                break;

            case GenericInputs.F5:
                if (PollKey(inputs, KeyCode.F5, index)) return 1.0f;
                break;

            case GenericInputs.F6:
                if (PollKey(inputs, KeyCode.F6, index)) return 1.0f;
                break;

            case GenericInputs.F7:
                if (PollKey(inputs, KeyCode.F7, index)) return 1.0f;
                break;

            case GenericInputs.F8:
                if (PollKey(inputs, KeyCode.F8, index)) return 1.0f;
                break;

            case GenericInputs.F9:
                if (PollKey(inputs, KeyCode.F9, index)) return 1.0f;
                break;

            case GenericInputs.F10:
                if (PollKey(inputs, KeyCode.F10, index)) return 1.0f;
                break;

            case GenericInputs.F11:
                if (PollKey(inputs, KeyCode.F11, index)) return 1.0f;
                break;

            case GenericInputs.F12:
                if (PollKey(inputs, KeyCode.F12, index)) return 1.0f;
                break;

            case GenericInputs.PrintScreen:
                if (PollKey(inputs, KeyCode.PrintScreen, index)) return 1.0f;
                break;

            case GenericInputs.ScrollLock:
                if (PollKey(inputs, KeyCode.ScrollLock, index)) return 1.0f;
                break;

            case GenericInputs.Pause:
                if (PollKey(inputs, KeyCode.Pause, index)) return 1.0f;
                break;

            case GenericInputs.Insert:
                if (PollKey(inputs, KeyCode.Insert, index)) return 1.0f;
                break;

            case GenericInputs.Home:
                if (PollKey(inputs, KeyCode.Home, index)) return 1.0f;
                break;

            case GenericInputs.PageUp:
                if (PollKey(inputs, KeyCode.PageUp, index)) return 1.0f;
                break;

            case GenericInputs.Delete:
                if (PollKey(inputs, KeyCode.Delete, index)) return 1.0f;
                break;

            case GenericInputs.End:
                if (PollKey(inputs, KeyCode.End, index)) return 1.0f;
                break;

            case GenericInputs.PageDown:
                if (PollKey(inputs, KeyCode.PageDown, index)) return 1.0f;
                break;

            case GenericInputs.Right:
                if (PollKey(inputs, KeyCode.Right, index)) return 1.0f;
                break;

            case GenericInputs.Left:
                if (PollKey(inputs, KeyCode.Left, index)) return 1.0f;
                break;

            case GenericInputs.Down:
                if (PollKey(inputs, KeyCode.Down, index)) return 1.0f;
                break;

            case GenericInputs.Up:
                if (PollKey(inputs, KeyCode.Up, index)) return 1.0f;
                break;

            case GenericInputs.NumLockClear:
                if (PollKey(inputs, KeyCode.NumLockClear, index)) return 1.0f;
                break;

            case GenericInputs.KeypadDivide:
                if (PollKey(inputs, KeyCode.KeypadDivide, index)) return 1.0f;
                break;

            case GenericInputs.KeypadMultiply:
                if (PollKey(inputs, KeyCode.KeypadMultiply, index)) return 1.0f;
                break;

            case GenericInputs.KeypadMinus:
                if (PollKey(inputs, KeyCode.KeypadMinus, index)) return 1.0f;
                break;

            case GenericInputs.KeypadPlus:
                if (PollKey(inputs, KeyCode.KeypadPlus, index)) return 1.0f;
                break;

            case GenericInputs.KeypadEnter:
                if (PollKey(inputs, KeyCode.KeypadEnter, index)) return 1.0f;
                break;

            case GenericInputs.Keypad1:
                if (PollKey(inputs, KeyCode.Keypad1, index)) return 1.0f;
                break;

            case GenericInputs.Keypad2:
                if (PollKey(inputs, KeyCode.Keypad2, index)) return 1.0f;
                break;

            case GenericInputs.Keypad3:
                if (PollKey(inputs, KeyCode.Keypad3, index)) return 1.0f;
                break;

            case GenericInputs.Keypad4:
                if (PollKey(inputs, KeyCode.Keypad4, index)) return 1.0f;
                break;

            case GenericInputs.Keypad5:
                if (PollKey(inputs, KeyCode.Keypad5, index)) return 1.0f;
                break;

            case GenericInputs.Keypad6:
                if (PollKey(inputs, KeyCode.Keypad6, index)) return 1.0f;
                break;

            case GenericInputs.Keypad7:
                if (PollKey(inputs, KeyCode.Keypad7, index)) return 1.0f;
                break;

            case GenericInputs.Keypad8:
                if (PollKey(inputs, KeyCode.Keypad8, index)) return 1.0f;
                break;

            case GenericInputs.Keypad9:
                if (PollKey(inputs, KeyCode.Keypad9, index)) return 1.0f;
                break;

            case GenericInputs.Keypad0:
                if (PollKey(inputs, KeyCode.Keypad0, index)) return 1.0f;
                break;

            case GenericInputs.KeypadPeriod:
                if (PollKey(inputs, KeyCode.KeypadPeriod, index)) return 1.0f;
                break;

            case GenericInputs.NonUSBackslash:
                if (PollKey(inputs, KeyCode.NonUSBackslash, index)) return 1.0f;
                break;

            case GenericInputs.LeftControl:
                if (PollKey(inputs, KeyCode.LeftControl, index)) return 1.0f;
                break;

            case GenericInputs.LeftShift:
                if (PollKey(inputs, KeyCode.LeftShift, index)) return 1.0f;
                break;

            case GenericInputs.LeftAlt:
                if (PollKey(inputs, KeyCode.LeftAlt, index)) return 1.0f;
                break;

            case GenericInputs.LeftMeta:
                if (PollKey(inputs, KeyCode.LeftMeta, index)) return 1.0f;
                break;

            case GenericInputs.RightControl:
                if (PollKey(inputs, KeyCode.RightControl, index)) return 1.0f;
                break;

            case GenericInputs.RightShift:
                if (PollKey(inputs, KeyCode.RightShift, index)) return 1.0f;
                break;

            case GenericInputs.RightAlt:
                if (PollKey(inputs, KeyCode.RightAlt, index)) return 1.0f;
                break;

            case GenericInputs.RightMeta
    :
                if (PollKey(inputs, KeyCode.RightMeta
    , index)) return 1.0f;
                break;

            case GenericInputs.AButton:
                if (PollButton(inputs, GamepadButtonCode.A, index)) return 1.0f;
                break;

            case GenericInputs.BButton:
                if (PollButton(inputs, GamepadButtonCode.B, index)) return 1.0f;
                break;

            case GenericInputs.XButton:
                if (PollButton(inputs, GamepadButtonCode.X, index)) return 1.0f;
                break;

            case GenericInputs.YButton:
                if (PollButton(inputs, GamepadButtonCode.Y, index)) return 1.0f;
                break;

            case GenericInputs.BackButton:
                if (PollButton(inputs, GamepadButtonCode.Back, index)) return 1.0f;
                break;

            case GenericInputs.GuideButton:
                if (PollButton(inputs, GamepadButtonCode.Guide, index)) return 1.0f;
                break;

            case GenericInputs.StartButton:
                if (PollButton(inputs, GamepadButtonCode.Start, index)) return 1.0f;
                break;

            case GenericInputs.LeftStick:
                if (PollButton(inputs, GamepadButtonCode.LeftStick, index)) return 1.0f;
                break;

            case GenericInputs.RightStick:
                if (PollButton(inputs, GamepadButtonCode.RightStick, index)) return 1.0f;
                break;

            case GenericInputs.LeftShoulder:
                if (PollButton(inputs, GamepadButtonCode.LeftShoulder, index)) return 1.0f;
                break;

            case GenericInputs.RightShoulder:
                if (PollButton(inputs, GamepadButtonCode.RightShoulder, index)) return 1.0f;
                break;

            case GenericInputs.DpadUp:
                if (PollButton(inputs, GamepadButtonCode.DpadUp, index)) return 1.0f;
                break;

            case GenericInputs.DpadDown:
                if (PollButton(inputs, GamepadButtonCode.DpadDown, index)) return 1.0f;
                break;

            case GenericInputs.DpadLeft:
                if (PollButton(inputs, GamepadButtonCode.DpadLeft, index)) return 1.0f;
                break;

            case GenericInputs.DpadRight:
                if (PollButton(inputs, GamepadButtonCode.DpadRight, index)) return 1.0f;
                break;

            case GenericInputs.LeftX:
                var LeftXValue = PollStickAxis(inputs, AxisCode.LeftX, index);
                if (System.MathF.Abs(LeftXValue) > 0.0f)
                    return LeftXValue;
                break;

            case GenericInputs.LeftY:
                var LeftYValue = PollStickAxis(inputs, AxisCode.LeftY, index);
                if (System.MathF.Abs(LeftYValue) > 0.0f)
                    return LeftYValue;
                break;

            case GenericInputs.RightX:
                var RightXValue = PollStickAxis(inputs, AxisCode.RightX, index);
                if (System.MathF.Abs(RightXValue) > 0.0f)
                    return RightXValue;
                break;

            case GenericInputs.RightY:
                var RightYValue = PollStickAxis(inputs, AxisCode.RightY, index);
                if (System.MathF.Abs(RightYValue) > 0.0f)
                    return RightYValue;
                break;

            case GenericInputs.MouseX:
                if (System.MathF.Abs(MouseDelta.X) > 0.0f)
                    return MouseDelta.X;
                break;

            case GenericInputs.MouseY:
                if (System.MathF.Abs(MouseDelta.Y) > 0.0f)
                    return MouseDelta.Y;
                break;

            case GenericInputs.LeftMouse:
                if (PollMouseButton(inputs, MouseButtonCode.Left)) return 1.0f;
                break;

            case GenericInputs.RightMouse:
                if (PollMouseButton(inputs, MouseButtonCode.Right)) return 1.0f;
                break;

            case GenericInputs.Middle:
                if (PollMouseButton(inputs, MouseButtonCode.Middle)) return 1.0f;
                break;

            case GenericInputs.X1:
                if (PollMouseButton(inputs, MouseButtonCode.X1)) return 1.0f;
                break;

            case GenericInputs.X2:
                if (PollMouseButton(inputs, MouseButtonCode.X2)) return 1.0f;
                break;

        }

        return 0.0f;
    }
}