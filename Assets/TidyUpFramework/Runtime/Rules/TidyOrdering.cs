using System;
using System.Collections.Generic;
using TidyUpFramework.Objects;

namespace TidyUpFramework.Rules
{
    public enum TidyOrderingMode
    {
        None,
        Alphabetical,
        Sequence,
        Priority,
        Size,
        Weight
    }

    public enum TidyOrderingDirection
    {
        Ascending,
        Descending
    }

    public enum TidyGroupingMode
    {
        None,
        SameDefinition,
        SameCategory,
        SameTagInGroup
    }

    public static class TidyOrdering
    {
        public static int Compare(TidyObject first, TidyObject second, TidyOrderingMode mode, TidyOrderingDirection direction)
        {
            var result = CompareAscending(first, second, mode);
            return direction == TidyOrderingDirection.Descending ? -result : result;
        }

        public static bool IsOrdered(IReadOnlyList<TidyObject> sequence, TidyOrderingMode mode, TidyOrderingDirection direction)
        {
            if (mode == TidyOrderingMode.None)
            {
                return true;
            }

            TidyObject previous = null;
            foreach (var current in sequence)
            {
                if (current == null)
                {
                    continue;
                }

                if (previous != null && Compare(previous, current, mode, direction) > 0)
                {
                    return false;
                }

                previous = current;
            }

            return true;
        }

        public static bool IsGrouped(IReadOnlyList<TidyObject> sequence, TidyGroupingMode mode, TidyTagGroup tagGroup)
        {
            if (mode == TidyGroupingMode.None)
            {
                return true;
            }

            object reference = null;
            var hasReference = false;
            foreach (var current in sequence)
            {
                if (current == null)
                {
                    continue;
                }

                var key = GroupKey(current, mode, tagGroup);
                if (!hasReference)
                {
                    reference = key;
                    hasReference = true;
                    continue;
                }

                if (!Equals(reference, key))
                {
                    return false;
                }
            }

            return true;
        }

        public static object GroupKey(TidyObject tidyObject, TidyGroupingMode mode, TidyTagGroup tagGroup)
        {
            switch (mode)
            {
                case TidyGroupingMode.SameDefinition:
                    return tidyObject.Definition;
                case TidyGroupingMode.SameCategory:
                    return tidyObject.Category;
                case TidyGroupingMode.SameTagInGroup:
                    return tidyObject.FindTag(tagGroup);
                default:
                    return null;
            }
        }

        static int CompareAscending(TidyObject first, TidyObject second, TidyOrderingMode mode)
        {
            switch (mode)
            {
                case TidyOrderingMode.Alphabetical:
                    return string.Compare(first.SortKey, second.SortKey, StringComparison.OrdinalIgnoreCase);
                case TidyOrderingMode.Sequence:
                    return first.SequenceIndex.CompareTo(second.SequenceIndex);
                case TidyOrderingMode.Priority:
                    return first.Priority.CompareTo(second.Priority);
                case TidyOrderingMode.Size:
                    return first.Size.CompareTo(second.Size);
                case TidyOrderingMode.Weight:
                    return first.Weight.CompareTo(second.Weight);
                default:
                    return 0;
            }
        }
    }
}
