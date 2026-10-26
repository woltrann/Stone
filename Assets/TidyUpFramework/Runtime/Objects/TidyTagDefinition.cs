using UnityEngine;

namespace TidyUpFramework.Objects
{
    public enum TidyTagGroup
    {
        Generic,
        Color,
        Size,
        Shape,
        Material,
        Theme,
        Custom
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Tag", fileName = "Tag_New", order = 2)]
    public sealed class TidyTagDefinition : TidyDefinition
    {
        [SerializeField, Tooltip("Groups tags so rules and UI can ask for 'the colour' or 'the size' of an object.")]
        TidyTagGroup group = TidyTagGroup.Generic;

        public TidyTagGroup Group => group;

        public void AssignGroup(TidyTagGroup newGroup)
        {
            group = newGroup;
        }
    }
}
