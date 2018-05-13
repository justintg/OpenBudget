using OpenBudget.Model.Event;
using OpenBudget.Model.EventStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Synchronization
{
    public interface ISynchronizationService
    {
        IEnumerable<IEventStream> GetEventStreams();

        void PublishEvents(IEnumerable<ModelEvent> events);

        HashSet<Guid> GetPublishedEventIDs();
    }

    public class FileDirectorySynchronizationService : ISynchronizationService
    {
        private string _rootDirectory;
        private string _deviceDirectory;
        private Guid _deviceId;
        private Dictionary<string, FileEventStream> _cachedEventStreams = new Dictionary<string, FileEventStream>();
        private Dictionary<string, List<Guid>> _eventsInStreams = new Dictionary<string, List<Guid>>();
        private HashSet<Guid> _publishedEvents = new HashSet<Guid>();


        public FileDirectorySynchronizationService(Guid deviceId, string rootDirectory)
        {
            _deviceId = deviceId;
            _rootDirectory = rootDirectory;
            _deviceDirectory = Path.Combine(rootDirectory, deviceId.ToString());

            Directory.CreateDirectory(Path.Combine(rootDirectory, deviceId.ToString()));
            UpdateEventCache();
        }

        private void UpdateEventCache()
        {
            foreach (var directory in Directory.EnumerateDirectories(_rootDirectory))
            {
                foreach (var eventFile in Directory.EnumerateFiles(directory, "*.eventstream"))
                {
                    if (!_cachedEventStreams.ContainsKey(eventFile))
                    {
                        FileEventStream stream = new FileEventStream(eventFile);
                        _cachedEventStreams.Add(eventFile, stream);

                        var streamEventIDs = stream.CreateEventIterator().Select(e => e.EventID).ToList();
                        foreach (var eventId in streamEventIDs)
                        {
                            if (!_publishedEvents.Add(eventId))
                            {
                                throw new InvalidOperationException("This eventID already exists!");
                            }
                        }
                        _eventsInStreams.Add(eventFile, streamEventIDs);
                    }
                }
            }
        }

        public IEnumerable<IEventStream> GetEventStreams()
        {
            UpdateEventCache();

            foreach (var stream in _cachedEventStreams)
            {
                yield return stream.Value;
            }
        }

        public void PublishEvents(IEnumerable<ModelEvent> events)
        {
            UpdateEventCache();

            var eventsToPublish = events.Where(e => !_publishedEvents.Contains(e.EventID)).OrderBy(e => e.EventVector).ToList();
            if (eventsToPublish.Count == 0)
                return;

            var streamId = Guid.NewGuid();
            EventStreamHeader header = new EventStreamHeader(eventsToPublish[0].EventVector, eventsToPublish.Last().EventVector, _deviceId, streamId);

            string filePath = Path.Combine(_deviceDirectory, streamId.ToString() + ".eventstream");
            using (Stream file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            using (EventStreamWriter writer = new EventStreamWriter(file, header, true))
            {
                writer.WriteEvents(eventsToPublish);
            }

            FileEventStream stream = new FileEventStream(filePath);
            _cachedEventStreams.Add(filePath, stream);

            var streamEventIDs = eventsToPublish.Select(e => e.EventID).ToList();
            foreach (var eventId in streamEventIDs)
            {
                if (!_publishedEvents.Add(eventId))
                {
                    throw new InvalidOperationException("This eventID already exists!");
                }
            }
            _eventsInStreams.Add(filePath, streamEventIDs);
        }

        public HashSet<Guid> GetPublishedEventIDs()
        {
            UpdateEventCache();
            return new HashSet<Guid>(_publishedEvents);
        }
    }
}
