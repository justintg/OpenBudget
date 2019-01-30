﻿using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Serialization;
using OpenBudget.Model.SQLite.Serialization;
using OpenBudget.Model.SQLite.Tables;
using OpenBudget.Model.Util;
using SQLite;
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
        SQLiteConnection _db;

        internal SQLiteEventStore(SQLiteConnection connection)
        {
            _db = connection;
        }

        public IEnumerable<ModelEvent> GetEvents()
        {
            var evts = _db.Table<SQLiteEvent>().ToList();
            foreach (var evt in evts)
            {
                yield return ConvertToEvent(evt);
            }
        }

        public VectorClock GetMaxVectorClock()
        {
            var vectorClock = _db.Table<Info>().Where(i => i.Key == "MaxVectorClock").SingleOrDefault();
            if (vectorClock == null)
                return null;
            else
                return new VectorClock(vectorClock.Data);
        }

        public HashSet<Guid> GetStoredEventIDSet()
        {
            var ids = _db.Query<SQLiteEvent>(@"select EventID from SQLiteEvent").Select(e => e.EventID).ToList();
            return new HashSet<Guid>(ids);
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
            string sql =
                @"select * from SQLiteEvent
                where SQLiteEvent.EventID
                in
                (";

            StringBuilder sb = new StringBuilder(sql);

            foreach (var id in ids)
            {
                sb.Append($"\"{id.ToString()}\",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(") order by rowid asc");

            var events = _db.Query<SQLiteEvent>(sb.ToString()).ToList();

            return events;
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
            var currentClock = _db.Table<Info>().Where(i => i.Key == "MaxVectorClock").SingleOrDefault();
            if (currentClock == null)
            {
                var vcInfo = new Info() { Key = "MaxVectorClock", Data = vectorClock.ToByteArray() };
                _db.Insert(vcInfo);
            }
            else
            {
                currentClock.Data = vectorClock.ToByteArray();
                _db.Update(currentClock);
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
            _db.RunInTransaction(() =>
            {
                foreach (var evt in events)
                {

                    SQLiteEvent sqlEvent = ConvertToStore(evt);
                    _db.Insert(sqlEvent);
                }
            });
        }

        public VectorClock GetMaxVectorForEntity(string entityType, string entityId)
        {
            var maxEvent = _db.Query<SQLiteEvent>(
                @"select VectorClock from SQLiteEvent
                where EntityType = ? and EntityID = ?
                and IsIgnored = 0
                order by rowid desc
                limit 1
                ", entityType, entityId).Single();


            return new VectorClock(maxEvent.VectorClock);
        }

        public IEnumerable<ModelEvent> GetEntityEvents(string entityType, string entityId)
        {
            var evts = _db.Query<SQLiteEvent>(
                @"select * from SQLiteEvent
                where EntityType = ? and EntityID = ?
                and IsIgnored = 0
                order by rowid asc",
                entityType, entityId);

            foreach (var evt in evts)
            {
                yield return ConvertToEvent(evt);
            }
        }

        public IEnumerable<EventVector> GetEntityEventVectors(string entityType, string entityId)
        {
            return _db.Query<SQLiteEvent>(@"select EventID, VectorClock from SQLiteEvent
                where EntityType = ? and EntityID = ?
                order by rowid asc", entityType, entityId).Select(
                e =>
                new EventVector(
                    e.EventID,
                    new VectorClock(e.VectorClock)));
        }

        public void IgnoreEvents(IEnumerable<Guid> eventIds)
        {
            _db.RunInTransaction(() =>
            {
                foreach (var id in eventIds)
                {
                    _db.Execute("update SQLiteEvent set IsIgnored = ? where EventID = ?", true, id.ToString());
                }
            });
        }
    }
}
