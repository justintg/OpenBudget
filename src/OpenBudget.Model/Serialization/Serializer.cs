using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Text;

namespace OpenBudget.Model.Serialization
{
    public class Serializer
    {
        JsonSerializer _serializer;

        public Serializer(IContractResolver contractResolver)
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Formatting = Formatting.None,
                ContractResolver = contractResolver,
                SerializationBinder = new EventTypeBinder()
            };

            _serializer = JsonSerializer.CreateDefault(settings);
        }

        public Serializer()
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver = new CustomResolver(),
                Formatting = Formatting.None,
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
}
