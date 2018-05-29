using Newtonsoft.Json.Serialization;
using OpenBudget.Model.Infrastructure;
using System;

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
    }
}
