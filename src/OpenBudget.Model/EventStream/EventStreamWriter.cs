using OpenBudget.Model.Events;
using OpenBudget.Model.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenBudget.Model.Serialization;

namespace OpenBudget.Model.EventStream
{
    public class EventStreamWriter : IDisposable
    {
        /// <summary>
        /// The underlying stream passed into the constructor.
        /// </summary>
        private Stream _stream;
        private EventStreamHeader _header;
        private JsonSerializer _serializer;
        private StreamWriter _streamWriter;
        private JsonTextWriter _jsonWriter;
        private bool _wroteHeader = false;
        private bool _disposeStream = false;

        public EventStreamWriter(Stream stream, EventStreamHeader header, bool disposeStream)
        {
            _stream = stream;
            _header = header;

            _serializer = new Serializer().GetJsonSerializer();
            _streamWriter = new StreamWriter(_stream, Encoding.UTF8);
            _jsonWriter = new JsonTextWriter(_streamWriter);
            _disposeStream = disposeStream;
        }

        public void WriteEvent(ModelEvent evt)
        {
            if (!_wroteHeader)
            {
                _jsonWriter.WriteStartObject();
                _jsonWriter.WritePropertyName("Header");
                _serializer.Serialize(_jsonWriter, _header);
                _jsonWriter.WritePropertyName("Events");
                _jsonWriter.WriteStartArray();
                //_jsonWriter.WriteStartConstructor("Header");
                _wroteHeader = true;
            }

            _serializer.Serialize(_jsonWriter, evt, typeof(ModelEvent));
        }

        public void WriteEvents(IEnumerable<ModelEvent> events)
        {
            foreach (var evt in events)
            {
                WriteEvent(evt);
            }
        }

        public void Dispose()
        {
            if (_wroteHeader)
            {
                _jsonWriter.WriteEndArray();
                _jsonWriter.WriteEndObject();
            }
            _jsonWriter.Flush();
            _streamWriter.Flush();

            if (_disposeStream)
                _stream.Dispose();
        }
    }
}
