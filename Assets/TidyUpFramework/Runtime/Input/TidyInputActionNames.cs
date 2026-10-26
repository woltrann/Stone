namespace TidyUpFramework.Input
{
    public static class TidyInputActionNames
    {
        public const string PlayerMap = "Player";
        public const string Move = "Move";
        public const string Look = "Look";
        public const string Interact = "Interact";
        public const string Drop = "Drop";
        public const string Throw = "Throw";
        public const string Rotate = "Rotate";
        public const string Inspect = "Inspect";
        public const string Sprint = "Sprint";
        public const string Crouch = "Crouch";
        public const string Hint = "Hint";
        public const string Pause = "Pause";

        public static string For(TidyInputAction action)
        {
            switch (action)
            {
                case TidyInputAction.Move:
                    return Move;
                case TidyInputAction.Look:
                    return Look;
                case TidyInputAction.Interact:
                    return Interact;
                case TidyInputAction.Drop:
                    return Drop;
                case TidyInputAction.Throw:
                    return Throw;
                case TidyInputAction.Rotate:
                    return Rotate;
                case TidyInputAction.Inspect:
                    return Inspect;
                case TidyInputAction.Sprint:
                    return Sprint;
                case TidyInputAction.Crouch:
                    return Crouch;
                case TidyInputAction.Hint:
                    return Hint;
                default:
                    return Pause;
            }
        }
    }
}
