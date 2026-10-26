using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    public enum TidyNumericProperty
    {
        Size,
        Weight,
        Priority,
        SequenceIndex
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Numeric Range Rule", fileName = "Rule_Range", order = 103)]
    public sealed class TidyNumericRule : TidyRuleDefinition
    {
        [SerializeField]
        TidyNumericProperty property = TidyNumericProperty.Size;

        [SerializeField]
        float minimum;

        [SerializeField]
        float maximum = 1f;

        public override bool Matches(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                return false;
            }

            var value = Read(tidyObject);
            return value >= minimum && value <= maximum;
        }

        public override string Describe()
        {
            return property + " " + minimum + "-" + maximum;
        }

        public void Configure(TidyNumericProperty newProperty, float newMinimum, float newMaximum)
        {
            property = newProperty;
            minimum = newMinimum;
            maximum = newMaximum;
        }

        float Read(TidyObject tidyObject)
        {
            switch (property)
            {
                case TidyNumericProperty.Weight:
                    return tidyObject.Weight;
                case TidyNumericProperty.Priority:
                    return tidyObject.Priority;
                case TidyNumericProperty.SequenceIndex:
                    return tidyObject.SequenceIndex;
                default:
                    return tidyObject.Size;
            }
        }
    }
}
