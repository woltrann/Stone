using System;
using UnityEngine;

namespace TidyUpFramework.Input
{
    public enum TidyInputScheme
    {
        KeyboardMouse,
        Gamepad,
        Touch
    }

    public enum TidyInputAction
    {
        Move,
        Look,
        Interact,
        Drop,
        Throw,
        Rotate,
        Inspect,
        Sprint,
        Crouch,
        Hint,
        Pause
    }

    public interface ITidyPlayerInput
    {
        TidyInputScheme ActiveScheme { get; }
        Vector2 Move { get; }
        Vector2 LookDelta { get; }
        bool SprintHeld { get; }
        bool CrouchHeld { get; }
        bool InteractPressed { get; }
        bool InteractHeld { get; }
        bool DropPressed { get; }
        bool ThrowPressed { get; }
        float RotateAxis { get; }
        int RotateSteps { get; }
        bool InspectPressed { get; }
        bool HintPressed { get; }
        bool PausePressed { get; }
        bool HasPointerAim { get; }
        Vector2 PointerAimPosition { get; }

        event Action<TidyInputScheme> SchemeChanged;
    }
}
