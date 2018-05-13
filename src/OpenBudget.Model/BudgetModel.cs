using OpenBudget.Model.Entities;
using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Event;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Synchronization;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenBudget.Model.Tests")]

namespace OpenBudget.Model
{
    public class BudgetModel
    {
        public Budget Budget { get; private set; }
        public Guid DeviceID { get; private set; }
        internal BudgetMessageBus InternalMessageBus { get; private set; }
        public BudgetMessageBus MessageBus { get; private set; }
        public IEventStore EventStore { get; private set; }
        internal EntityGenerator<Budget> BudgetGenerator { get; private set; }
        internal EntityGenerator<Account> AccountGenerator { get; private set; }
        internal EntityGenerator<Transaction> TransactionGenerator { get; private set; }
        internal EntityGenerator<SubTransaction> SubTransactionGenerator { get; private set; }
        internal EntityGenerator<BudgetCategory> BudgetCategoryGenerator { get; private set; }
        internal EntityGenerator<BudgetSubCategory> BudgetSubCategoryGenerator { get; private set; }
        internal EntityGenerator<Payee> PayeeGenerator { get; private set; }
        internal IncomeCategoryGenerator IncomeCategoryGenerator { get; private set; }
        internal BudgetCategoryMonthGenerator BudgetCategoryMonthGenerator { get; private set; }

        private Dictionary<Type, object> _generators = new Dictionary<Type, object>();

        private ISynchronizationService _syncService;

        internal EntityGenerator<T> FindGenerator<T>() where T : EntityBase
        {
            return (EntityGenerator<T>)_generators[typeof(T)];
        }

        public T FindEntity<T>(string EntityID) where T : EntityBase
        {
            IIdentityMap map = FindGenerator<T>() as IIdentityMap;
            return (T)map.GetEntity(EntityID);
        }

        public EntityBase FindEntity(string EntityType, string EntityID)
        {
            var generator = _generators.Where(kvp => kvp.Key.Name == EntityType).Single().Value as IIdentityMap;
            return generator.GetEntity(EntityID);
        }

        protected BudgetModel(Guid deviceId, IEventStore eventStore) : this(deviceId, eventStore, true)
        {

        }

        protected BudgetModel(Guid deviceId, IEventStore eventStore, Budget initialBudget) : this(deviceId, eventStore, false)
        {
            if (initialBudget == null) throw new ArgumentNullException(nameof(initialBudget));

            Budget = initialBudget;
            initialBudget.AttachToModel(this);
        }

        protected BudgetModel(Guid deviceId, IEventStore eventStore, bool createBudget)
        {
            DeviceID = deviceId;
            EventStore = eventStore;
            InternalMessageBus = new BudgetMessageBus();
            MessageBus = new BudgetMessageBus();

            BudgetGenerator = RegisterGenerator(new EntityGenerator<Budget>(this));
            AccountGenerator = RegisterGenerator(new EntityGenerator<Account>(this));
            TransactionGenerator = RegisterGenerator(new EntityGenerator<Transaction>(this));
            SubTransactionGenerator = RegisterGenerator(new EntityGenerator<SubTransaction>(this));
            BudgetCategoryGenerator = RegisterGenerator(new EntityGenerator<BudgetCategory>(this));
            BudgetSubCategoryGenerator = RegisterGenerator(new EntityGenerator<BudgetSubCategory>(this));
            PayeeGenerator = RegisterGenerator(new EntityGenerator<Payee>(this));
            IncomeCategoryGenerator = (IncomeCategoryGenerator)RegisterGenerator(new IncomeCategoryGenerator(this));
            BudgetCategoryMonthGenerator = (BudgetCategoryMonthGenerator)RegisterGenerator(new BudgetCategoryMonthGenerator(this));

            if (createBudget)
            {
                Budget = new Budget();
                Budget.AttachToModel(this);
            }
        }

        public void setSynchronizationService(ISynchronizationService syncService)
        {
            _syncService = syncService;
        }

        internal void EnsureIdentityTracked(EntityBase entity)
        {
            Type entityType = entity.GetType();
            IIdentityMap generator = _generators[entityType] as IIdentityMap;
            generator.EnsureIdentityTracked(entity);
        }

        public static BudgetModel OpenExistingOnNewDevice(Guid deviceId, ISynchronizationService syncService, IEventStore eventStore)
        {
            //var budgetId = events.Single(e => e.EntityType == "Budget" && e is EntityCreatedEvent).EntityID;
            BudgetModel model = new BudgetModel(deviceId, eventStore, false);
            model.setSynchronizationService(syncService);
            model.Sync();

            model.Budget = (Budget)model.BudgetGenerator.GetAll().Single();
            model.Budget.AttachToModel(model);
            return model;
        }

        public static BudgetModel Load(Guid deviceId, IEventStore eventStore)
        {
            var events = eventStore.GetEvents();
            var budgetId = events.Single(e => e.EntityType == "Budget" && e is EntityCreatedEvent).EntityID;
            BudgetModel model = new BudgetModel(deviceId, eventStore, false);
            model.EventStore = eventStore;
            foreach (var evt in events)
            {
                model.InternalMessageBus.PublishEvent(evt.EntityType, evt);
            }
            model.Budget = (Budget)model.BudgetGenerator.GetAll().Single();
            model.Budget.AttachToModel(model);
            return model;
        }

        public static BudgetModel CreateNew(Guid deviceId, IEventStore eventStore)
        {
            return new BudgetModel(deviceId, eventStore);
        }

        public static BudgetModel CreateNew(Guid deviceId, IEventStore eventStore, Budget initialBudget)
        {
            if (initialBudget == null) throw new ArgumentNullException(nameof(initialBudget));

            return new BudgetModel(deviceId, eventStore, initialBudget);
        }

        public void Sync()
        {
            if (_syncService == null)
                return;

            _syncService.PublishEvents(EventStore.GetUnpublishedEvents(_syncService.GetPublishedEventIDs()));

            var streams = _syncService.GetEventStreams();

            MergeEventStreams(streams);
        }

        private HashSet<Guid> _mergedEventStreams = new HashSet<Guid>();

        private void MergeEventStreams(IEnumerable<IEventStream> eventStreams)
        {
            VectorClock currentMaxVector = this.EventStore.GetMaxVectorClock();

            List<IEventStream> eventStreamsToConsider = eventStreams.Where(es => !_mergedEventStreams.Contains(es.Header.EventStreamID) && es.Header.DeviceID != this.DeviceID).ToList();
            if (eventStreamsToConsider.Count == 0)
                return;

            HashSet<Guid> oldEventIds = this.EventStore.GetStoredEventIDSet();
            List<ModelEvent> newEvents = eventStreamsToConsider.SelectMany(s => s.CreateEventIterator()).Where(e => !oldEventIds.Contains(e.EventID)).OrderBy(e => e.EventVector).ToList();


            bool haveConflicts = this.StreamsHaveConflicts(currentMaxVector, eventStreamsToConsider);
            if (haveConflicts)
            {
                //Merge Resolution
                List<EntityConflictResolution> conflictResolutions = MergeEventsWithConflicts(newEvents, eventStreamsToConsider);
                MergeEventVectors(eventStreamsToConsider);
                this.SaveChangesInternal(conflictResolutions);
                this.Sync();
            }
            else
            {
                MergeEventsSimple(newEvents);
                MergeEventVectors(eventStreamsToConsider);
            }
        }

        private List<EntityConflictResolution> MergeEventsWithConflicts(List<ModelEvent> newEvents, IEnumerable<IEventStream> eventStreamsToConsider)
        {
            List<EntityConflictResolution> conflictResolutions = new List<EntityConflictResolution>();
            var eventsByEntity = newEvents.GroupBy(k => new Tuple<string, string>(k.EntityType, k.EntityID));
            foreach (var entity in eventsByEntity)
            {
                string entityType = entity.Key.Item1;
                string entityId = entity.Key.Item2;
                VectorClock currentInternalMaxVector = this.EventStore.GetMaxVectorForEntity(entityType, entityId);
                var deviceEvents = OrganizeEventsByDevice(entityType, entityId, entity);
                bool entityHasConflict = EntityHasConflict(entityType, entityId, currentInternalMaxVector, deviceEvents);
                if (entityHasConflict)
                {
                    List<ConflictResolutionEvent> resolutionEvents = ResolveConflict(entityType, entityId, deviceEvents, currentInternalMaxVector);
                    EntityConflictResolution conflictResolution = new EntityConflictResolution()
                    {
                        EntityID = entityId,
                        EntityType = entityType,
                        ConflictResolutionEvents = resolutionEvents
                    };
                    conflictResolutions.Add(conflictResolution);

                    this.EventStore.StoreEvents(entity.ToList());
                }
                else
                {
                    MergeEventsSimple(entity.ToList());
                }
            }

            return conflictResolutions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="entityId"></param>
        /// <param name="deviceEvents">A list of events for the entity segregated by device</param>
        /// <returns>True if there is a conflict, False if there is no conflict</returns>
        private bool EntityHasConflict(string entityType, string entityId, VectorClock internalMaxVector, List<DeviceEventContainer> deviceEvents)
        {
            foreach (var device in deviceEvents)
            {
                if (internalMaxVector.CompareVectors(device.MaxVectorClockIncludingResolutions) == VectorClock.ComparisonResult.Simultaneous)
                {
                    return true;
                }
            }
            return false;
        }

        private List<DeviceEventContainer> OrganizeEventsByDevice(string entityType, string entityId, IEnumerable<ModelEvent> newEvents)
        {
            List<DeviceEventContainer> containerList = new List<DeviceEventContainer>();
            var deviceGroups = newEvents.GroupBy(e => e.DeviceID);
            foreach (var device in deviceGroups)
            {
                DeviceEventContainer deviceContainer = new DeviceEventContainer();
                deviceContainer.EntityType = entityType;
                deviceContainer.EntityID = entityId;
                deviceContainer.DeviceID = device.Key;

                deviceContainer.ConflictResolutionEvents = device.Where(e => e is ConflictResolutionEvent).Select(e => (ConflictResolutionEvent)e).ToList();

                // ignoredEvents contains the IDs of all previously ignored events as well 
                HashSet<Guid> ignoredEvents = new HashSet<Guid>(deviceContainer.ConflictResolutionEvents.SelectMany(c => c.EventsToIgnore));

                var eventsWithoutResolution = device.Where(e => !(e is ConflictResolutionEvent));
                deviceContainer.DeviceEvents = eventsWithoutResolution.Where(e => !ignoredEvents.Contains(e.EventID)).OrderBy(e => e.EventVector).ToList();
                deviceContainer.PreviouslyIgnoredEvents = eventsWithoutResolution.Where(e => ignoredEvents.Contains(e.EventID)).OrderBy(e => e.EventVector).ToList();

                deviceContainer.MaxVectorClock = deviceContainer.DeviceEvents.OrderBy(e => e.EventVector).LastOrDefault()?.EventVector;
                deviceContainer.MaxVectorClockIncludingResolutions = device.OrderBy(e => e.EventVector).Last().EventVector;
                containerList.Add(deviceContainer);
            }

            return containerList;
        }

        private List<ConflictResolutionEvent> ResolveConflict(string entityType, string entityId, List<DeviceEventContainer> deviceEvents, VectorClock internalVector)
        {
            //First get a list of exisiting conflict resolutions
            List<ConflictResolutionEvent> resolutionEvents = new List<ConflictResolutionEvent>();
            resolutionEvents.AddRange(deviceEvents.SelectMany(e => e.ConflictResolutionEvents));

            List<Guid> eventsToIgnore = new List<Guid>();
            DeviceEventContainer deviceWithlastAction = null; //Null means the current device has the last action
            VectorClock mostRecentVector = internalVector;
            foreach (var device in deviceEvents)
            {
                if (device.MaxVectorClock == null && device.DeviceEvents.Count == 0)
                    continue; //All events were ignored and no new events to consider

                if (mostRecentVector.CompareTo(device.MaxVectorClock) == -1)
                {
                    deviceWithlastAction = device;
                    mostRecentVector = device.MaxVectorClock;
                }
                else
                {
                    eventsToIgnore.AddRange(device.DeviceEvents.Select(e => e.EventID));
                }
            }

            if (deviceWithlastAction != null)
            {
                //Ignore the current device events
                var vectors = EventStore.GetEntityEventVectors(entityType, entityId);
                foreach (var vector in vectors)
                {
                    if (vector.Vector.CompareVectors(deviceWithlastAction.MaxVectorClock) == VectorClock.ComparisonResult.Simultaneous)
                    {
                        eventsToIgnore.Add(vector.EventID);
                    }
                }
            }

            if (eventsToIgnore.Count > 0)
                resolutionEvents.Add(new ConflictResolutionEvent(entityType, entityId, eventsToIgnore));

            return resolutionEvents;
        }

        private void MergeEventsSimple(List<ModelEvent> newEvents)
        {
            List<ConflictResolutionEvent> conflictResolutions = newEvents.Where(e => e is ConflictResolutionEvent).Select(e => (ConflictResolutionEvent)e).ToList();
            this.EventStore.StoreEvents(newEvents);

            foreach (var conflictResolution in conflictResolutions)
            {
                this.EventStore.IgnoreEvents(conflictResolution.EventsToIgnore);
                this.EventStore.IgnoreEvents(conflictResolution.EventID.Yield());
            }

            var entitiesToRebuild = conflictResolutions.Select(e => new Tuple<string, string>(e.EntityType, e.EntityID)).Distinct().ToList();


            foreach (ModelEvent newEvent in newEvents)
            {
                var eventEntity = new Tuple<string, string>(newEvent.EntityType, newEvent.EntityID);
                if (!entitiesToRebuild.Contains(eventEntity))
                    this.InternalMessageBus.PublishEvent(newEvent.EntityType, newEvent);

                this.MessageBus.PublishEvent(newEvent.EntityType, newEvent);
            }

            foreach (var entity in entitiesToRebuild)
            {
                this.RebuildEntity(entity.Item1, entity.Item2);
            }

        }

        private void MergeEventVectors(IEnumerable<IEventStream> eventStreamsToConsider)
        {
            foreach (IEventStream eventStream in eventStreamsToConsider)
            {
                this.EventStore.MergeEventStreamClock(eventStream);
                _mergedEventStreams.Add(eventStream.Header.EventStreamID);
            }
        }

        private bool StreamsHaveConflicts(VectorClock currentMaxVectorClock, IEnumerable<IEventStream> eventStreams)
        {
            if (currentMaxVectorClock == null)
                return false;

            /*A device could have multiple streams, as long as the last VectorClock of all streams from that device is not
            simultaneous we can assume that there is no conflicts or a previous ConflictResolution will be 
            present.*/
            var maxVectorClockByDevice = eventStreams.GroupBy(s => s.Header.DeviceID).Select(sl => sl.Max(s => s.Header.EndVectorClock));

            foreach (var deviceMaxVector in maxVectorClockByDevice)
            {
                if (currentMaxVectorClock.CompareVectors(deviceMaxVector) == VectorClock.ComparisonResult.Simultaneous)
                {
                    return true;
                }
            }

            return false;
        }


        private EntityGenerator<T> RegisterGenerator<T>(EntityGenerator<T> generator) where T : EntityBase
        {
            _generators[typeof(T)] = generator;
            return generator;
        }

        public void SaveChanges()
        {
            SaveChangesInternal(null);
        }

        private void BeforeSaveChanges()
        {
            //Fire before event recursively down object tree
            Budget.BeforeSaveChanges();

            //Check for generators that implement IHasChanges and fire before event
            foreach (var generator in _generators.Values)
            {
                IHasChanges hasChanges = generator as IHasChanges;
                if (hasChanges != null)
                {
                    hasChanges.BeforeSaveChanges();
                }
            }
        }

        private void SaveChangesInternal(List<EntityConflictResolution> pendingConflictResolutions)
        {
            BeforeSaveChanges();

            var changes = Budget.GetAndSaveChanges().ToList();

            foreach (var change in GetGeneratorChanges())
            {
                changes.Add(change);
            }

            if (pendingConflictResolutions != null)
                changes.AddRange(pendingConflictResolutions.SelectMany(e => e.ConflictResolutionEvents).Where(e => e.DeviceID == Guid.Empty));


            var vectorClock = EventStore.GetMaxVectorClock();

            foreach (var change in changes)
            {
                vectorClock = vectorClock == null ?
                    new VectorClock().Increment(DeviceID)
                    : vectorClock.Increment(DeviceID);

                change.stampEvent(DeviceID, vectorClock);
                this.MessageBus.PublishEvent(change.EntityType, change);
            }

            EventStore.StoreEvents(changes);
            EventStore.SetMaxVectorClock(vectorClock);

            if (pendingConflictResolutions != null)
                RebuildEntities(pendingConflictResolutions);
        }

        private void RebuildEntities(List<EntityConflictResolution> entityConflictResolutions)
        {
            foreach (var entityResolution in entityConflictResolutions)
            {
                foreach (var evt in entityResolution.ConflictResolutionEvents)
                {
                    this.EventStore.IgnoreEvents(evt.EventsToIgnore);
                    this.EventStore.IgnoreEvents(evt.EventID.Yield());
                }

                this.RebuildEntity(entityResolution.EntityType, entityResolution.EntityID);
            }
        }

        private void RebuildEntity(string entityType, string entityId)
        {
            EntityBase entity = this.FindEntity(entityType, entityId);
            var entityEvents = this.EventStore.GetEntityEvents(entityType, entityId);
            entity.RebuildEntity(entityEvents);
        }

        private IEnumerable<ModelEvent> GetGeneratorChanges()
        {
            foreach (var generator in _generators.Values)
            {
                IHasChanges hasChanges = generator as IHasChanges;
                if (hasChanges != null)
                {
                    foreach (var change in hasChanges.GetAndSaveChanges())
                    {
                        yield return change;
                    }
                }
            }
        }
    }
}
