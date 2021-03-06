using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class ParameterSymbol : BaseValueSymbol
    {
        internal const string TypeName = "parameter";

        // Used when the template explicitly defines the symbol "name".
        // The template definition is used exclusively, except for the case where it doesn't define any value forms.
        // When that is the case, the default value forms are used.
        public static ParameterSymbol ExplicitNameSymbolMergeWithDefaults(ISymbolModel templateDefinedName, ISymbolModel defaultDefinedName)
        {
            if (!(templateDefinedName is ParameterSymbol templateSymbol))
            {
                throw new InvalidCastException("templateDefinedName is not a ParameterSymbol");
            }

            if (!(defaultDefinedName is ParameterSymbol defaultSymbol))
            {
                throw new InvalidCastException("defaultDefinedName is not a ParameterSymbol");
            }

            if (templateSymbol.Forms.GlobalForms.Count > 0)
            {   // template symbol has forms, use them
                return templateSymbol;
            }

            ParameterSymbol mergedSymbol = new ParameterSymbol()
            {
                Binding = templateSymbol.Binding,
                DefaultValue = templateSymbol.DefaultValue,
                Description = templateSymbol.Description,
                Forms = defaultSymbol.Forms,    // this is the only thing that gets replaced from the default
                IsRequired = templateSymbol.IsRequired,
                Type = templateSymbol.Type,
                Replaces = templateSymbol.Replaces,
                DataType = templateSymbol.DataType,
                FileRename = templateSymbol.FileRename,
                IsTag = templateSymbol.IsTag,
                TagName = templateSymbol.TagName,
                Choices = templateSymbol.Choices,
                ReplacementContexts = templateSymbol.ReplacementContexts,
            };

            return mergedSymbol;
        }

        // only relevant for choice datatype
        public bool IsTag { get; set; }

        // only relevant for choice datatype
        public string TagName { get; set; }

        private IReadOnlyDictionary<string, string> _choices;

        public IReadOnlyDictionary<string, string> Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                _choices = value.CloneIfDifferentComparer(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static ISymbolModel FromJObject(JObject jObject, IParameterSymbolLocalizationModel localization, string defaultOverride)
        {
            ParameterSymbol symbol = FromJObject<ParameterSymbol>(jObject, localization, defaultOverride);
            Dictionary<string, string> choicesAndDescriptions = new Dictionary<string, string>();

            if (symbol.DataType == "choice")
            {
                symbol.IsTag = jObject.ToBool(nameof(IsTag), true);
                symbol.TagName = jObject.ToString(nameof(TagName));

                foreach (JObject choiceObject in jObject.Items<JObject>(nameof(Choices)))
                {
                    string choice = choiceObject.ToString("choice");

                    if (localization == null
                        || ! localization.ChoicesAndDescriptions.TryGetValue(choice, out string choiceDescription))
                    {
                        choiceDescription = choiceObject.ToString("description");
                    }
                    choicesAndDescriptions.Add(choice, choiceDescription ?? string.Empty);
                }
            }

            symbol.Choices = choicesAndDescriptions;

            return symbol;
        }

        public static ISymbolModel FromDeprecatedConfigTag(string value)
        {
            ParameterSymbol symbol = new ParameterSymbol
            {
                DefaultValue = value,
                Type = TypeName,
                DataType = "choice",
                IsTag = true,
                Choices = new Dictionary<string, string>() { { value, string.Empty } },
            };

            return symbol;
        }
    }
}
