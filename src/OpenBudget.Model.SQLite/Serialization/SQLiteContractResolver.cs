using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenBudget.Model.Events;
using OpenBudget.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.SQLite.Serialization
{
    public class SQLiteContractResolver : CustomResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!typeof(ModelEvent).IsAssignableFrom(type))
            {
                return base.CreateProperties(type, memberSerialization);
            }

            var properties = base.CreateProperties(type, memberSerialization);

            string[] ignoredProperties = new[] { "EventID", "EntityType", "EntityID", "DeviceID", "EventVector" };
            foreach(var prop in properties.Where(p => ignoredProperties.Contains(p.PropertyName)))
            {
                prop.Ignored = true;
            }

            return properties;
        }
    }
}
