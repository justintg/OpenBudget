using OpenBudget.Model.Event;
using OpenBudget.Model.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenBudget.Model.Serialization;

namespace OpenBudget.Model.EventStream
{
    public enum ItemType
    {
        None = 0,
        Header,
        Event
    }
    public class EventStreamReader : IDisposable
    {
        private Stream _stream;
        private JsonSerializer _serializer;
        private StreamReader _streamReader;
        private JsonTextReader _jsonReader;
        private bool _eventArrayFound = false;
        private bool _streamEnded = false;

        public ItemType ItemType { get; private set; }
        public EventStreamHeader HeaderValue { get; private set; }
        public ModelEvent CurrentEvent { get; private set; }

        public EventStreamReader(Stream stream)
        {
            _stream = stream;

            _serializer = new Serializer().GetJsonSerializer();
            _streamReader = new StreamReader(_stream);
            _jsonReader = new JsonTextReader(_streamReader);
        }

        public bool Read()
        {
            if (HeaderValue == null)
            {
                return ReadHeader();
            }
            else
            {
                return ReadEvent();
            }
        }

        private bool ReadEvent()
        {
            if (_streamEnded)
            {
                return false;
            }

            if (!_eventArrayFound)
            {
                _jsonReader.Read();
                _jsonReader.Read();
                if (_jsonReader.TokenType != JsonToken.StartArray || _jsonReader.Path != "Events")
                    throw new EventStreamException("Invalid Event Stream, no event array found!");

                _eventArrayFound = true;
            }

            _jsonReader.Read();
            if (_jsonReader.TokenType == JsonToken.EndArray && _jsonReader.Path == "Events")
            {
                _jsonReader.Read();
                bool endDocument = _jsonReader.Read();
                if (endDocument == true)
                    throw new EventStreamException("Invalid Event Stream, unrecognized data in stream!");

                ItemType = ItemType.None;
                CurrentEvent = null;
                _streamEnded = true;
                return false;
            }
            ItemType = ItemType.Event;
            CurrentEvent = _serializer.Deserialize<ModelEvent>(_jsonReader);

            return true;
        }

        private bool ReadHeader()
        {
            _jsonReader.Read();
            _jsonReader.Read();
            _jsonReader.Read();
            if (_jsonReader.TokenType != JsonToken.StartObject || _jsonReader.Path != "Header")
                throw new EventStreamException("Invalid Event Stream, no header was found!");

            ItemType = ItemType.Header;
            HeaderValue = _serializer.Deserialize<EventStreamHeader>(_jsonReader);

            return true;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
