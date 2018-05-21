using OpenBudget.Model.Entities;
using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Util
{
    public class Serializer
    {
        JsonSerializer _serializer;

        public Serializer()
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver = new CustomResolver(),
                SerializationBinder = new EventTypeBinder()
            };

            _serializer = JsonSerializer.CreateDefault(settings);
        }

        public string SerializeToString(object obj, Type objectType = null)
        {
            using (StringWriter sw = new StringWriter())
            {
                if (objectType == null)
                    _serializer.Serialize(sw, obj);
                else
                    _serializer.Serialize(sw, obj, objectType);
                return sw.ToString();
            }
        }

        public T DeserializeFromString<T>(string data)
        {
            using (StringReader sr = new StringReader(data))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                return _serializer.Deserialize<T>(reader);
            }
        }

        public byte[] SerializeToBytes(object obj, Type objectType = null)
        {
            MemoryStream baseStream = new MemoryStream();

            using (StreamWriter sw = new StreamWriter(baseStream, Encoding.UTF8))
            using (JsonTextWriter jw = new JsonTextWriter(sw))
            {
                if (objectType == null)
                {
                    _serializer.Serialize(jw, obj);
                }
                else
                {
                    _serializer.Serialize(jw, obj, objectType);
                }
            }

            return baseStream.ToArray();
        }

        public T DeserializeFromBytes<T>(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
            using (JsonTextReader jr = new JsonTextReader(sr))
            {
                return _serializer.Deserialize<T>(jr);
            }
        }

        public JsonSerializer GetJsonSerializer() => _serializer;
    }

    public class EventTypeBinder : ISerializationBinder
    {
        private Dictionary<int, Type> eventMapping = new Dictionary<int, Type>
        {
            [0] = typeof(EntityCreatedEvent),
            [1] = typeof(EntityUpdatedEvent),
            [2] = typeof(EntityReference),
            [3] = typeof(GroupedFieldChangeEvent),
            [4] = typeof(ConflictResolutionEvent),
            [50] = typeof(TypedFieldChange<string>),
            [51] = typeof(TypedFieldChange<EntityReference>),
            [52] = typeof(TypedFieldChange<decimal>),
            [53] = typeof(TypedFieldChange<DateTime>),
            [54] = typeof(TypedFieldChange<AccountBudgetTypes>),
            [55] = typeof(TypedFieldChange<TransactionTypes>),
            [56] = typeof(TypedFieldChange<bool>)
        };

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var mapping = eventMapping.Where(kvp => kvp.Value == serializedType).Single();
            assemblyName = null;
            typeName = mapping.Key.ToString();
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            int index = int.Parse(typeName);
            return eventMapping[index];
        }
    }

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
