using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Interaction
{
    public enum TidyHighlightStyle
    {
        None = 0,
        Hover = 1,
        Hint = 2,
        Valid = 3,
        Invalid = 4
    }

    public interface ITidyHighlighter
    {
        void SetHighlight(Component key, IReadOnlyList<Renderer> renderers, TidyHighlightStyle style, object owner);
        void ClearHighlight(Component key, object owner);
        void ClearOwner(object owner);
    }
}
