using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBudget.Model.Serialization
{
    public class CustomResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            // if type implements your interface - serialize it as object
            if (typeof(VectorClock).IsAssignableFrom(objectType))
            {
                return base.CreateObjectContract(objectType);
            }

            return base.CreateContract(objectType);
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            if (typeof(FieldChange).IsAssignableFrom(type))
            {
                var typedNewValue = properties.FirstOrDefault(p => p.PropertyName == "TypedNewValue");
                if (typedNewValue != null)
                {
                    typedNewValue.PropertyName = "V";
                }
            }

            return properties;
        }
    }
}
