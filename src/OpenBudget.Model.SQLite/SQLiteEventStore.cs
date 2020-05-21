using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Serialization;
using OpenBudget.Model.SQLite.Serialization;
using OpenBudget.Model.SQLite.Tables;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class SQLiteEventStore : IEventStore
    {
        Serializer _serializer = new Serializer(new SQLiteContractResolver());
        string _connectionString;

        internal SQLiteEventStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqliteContext GetContext()
        {
            return new SqliteContext(_connectionString);
        }

        public IEnumerable<ModelEvent> GetEvents()
        {
            using (var context = GetContext())
            {
                var evts = context.Events.ToList();
                foreach (var evt in evts)
                {
                    yield return ConvertToEvent(evt);
                }
            }
        }

        public VectorClock GetMaxVectorClock()
        {
            using (var context = GetContext())
            {
                var vectorClock = context.Info.SingleOrDefault(i => i.Key == "MaxVectorClock");
                if (vectorClock == null)
                    return null;
                else
                    return new VectorClock(vectorClock.Data);
            }
        }

        public HashSet<Guid> GetStoredEventIDSet()
        {
            using (var context = GetContext())
            {
                var ids = context.Events.Select(e => e.EventID).ToList();
                return new HashSet<Guid>(ids);
            }
        }

        public IEnumerable<ModelEvent> GetUnpublishedEvents(HashSet<Guid> publishedEventSet)
        {
            var storedEvents = GetStoredEventIDSet();
            List<Guid> unpublishedEvents = storedEvents.Where(id => !publishedEventSet.Contains(id)).ToList();
            if (unpublishedEvents.Count == 0)
            {
                yield break;
            }

            var sqlEvents = GetManyEvents(unpublishedEvents);
            foreach (var sqlEvent in sqlEvents)
            {
                yield return ConvertToEvent(sqlEvent);
            }
        }

        private List<SQLiteEvent> GetManyEvents(IEnumerable<Guid> ids)
        {
            using (var context = GetContext())
            {
                return context.Events.Where(e => ids.Contains(e.EventID)).ToList();
            }
        }

        public void MergeEventStreamClock(IEventStream eventStream)
        {
            var internalVector = GetMaxVectorClock();
            if (internalVector == null)
            {
                SetMaxVectorClock(eventStream.Header.EndVectorClock);
            }
            else
            {
                SetMaxVectorClock(internalVector.Merge(eventStream.Header.EndVectorClock));
            }
        }

        public void SetMaxVectorClock(VectorClock vectorClock)
        {
            using (var context = GetContext())
            {
                var currentClock = context.Info.SingleOrDefault(i => i.Key == "MaxVectorClock");
                if (currentClock == null)
                {
                    var vcInfo = new Info() { Key = "MaxVectorClock", Data = vectorClock.ToByteArray() };
                    context.Info.Add(vcInfo);
                }
                else
                {
                    currentClock.Data = vectorClock.ToByteArray();
                    context.Update(currentClock);
                }
                context.SaveChanges();
            }
        }

        private PropertyAccessorSet<ModelEvent> _modelEventProperties = new PropertyAccessorSet<ModelEvent>(true);

        private ModelEvent ConvertToEvent(SQLiteEvent evt)
        {
            var modelEvent = _serializer.DeserializeFromBytes<ModelEvent>(evt.EventData);
            _modelEventProperties.SetEntityData<Guid>(modelEvent, evt.EventID, nameof(ModelEvent.EventID));
            _modelEventProperties.SetEntityData<string>(modelEvent, evt.EntityType, nameof(ModelEvent.EntityType));
            _modelEventProperties.SetEntityData<string>(modelEvent, evt.EntityID, nameof(ModelEvent.EntityID));
            _modelEventProperties.SetEntityData<VectorClock>(modelEvent, new VectorClock(evt.VectorClock), nameof(ModelEvent.EventVector));
            _modelEventProperties.SetEntityData<Guid>(modelEvent, evt.DeviceID, nameof(ModelEvent.DeviceID));
            return modelEvent;
        }

        private SQLiteEvent ConvertToStore(ModelEvent evt)
        {
            SQLiteEvent sqlEvent = new SQLiteEvent();
            sqlEvent.EventID = evt.EventID;
            sqlEvent.EntityType = evt.EntityType;
            sqlEvent.EntityID = evt.EntityID;
            sqlEvent.DeviceID = evt.DeviceID;
            sqlEvent.VectorClock = evt.EventVector.ToByteArray();
            sqlEvent.EventData = _serializer.SerializeToBytes(evt, typeof(ModelEvent));

            return sqlEvent;
        }

        public void StoreEvents(IEnumerable<ModelEvent> events)
        {
            using (var context = GetContext())
            {
                foreach (var evt in events)
                {

                    SQLiteEvent sqlEvent = ConvertToStore(evt);
                    context.Add(sqlEvent);
                }
                context.SaveChanges();
            }
        }

        public VectorClock GetMaxVectorForEntity(string entityType, string entityId)
        {
            using (var context = GetContext())
            {

                var maxEvent = context.Events.FromSqlRaw(
                    @"select VectorClock from Events
                where EntityType = {0} and EntityID = {1}
                and IsIgnored = 0
                order by rowid desc
                limit 1
                ", entityType, entityId).Single();

                return new VectorClock(maxEvent.VectorClock);
            }
        }

        public IEnumerable<ModelEvent> GetEntityEvents(string entityType, string entityId)
        {
            using (var context = GetContext())
            {
                var evts = context.Events.FromSqlRaw(
                    @"select * from Events
                where EntityType = {0} and EntityID = {1}
                and IsIgnored = 0
                order by rowid asc",
                    entityType, entityId);

                foreach (var evt in evts)
                {
                    yield return ConvertToEvent(evt);
                }
            }
        }

        public IEnumerable<EventVector> GetEntityEventVectors(string entityType, string entityId)
        {
            using (var context = GetContext())
            {
                return context.Events.FromSqlRaw(@"select EventID, VectorClock from Events
                where EntityType = {0} and EntityID = {1}
                order by rowid asc", entityType, entityId).Select(
                    e =>
                    new EventVector(
                        e.EventID,
                        new VectorClock(e.VectorClock))).ToList();
            }
        }

        public void IgnoreEvents(IEnumerable<Guid> eventIds)
        {
            using (var context = GetContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                foreach (var id in eventIds)
                {
                    context.Database.ExecuteSqlRaw("update Events set IsIgnored = ? where EventID = ?", true, id.ToString());
                }
                transaction.Commit();
            }
        }
    }
}
