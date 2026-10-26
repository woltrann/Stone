using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Exact Object Rule", fileName = "Rule_Exact", order = 102)]
    public sealed class TidyDefinitionRule : TidyRuleDefinition
    {
        [SerializeField, Tooltip("The object matches when its definition is one of these.")]
        List<TidyObjectDefinition> definitions = new List<TidyObjectDefinition>();

        public override bool Matches(TidyObject tidyObject)
        {
            return tidyObject != null && tidyObject.Definition != null && definitions.Contains(tidyObject.Definition);
        }

        public override string Describe()
        {
            return string.Join(", ", definitions.Where(definition => definition != null).Select(definition => definition.DisplayName));
        }

        public void Configure(IEnumerable<TidyObjectDefinition> newDefinitions)
        {
            definitions = new List<TidyObjectDefinition>(newDefinitions);
        }
    }
}
