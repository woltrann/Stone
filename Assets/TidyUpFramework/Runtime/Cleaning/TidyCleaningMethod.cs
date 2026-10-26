using System;

namespace TidyUpFramework.Cleaning
{
    [Flags]
    public enum TidyCleaningMethod
    {
        None = 0,
        BareHands = 1,
        Scrubbing = 2,
        Vacuum = 4
    }

    public enum TidyCleaningToolMethod
    {
        Automatic,
        Scrubbing,
        Vacuum
    }
}
