using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Event;
using System.IO;

namespace OpenBudget.Model.EventStream
{
    public class FileEventStream : IEventStream
    {
        private string _filePath;

        public FileEventStream(string filePath)
        {
            _filePath = filePath;

            using (var fileStream = File.OpenRead(_filePath))
            using (var reader = new EventStreamReader(fileStream))
            {
                if (reader.Read())
                {
                    Header = reader.HeaderValue;
                }
                else
                {
                    throw new EventStreamException("Invalid event stream");
                }
            }
        }

        public EventStreamHeader Header { get; private set; }

        public IEnumerable<ModelEvent> CreateEventIterator()
        {
            using (var fileStream = File.OpenRead(_filePath))
            using (var reader = new EventStreamReader(fileStream))
            {
                reader.Read();//Header
                while (reader.Read())
                {
                    yield return reader.CurrentEvent;
                }
            }
        }
    }
}
