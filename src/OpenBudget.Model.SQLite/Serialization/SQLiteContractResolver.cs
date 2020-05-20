using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenBudget.Model.Events;
using OpenBudget.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

            var deviceId = properties.Single(p => p.PropertyName == "EventID");
            deviceId.DefaultValueHandling = DefaultValueHandling.Populate;
            deviceId.DefaultValue = default(Guid);
            deviceId.ShouldSerialize = o => false;
            deviceId.ShouldDeserialize = o => true;

            string[] ignoredProperties = new[] { "EntityType", "EntityID", "DeviceID", "EventVector" };
            foreach (var prop in properties.Where(p => ignoredProperties.Contains(p.PropertyName)))
            {
                prop.ShouldSerialize = o => false;
                prop.ShouldDeserialize = o => true;
            }

            if (typeof(GroupedFieldChangeEvent).IsAssignableFrom(type))
            {
                var groupedEvents = properties.Where(p => p.PropertyName == "_groupedEvents").Single();
                groupedEvents.Converter = new GroupedEventsConverter();
            }

            return properties;
        }

        public class GroupedEventsConverter : JsonConverter
        {
            JsonSerializer _defaultSerializer = null;
            public GroupedEventsConverter()
            {
                _defaultSerializer = new Serializer().GetJsonSerializer();
            }

            public override bool CanRead => true;

            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return _defaultSerializer.Deserialize(reader, objectType);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                _defaultSerializer.Serialize(writer, value);
            }
        }
    }
}
