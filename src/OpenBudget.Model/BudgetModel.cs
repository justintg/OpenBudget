using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Entities.Repositories;
using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Infrastructure.Messaging;
using OpenBudget.Model.Infrastructure.UnitOfWork;
using OpenBudget.Model.Synchronization;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenBudget.Model.Tests")]
[assembly: InternalsVisibleTo("OpenBudget.Model.SQLite.Tests")]

namespace OpenBudget.Model
{
    public class BudgetModel
    {
        public Guid DeviceID { get; private set; }
        internal BudgetMessageBus InternalMessageBus { get; private set; }
        public BudgetMessageBus MessageBus { get; private set; }
        public IEventStore EventStore { get; private set; }
        public IBudgetStore BudgetStore { get; private set; }
        internal IBudgetViewCache BudgetViewCache { get; private set; }

        internal EntityDenormalizer<Budget> BudgetGenerator { get; private set; }
        internal EntityDenormalizer<Account> AccountGenerator { get; private set; }
        internal EntityDenormalizer<Transaction> TransactionGenerator { get; private set; }
        internal EntityDenormalizer<MasterCategory> BudgetCategoryGenerator { get; private set; }
        internal EntityDenormalizer<Category> BudgetSubCategoryGenerator { get; private set; }
        internal EntityDenormalizer<Payee> PayeeGenerator { get; private set; }
        internal EntityDenormalizer<CategoryMonth> CategoryMonthGenerator { get; private set; }
        internal EntityDenormalizer<IncomeCategory> IncomeCategoryGenerator { get; private set; }

        internal AccountBalanceDenormalizer AccountBalanceDenormalizer { get; private set; }

        internal EntityRepository<Budget, BudgetSnapshot> BudgetRepository { get; private set; }
        internal EntityRepository<Account, AccountSnapshot> AccountRepository { get; private set; }
        internal EntityRepository<Transaction, TransactionSnapshot> TransactionRepository { get; private set; }
        internal EntityRepository<MasterCategory, MasterCategorySnapshot> MasterCategoryRepository { get; private set; }
        internal EntityRepository<Category, CategorySnapshot> CategoryRepository { get; private set; }
        internal EntityRepository<Payee, PayeeSnapshot> PayeeRepository { get; private set; }
        internal SubEntityRepository<SubTransaction, SubTransactionSnapshot> SubTransactionRepository { get; private set; }
        internal IncomeCategoryRepository IncomeCategoryRepository { get; private set; }
        internal CategoryMonthRepository CategoryMonthRepository { get; private set; }

        internal EntitySnapshotDenormalizer<Budget, BudgetSnapshot> BudgetSnapshotDenormalizer { get; private set; }
        internal EntitySnapshotDenormalizer<Account, AccountSnapshot> AccountSnapshotDenormalizer { get; private set; }
        internal EntitySnapshotDenormalizer<Transaction, TransactionSnapshot> TransactionSnapshotDenormalizer { get; private set; }
        internal EntitySnapshotDenormalizer<MasterCategory, MasterCategorySnapshot> MasterCategorySnapshotDenormalizer { get; private set; }
        internal EntitySnapshotDenormalizer<Category, CategorySnapshot> CategorySnapshotDenormalizer { get; private set; }
        internal EntitySnapshotDenormalizer<Payee, PayeeSnapshot> PayeeSnapshotDenormalizer { get; private set; }
        internal SubEntitySnapshotDenormalizer<SubTransaction, SubTransactionSnapshot, Transaction> SubTransactionSnapshotDenormalizer { get; private set; }
        internal NoCreateEntitySnapshotDenormalizer<CategoryMonth, CategoryMonthSnapshot> CategoryMonthSnapshotDenormalizer { get; private set; }
        internal NoCreateEntitySnapshotDenormalizer<IncomeCategory, IncomeCategorySnapshot> IncomeCategorySnapshotDenormalizer { get; private set; }


        private BudgetViewListener _budgetViewListenter;
        private Dictionary<Type, IEntityDenormalizer> _entityDenormalizers = new Dictionary<Type, IEntityDenormalizer>();
        private Dictionary<string, object> _entityRepositoriesStringKey = new Dictionary<string, object>();
        private Dictionary<Type, object> _entityRepositories = new Dictionary<Type, object>();
        private Dictionary<Type, object> _subEntityRepositories = new Dictionary<Type, object>();
        private ISynchronizationService _syncService;

        public T FindEntity<T>(string entityId) where T : EntityBase
        {
            var repo = FindRepository<T>();
            var entity = repo.GetEntity(entityId);
            return entity.IsDeleted ? default(T) : entity;
        }

        protected BudgetModel(Guid deviceId, IBudgetStore budgetStore) : this(deviceId, budgetStore, true)
        {

        }

        protected BudgetModel(Guid deviceId, IBudgetStore budgetStore, Budget initialBudget) : this(deviceId, budgetStore, false)
        {
            if (initialBudget == null) throw new ArgumentNullException(nameof(initialBudget));

            RegisterHasChanges(initialBudget);
            this.SaveChanges();
        }

        protected BudgetModel(Guid deviceId, IBudgetStore budgetStore, bool createBudget)
        {
            DeviceID = deviceId;
            BudgetStore = budgetStore;
            EventStore = BudgetStore.EventStore;
            InternalMessageBus = new BudgetMessageBus();
            MessageBus = new BudgetMessageBus();

            InitializeInternals();

            if (createBudget)
            {
                var budget = new Budget();
                RegisterHasChanges(budget);
                this.SaveChanges();
            }

            InitializeBudgetViewCache();
        }

        private void InitializeInternals()
        {
            InitializeRepositories();
            InitializeSnapshotDenormalizers();
            InitializeEntityDenormalizers();
        }

        private void InitializeRepositories()
        {
            BudgetRepository = RegisterRepository(new EntityRepository<Budget, BudgetSnapshot>(this));
            AccountRepository = RegisterRepository(new AccountRepository(this));
            TransactionRepository = RegisterRepository(new TransactionRepository(this));
            MasterCategoryRepository = RegisterRepository(new EntityRepository<MasterCategory, MasterCategorySnapshot>(this));
            CategoryRepository = RegisterRepository(new EntityRepository<Category, CategorySnapshot>(this));
            PayeeRepository = RegisterRepository(new EntityRepository<Payee, PayeeSnapshot>(this));
            SubTransactionRepository = RegisterRepository(new SubEntityRepository<SubTransaction, SubTransactionSnapshot>(this));
            IncomeCategoryRepository = (IncomeCategoryRepository)RegisterRepository(new IncomeCategoryRepository(this));
            CategoryMonthRepository = (CategoryMonthRepository)RegisterRepository(new CategoryMonthRepository(this));
        }

        private SubEntityRepository<TEntity, TSnapshot> RegisterRepository<TEntity, TSnapshot>(SubEntityRepository<TEntity, TSnapshot> repository)
            where TEntity : SubEntity where TSnapshot : EntitySnapshot, new()
        {
            _subEntityRepositories[typeof(TEntity)] = repository;
            return repository;
        }

        private NoCreateEntityRepository<TEntity, TSnapshot> RegisterRepository<TEntity, TSnapshot>(NoCreateEntityRepository<TEntity, TSnapshot> repository)
        where TEntity : NoCreateEntity where TSnapshot : EntitySnapshot, new()
        {
            _entityRepositories[typeof(TEntity)] = repository;
            _entityRepositoriesStringKey[typeof(TEntity).Name] = repository;
            return repository;
        }

        private EntityRepository<TEntity, TSnapshot> RegisterRepository<TEntity, TSnapshot>(EntityRepository<TEntity, TSnapshot> repository)
        where TEntity : EntityBase where TSnapshot : EntitySnapshot, new()
        {
            _entityRepositories[typeof(TEntity)] = repository;
            _entityRepositoriesStringKey[typeof(TEntity).Name] = repository;
            return repository;
        }

        private void InitializeSnapshotDenormalizers()
        {
            BudgetSnapshotDenormalizer = new EntitySnapshotDenormalizer<Budget, BudgetSnapshot>(this);
            AccountSnapshotDenormalizer = new EntitySnapshotDenormalizer<Account, AccountSnapshot>(this);
            TransactionSnapshotDenormalizer = new EntitySnapshotDenormalizer<Transaction, TransactionSnapshot>(this);
            MasterCategorySnapshotDenormalizer = new EntitySnapshotDenormalizer<MasterCategory, MasterCategorySnapshot>(this);
            CategorySnapshotDenormalizer = new EntitySnapshotDenormalizer<Category, CategorySnapshot>(this);
            PayeeSnapshotDenormalizer = new EntitySnapshotDenormalizer<Payee, PayeeSnapshot>(this);
            SubTransactionSnapshotDenormalizer = new SubEntitySnapshotDenormalizer<SubTransaction, SubTransactionSnapshot, Transaction>(this);
            CategoryMonthSnapshotDenormalizer = new NoCreateEntitySnapshotDenormalizer<CategoryMonth, CategoryMonthSnapshot>(this, CategoryMonthRepository);
            IncomeCategorySnapshotDenormalizer = new NoCreateEntitySnapshotDenormalizer<IncomeCategory, IncomeCategorySnapshot>(this, IncomeCategoryRepository);
        }

        private void InitializeEntityDenormalizers()
        {
            BudgetGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<Budget>(this));
            AccountGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<Account>(this));
            TransactionGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<Transaction>(this));
            BudgetCategoryGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<MasterCategory>(this));
            BudgetSubCategoryGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<Category>(this));
            PayeeGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<Payee>(this));
            CategoryMonthGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<CategoryMonth>(this));
            IncomeCategoryGenerator = RegisterEntityDenormalizer(new EntityDenormalizer<IncomeCategory>(this));

            AccountBalanceDenormalizer = new AccountBalanceDenormalizer(this);
        }

        private void InitializeBudgetViewCache()
        {
            IBudgetViewCacheFactory cacheFactory = this.BudgetStore.TryGetExtension<IBudgetViewCacheFactory>();
            IBudgetViewCache cache = null;
            if (cacheFactory != null)
            {
                cache = cacheFactory.CreateCache(this);
            }
            else
            {
                cache = new MemoryBudgetViewCache(this);
            }

            this.BudgetViewCache = cache;
            _budgetViewListenter = new BudgetViewListener(this);
        }

        public EntityBase FindEntity(string entityType, string entityId)
        {
            var repository = FindRepository(entityType);
            return repository.GetEntityBase(entityId);
        }

        internal IEntityRepository FindRepository(string entityType)
        {
            if (_entityRepositoriesStringKey.TryGetValue(entityType, out object value))
            {
                if (value is IEntityRepository repository)
                {
                    return repository;
                }
            }

            return null;
        }

        internal ISubEntityRepository<TEntity> FindSubEntityRepository<TEntity>()
            where TEntity : SubEntity
        {
            if (_subEntityRepositories.TryGetValue(typeof(TEntity), out object repository))
            {
                return (ISubEntityRepository<TEntity>)repository;
            }

            return null;
        }

        internal IEntityRepository<TEntity> FindRepository<TEntity>()
            where TEntity : EntityBase
        {
            object repository = null;

            if (_entityRepositories.TryGetValue(typeof(TEntity), out repository))
            {
                return (IEntityRepository<TEntity>)repository;
            }

            return null;
        }


        internal IEntityDenormalizer FindDenormalizer<TEntity>()
        {
            return _entityDenormalizers[typeof(TEntity)];
        }

        internal IEntityDenormalizer FindDenormalizer(EntityBase entity)
        {
            return _entityDenormalizers[entity.GetType()];
        }

        public Budget GetBudget()
        {
            return BudgetRepository.GetAllEntities().Single();
        }

        public EntityBase FindEntity(EntityReference reference)
        {
            return this.FindEntity(reference.EntityType, reference.EntityID);
        }

        public void SetSynchronizationService(ISynchronizationService syncService)
        {
            _syncService = syncService;
        }

        internal void EnsureIdentityTracked(EntityBase entity)
        {
            Type entityType = entity.GetType();
            IIdentityMap generator = _entityDenormalizers[entityType] as IIdentityMap;
            generator.EnsureIdentityTracked(entity);
        }

        internal void AttachToModel(EntityBase entity)
        {
            AttachToModelBase(entity);
            switch (entity.GetType().Name)
            {
                case nameof(Account):
                    AttachAccountToModel((Account)entity);
                    break;
            }
        }

        private void AttachAccountToModel(Account account)
        {
            AccountBalanceDenormalizer.RegisterForChanges(account);
        }

        private void AttachToModelBase(EntityBase entity)
        {
            if (entity.IsAttached && entity.Model != this)
            {
                throw new InvalidOperationException("You can't attach the same entity to multiple models.");
            }
            else if (entity.IsAttached && entity.Model == this)
                return;

            var denormalizer = FindDenormalizer(entity);
            denormalizer.RegisterForChanges(entity);

            entity.Model = this;
            entity.NotifyAttachedToBudget(this);
        }

        public static BudgetModel OpenExistingOnNewDevice(Guid deviceId, ISynchronizationService syncService, IBudgetStore budgetStore)
        {
            //var budgetId = events.Single(e => e.EntityType == "Budget" && e is EntityCreatedEvent).EntityID;
            BudgetModel model = new BudgetModel(deviceId, budgetStore, false);
            model.SetSynchronizationService(syncService);
            model.Sync();

            //model.Budget = (Budget)model.BudgetGenerator.GetAll().Single();
            //model.Budget.AttachToModel(model);
            return model;
        }

        public static BudgetModel Load(Guid deviceId, IBudgetStore budgetStore)
        {
            BudgetModel model = new BudgetModel(deviceId, budgetStore, false);

            VectorClock eventVector = budgetStore.EventStore.GetMaxVectorClock();
            VectorClock snapshotVector = budgetStore.SnapshotStore.GetLastVectorClock();

            if (eventVector.CompareTo(snapshotVector) != 0)
            {
                foreach (var evt in model.EventStore.GetEvents())
                {
                    model.InternalMessageBus.PublishEvent(evt.EntityType, evt);
                }
            }

            model.BudgetViewCache.RecalculateCache();
            return model;
        }

        public static BudgetModel CreateNew(Guid deviceId, IBudgetStore budgetStore)
        {
            return new BudgetModel(deviceId, budgetStore);
        }

        public static BudgetModel CreateNew(Guid deviceId, IBudgetStore budgetStore, Budget initialBudget)
        {
            if (initialBudget == null) throw new ArgumentNullException(nameof(initialBudget));

            return new BudgetModel(deviceId, budgetStore, initialBudget);
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

        private EntityDenormalizer<T> RegisterEntityDenormalizer<T>(EntityDenormalizer<T> entityDenormalizer) where T : EntityBase
        {
            _entityDenormalizers[typeof(T)] = entityDenormalizer;
            return entityDenormalizer;
        }

        public void CancelChanges()
        {
            _unitOfWork.CancelChanges();
        }

        public void SaveChanges()
        {
            SaveChangesInternal(null);
        }

        private UnitOfWork _unitOfWork = new UnitOfWork();

        internal UnitOfWork GetCurrentUnitOfWork() => _unitOfWork;

        internal void RegisterHasChanges(EntityBase entity)
        {
            _unitOfWork.RegisterChangedEntity(entity);
        }

        internal void EnsureSaveOrder(EntityBase first, EntityBase second)
        {
            _unitOfWork.EnsureSaveOrder(first, second);
        }

        private void SaveChangesInternal(List<EntityConflictResolution> pendingConflictResolutions)
        {
            /*if (pendingConflictResolutions != null)
                changes.AddRange(pendingConflictResolutions.SelectMany(e => e.ConflictResolutionEvents).Where(e => e.DeviceID == Guid.Empty));*/

            var changes = _unitOfWork.GetChangeEvents();

            var events = changes.Select(c => c.Event).ToList();
            StampEvents(events);
            EventStore.StoreEvents(events);

            UpdateModelState(changes);


            if (pendingConflictResolutions != null)
                RebuildEntities(pendingConflictResolutions);

            _unitOfWork = new UnitOfWork();
        }

        private IDisposable StartUpdateBatch()
        {
            return Disposable.Create(
                    BudgetStore.SnapshotStore.StartSnapshotStoreBatch(),
                    AccountBalanceDenormalizer.StartBatch(),
                    _budgetViewListenter.StartBatch()
                );
        }

        private void UpdateModelState(List<EventSaveInfo> changes)
        {
            using (StartUpdateBatch())
            {
                foreach (var change in changes)
                {
                    change.EventSavedCallback(change);
                    if (change.NeedsAttach)
                    {
                        AttachToModel(change.Entity);
                    }
                    InternalMessageBus.PublishEvent(change.Event.EntityType, change.Event);
                    MessageBus.PublishEvent(change.Event.EntityType, change.Event);
                }
            }
        }

        private void StampEvents(IEnumerable<ModelEvent> events)
        {
            var vectorClock = EventStore.GetMaxVectorClock();

            foreach (var evt in events)
            {
                vectorClock = vectorClock == null ?
                    new VectorClock().Increment(DeviceID)
                    : vectorClock.Increment(DeviceID);
                evt.StampEvent(DeviceID, vectorClock);
            }

            EventStore.SetMaxVectorClock(vectorClock);
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
    }
}
