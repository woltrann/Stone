using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Core
{
    [Flags]
    public enum TidyControlChannel
    {
        None = 0,
        Movement = 1,
        Look = 2,
        Interaction = 4,
        Cursor = 8,
        All = Movement | Look | Interaction | Cursor
    }

    public static class TidyControlLock
    {
        static readonly Dictionary<object, TidyControlChannel> LocksByOwner = new Dictionary<object, TidyControlChannel>();
        static TidyControlChannel combined;

        public static event Action<TidyControlChannel> Changed;

        public static TidyControlChannel Locked => combined;

        public static bool IsLocked(TidyControlChannel channel)
        {
            return (combined & channel) != 0;
        }

        public static void Acquire(object owner, TidyControlChannel channels)
        {
            if (owner == null)
            {
                return;
            }

            LocksByOwner[owner] = channels;
            Recalculate();
        }

        public static void Release(object owner)
        {
            if (owner != null && LocksByOwner.Remove(owner))
            {
                Recalculate();
            }
        }

        static void Recalculate()
        {
            var next = TidyControlChannel.None;
            foreach (var channels in LocksByOwner.Values)
            {
                next |= channels;
            }

            if (next == combined)
            {
                return;
            }

            combined = next;
            Changed?.Invoke(combined);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            LocksByOwner.Clear();
            combined = TidyControlChannel.None;
            Changed = null;
        }
    }
}
