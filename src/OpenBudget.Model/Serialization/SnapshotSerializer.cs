using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenBudget.Model.Serialization
{
    public class SnapshotSerializer
    {
        JsonSerializer _serializer;

        public SnapshotSerializer()
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver = new CustomResolver()
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
    }
}
