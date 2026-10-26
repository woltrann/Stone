using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    public abstract class TidyRuleDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Optional sentence shown in hints and objective tooltips, for example 'Red books'.")]
        string playerFacingDescription;

        public string PlayerFacingDescription => string.IsNullOrEmpty(playerFacingDescription) ? Describe() : playerFacingDescription;

        public abstract bool Matches(TidyObject tidyObject);

        public virtual string Describe()
        {
            return name;
        }

        public void AssignDescription(string description)
        {
            playerFacingDescription = description;
        }
    }
}
