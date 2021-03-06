﻿using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.UnitOfWork;
using OpenBudget.Model.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class EntityBase<TSnapshot> : EntityBase where TSnapshot : EntitySnapshot, new()
    {
        private static PropertyAccessorSet<TSnapshot> _snapshotProperties;

        static EntityBase()
        {
            _snapshotProperties = new PropertyAccessorSet<TSnapshot>();
        }

        private TSnapshot _entityData = new TSnapshot();

        protected EntityBase(string entityId) : base(entityId)
        {
        }

        protected EntityBase(EntityCreatedEvent evt) : base(evt)
        {
        }

        protected EntityBase(TSnapshot snapshot) : base(snapshot.EntityID)
        {
            _entityData = snapshot;
            this.SaveState = EntitySaveState.Unattached;
            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
        }

        internal TSnapshot GetSnapshot()
        {
            return _entityData;
        }

        protected override EntitySnapshot GetSnapshotInternal()
        {
            return _entityData;
        }

        protected override T GetEntityData<T>(string property)
        {
            return _snapshotProperties.GetEntityData<T>(_entityData, property);
        }

        protected override void SetEntityData<T>(T value, string property)
        {
            _snapshotProperties.SetEntityData<T>(_entityData, value, property);
        }

        protected override IEnumerable<string> GetPropertyNames()
        {
            return _snapshotProperties.GetPropertyNames();
        }

        protected override void SetEntityDataObject(object value, string property)
        {
            _snapshotProperties.SetEntityDataObject(_entityData, value, property);
        }

        protected override object GetEntityDataObject(string property)
        {
            return _snapshotProperties.GetEntityDataObject(_entityData, property);
        }

        protected override void ClearEntityData()
        {
            _entityData = new TSnapshot();
        }
    }

    public enum EntitySaveState
    {
        Unattached,
        UnattachedRegistered,
        AttachedNoChanges,
        AttachedHasChanges,
    }

    public abstract class EntityBase : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo
    {
        public string EntityID
        {
            get { return GetProperty<string>(); }
            private set { SetProperty(value); }
        }

        public bool IsDeleted
        {
            get => GetProperty<bool>();
            private set => SetProperty(value);
        }

        public string LastEventID
        {
            get => GetProperty<string>();
            protected set => SetEntityData<string>(value, nameof(LastEventID));
        }

        public VectorClock LastEventVector
        {
            get => GetProperty<VectorClock>();
            protected set => SetEntityData<VectorClock>(value?.Copy(), nameof(LastEventVector));
        }

        public virtual bool IsAttached
        {
            get { return SaveState == EntitySaveState.AttachedHasChanges || SaveState == EntitySaveState.AttachedNoChanges; }
        }

        private EntitySaveState _saveState;

        public virtual EntitySaveState SaveState
        {
            get
            {
                return _saveState;
            }
            internal set
            {
                _saveState = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsAttached));
            }
        }

        internal EntityLookupRoot LookupRoot { get; set; }

        public virtual EntityBase Parent
        {
            get { return ResolveEntityReference<EntityBase>(isLoadingParent: true); }
            internal set { SetEntityReference<EntityBase>(value); }
        }

        public Budget GetParentBudget()
        {
            var parent = Parent;
            if (parent == null)
            {
                return (Budget)this;
            }

            return parent.GetParentBudget();
        }

        protected abstract EntitySnapshot GetSnapshotInternal();

        protected EntityBase(string entityId)
        {
            _childEntityCollections = new List<IEntityCollection>();
            _subEntities = new Dictionary<string, ISubEntityCollection>();
            CurrentEvent = new EntityCreatedEvent(this.GetType().Name, entityId);
            SaveState = EntitySaveState.Unattached;
            EntityID = entityId;
            RegisterValidations();
            RegisterDependencies();
            RegisterCurrencyProperties();
            LookupRoot = new EntityLookupRoot(this);
        }

        protected EntityBase(EntityCreatedEvent evt)
        {
            _childEntityCollections = new List<IEntityCollection>();
            _subEntities = new Dictionary<string, ISubEntityCollection>();
            ReplayEvents(evt.Yield());
            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
            SaveState = EntitySaveState.AttachedNoChanges;
            RegisterValidations();
            RegisterDependencies();
            RegisterCurrencyProperties();
            LookupRoot = new EntityLookupRoot(this);
        }

        public FieldChangeEvent CurrentEvent { get; protected set; }

        private BudgetModel _model;

        public virtual BudgetModel Model
        {
            get { return _model; }
            internal set { _model = value; }
        }

        public virtual void Delete()
        {
            IsDeleted = true;
            if (Parent != null)
                Parent.RequestChildDeletion(this);
        }

        public bool HasChanges
        {
            get
            {
                return this.CurrentEvent is EntityCreatedEvent || this.CurrentEvent.Changes.Count > 0;
            }
        }

        internal void RemoveReferenceToChild(EntityBase child)
        {
            GetChildCollection(child)?.ForceRemoveChild(child);
        }

        internal void ForceReferenceToChild(EntityBase child)
        {

            GetChildCollection(child)?.ForceAddChild(child);
        }

        internal void RequestChildDeletion(EntityBase child)
        {
            if (child is SubEntity childSubEntity)
            {
                var collection = GetSubEntityCollection(childSubEntity);
                collection.DeleteChild(childSubEntity);
            }
            else
            {
                GetChildCollection(child)?.RequestDeletion(child);
            }
        }

        internal void CancelChildDeletion(EntityBase child)
        {
            GetChildCollection(child)?.CancelDeletion(child);
        }

        private IEntityCollection GetChildCollection(EntityBase child)
        {
            var childType = child.GetType();
            var childCollectionType = typeof(EntityCollection<>).MakeGenericType(childType);
            var childCollection = _childEntityCollections.Where(c => c.GetType() == childCollectionType).FirstOrDefault();
            return childCollection;
        }

        private ISubEntityCollection GetSubEntityCollection(SubEntity child)
        {
            var entityType = child.GetType().Name;
            return _subEntities[entityType];
        }

        internal virtual void BeforeSaveChanges(BudgetModel budgetModel)
        {
            FormatCurrencyProperties(budgetModel);
            foreach (var subEntity in _subEntities.Values)
            {
                subEntity.BeforeSaveChanges(budgetModel);
            }

            if (this is ISortableEntity sortable)
            {
                DetermineSortOrder(budgetModel, sortable);
            }
        }

        internal bool IsBeingSaved { get; private set; }
        internal bool RegisteredForChanges { get; private set; }

        internal void NotifyLoadedFromChild(EntityBase child)
        {
            var childCollection = GetChildCollection(child);
            childCollection?.EnsureContainsMaterializedChild(child);
        }

        internal void NotifyAttachedToBudget(BudgetModel model)
        {
            if (HasChanges)
                SaveState = EntitySaveState.AttachedHasChanges;
            else
                SaveState = EntitySaveState.AttachedNoChanges;

            foreach (var childEntityCollection in _childEntityCollections)
            {
                childEntityCollection.AttachToModel(model);
            }

            OnAttached(model);
        }

        protected virtual void OnAttached(BudgetModel model)
        {

        }

        protected void NotifyEventSaved(EventSaveInfo eventSaveInfo)
        {
            var evt = eventSaveInfo.Event;
            LastEventID = evt.EventID.ToString();
            LastEventVector = evt.EventVector;

            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);

            if (IsAttached)
                SaveState = EntitySaveState.AttachedNoChanges;

            if (eventSaveInfo.SubEntityEvents != null)
            {
                foreach (var subEntitySaveInfo in eventSaveInfo.SubEntityEvents)
                {
                    subEntitySaveInfo.EventSavedCallback(subEntitySaveInfo);
                }
            }
        }

        private EventSaveInfo ConvertToCallback(ModelEvent evt, List<EventSaveInfo> subEntityEvents = null)
        {
            bool needsAttach = !IsAttached;
            return new EventSaveInfo()
            {
                Entity = this,
                NeedsAttach = needsAttach,
                Event = evt,
                EventSavedCallback = NotifyEventSaved,
                Snapshot = GetSnapshotInternal(),
                SubEntityEvents = subEntityEvents
            };
        }

        internal virtual IEnumerable<EventSaveInfo> GetAndSaveChanges()
        {
            bool groupChangesPublished = false;
            List<FieldChangeEvent> groupedChanges = new List<FieldChangeEvent>();
            List<EventSaveInfo> subEntityEvents = new List<EventSaveInfo>();

            foreach (var subEntity in _subEntities.Values)
            {
                var changes = subEntity.GetChanges();
                subEntityEvents.AddRange(changes);
                groupedChanges.AddRange(changes.Select(c => c.Event).Where(e => e is FieldChangeEvent).Select(e => e as FieldChangeEvent));
            }

            var evt = CurrentEvent;
            if (evt.Changes.Count > 0 || evt is EntityCreatedEvent)
            {
                if (groupedChanges.Count > 0)
                {
                    /*The FieldChangeEvent from the main entity should be the first in the collection so that
                    on rebuild the entity will be created first and then Sub-Entity events will be broadcasted
                    through the newly created entity*/
                    groupedChanges.Insert(0, evt);
                    GroupedFieldChangeEvent groupedEvent = new GroupedFieldChangeEvent(this.GetType().Name, EntityID, groupedChanges);
                    groupChangesPublished = true;
                    yield return ConvertToCallback(groupedEvent, subEntityEvents);
                }
                else
                    yield return ConvertToCallback(evt);
            }

            if (groupedChanges.Count > 0 && !groupChangesPublished)
            {
                GroupedFieldChangeEvent groupedEvent = new GroupedFieldChangeEvent(this.GetType().Name, EntityID, groupedChanges);
                yield return ConvertToCallback(groupedEvent, subEntityEvents);
            }

            /*foreach (var child in _childEntityCollections)
            {
                foreach (var change in child.GetAndSaveChanges())
                {
                    yield return change;
                }
            }*/
        }

        internal T RegisterChildEntityCollection<T>(T childEntityCollection) where T : IEntityCollection
        {
            _childEntityCollections.Add(childEntityCollection);
            return childEntityCollection;
        }

        internal SubEntityCollection<T> RegisterSubEntityCollection<T>(SubEntityCollection<T> subEntityCollection) where T : SubEntity
        {
            _subEntities.Add(typeof(T).Name, subEntityCollection);
            return subEntityCollection;
        }

        private List<IEntityCollection> _childEntityCollections;

        internal Dictionary<string, ISubEntityCollection> _subEntities;


        internal void rebuildEntityData(IEnumerable<FieldChangeEvent> events)
        {
            NotifyAllPropertiesChanging();

            ClearEntityData();
            ReplayEvents(events);

            NotifyAllPropertiesChanged();
        }

        protected void NotifyAllPropertiesChanging()
        {
            foreach (var prop in GetPropertyNames())
            {
                RaisePropertyChanging(prop);
            }
        }

        protected void NotifyAllPropertiesChanged()
        {
            foreach (var prop in GetPropertyNames())
            {
                RaisePropertyChanged(prop);
            }
        }

        internal virtual void RebuildEntity(IEnumerable<ModelEvent> events)
        {
            ClearEntityData();
            var fieldChangeEvents = events.Where(e => e is FieldChangeEvent).Select(e => (FieldChangeEvent)e);
            ReplayEvents(fieldChangeEvents);
        }

        internal virtual void ReplayEvents(IEnumerable<FieldChangeEvent> events)
        {
            /*Check if this event is actually the last event and do nothing
             * if it is, since this entity already has current data */
            if (events.Count() == 1)
            {
                var evt = events.Single();
                if (evt.EventID.ToString() == LastEventID)
                    return;
            }

            var changedProperties = events.SelectMany(e => e.Changes.Keys).Distinct().ToList();
            foreach (var prop in changedProperties)
                RaisePropertyChanging(prop);

            foreach (var evt in events)
            {
                foreach (var change in evt.Changes)
                {
                    object previousValue = GetEntityDataObject(change.Key);

                    SetEntityDataObject(change.Value.NewValue, change.Key);
                    OnReplayChange(change.Key, previousValue, change.Value);
                }
                LastEventID = evt.EventID.ToString();
                LastEventVector = evt.EventVector;
            }

            foreach (var prop in changedProperties)
                RaisePropertyChanged(prop);
        }

        protected virtual void OnReplayChange(string field, object previousValue, FieldChange change)
        {

        }

        internal virtual void HandleSubEntityEvent(FieldChangeEvent @event)
        {
            var collection = _subEntities[@event.EntityType];
            if (@event is EntityCreatedEvent)
            {
                collection.Handle((EntityCreatedEvent)@event);
            }
            else if (@event is EntityUpdatedEvent)
            {
                collection.Handle((EntityUpdatedEvent)@event);
            }
        }

        internal void CancelCurrentChanges()
        {
            if (CurrentEvent is EntityCreatedEvent)
                throw new InvalidOperationException("You cant cancel the Entity Created Event");

            var changes = CurrentEvent.Changes;
            foreach (var change in changes)
            {
                SetEntityDataObject(change.Value.PreviousValue, change.Key);
                OnCancelChange(change.Key, change.Value);
            }
            NotifyAllPropertiesChanged();

            foreach (var subEntity in _subEntities)
            {
                subEntity.Value.CancelCurrentChanges();
            }

            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
        }

        protected virtual void OnCancelChange(string property, FieldChange change)
        {
            if (property == nameof(Parent)) HandleParentCancel(change);
            if (property == nameof(IsDeleted)) HandleDeletedCancel(change);
        }

        protected virtual void HandleDeletedCancel(FieldChange genericChange)
        {
            if (!(genericChange is TypedFieldChange<bool> change)) return;
            if (change.TypedNewValue && !change.TypedPreviousValue)
            {
                this.Parent.CancelChildDeletion(this);
            }
        }

        protected virtual void HandleParentCancel(FieldChange genericChange)
        {
            if (!(genericChange is TypedFieldChange<EntityReference> change)) return;
            if (change.TypedPreviousValue != null
                && change.TypedNewValue != null
                && change.TypedPreviousValue != change.TypedNewValue)
            {
                var parent = change.TypedPreviousValue.ReferencedEntity;
                var canceledParent = change.TypedNewValue.ReferencedEntity;
                canceledParent.RemoveReferenceToChild(this);
                parent.ForceReferenceToChild(this);
            }
        }

        protected abstract void SetEntityData<T>(T value, string property);
        protected abstract T GetEntityData<T>(string property);
        protected abstract IEnumerable<string> GetPropertyNames();
        protected abstract void SetEntityDataObject(object value, string property);
        protected abstract object GetEntityDataObject(string property);
        protected abstract void ClearEntityData();

        internal T GetProperty<T>([CallerMemberName]string property = null)
        {
            return GetEntityData<T>(property);
        }

        public int GetCurrencyDenominator()
        {
            return this.IsAttached ? Model.CurrencyDenominator : Budget.DEFAULT_CURRENCY_DENOMINATOR; ;
        }

        protected void SetCurrency(decimal value, [CallerMemberName]string property = null)
        {
            int denominator = GetCurrencyDenominator();

            string denominatorProperty = property + "_Denominator";

            long longValue = CurrencyConverter.ToLongValue(value, denominator);

            SetProperty<long>(longValue, property);
            SetProperty<int>(denominator, denominatorProperty);
        }

        protected decimal GetCurrency([CallerMemberName]string property = null)
        {
            string denominatorProperty = property + "_Denominator";
            int denominator = GetProperty<int>(denominatorProperty);

            long longValue = GetProperty<long>(property);
            if (longValue == 0 || denominator == 0) return 0M;

            return CurrencyConverter.ToDecimalValue(longValue, denominator);
        }

        protected void SetProperty<T>(T value, [CallerMemberName]string property = null)
        {
            RaisePropertyChanging(property);

            T oldValue = GetProperty<T>(property);

            SetEntityData<T>(value, property);

            T newValue = value;

            if (_validationEnabled)
            {
                ValidateProperty(property);
                ValidateDependencies(property);
            }

            RaisePropertyChanged(property);
            FieldChange change = FieldChange.Create(oldValue, newValue);
            CurrentEvent.AddChange(property, change);

            EnsureRegisteredForChanges();
        }

        protected virtual void EnsureRegisteredForChanges()
        {
            if (!IsAttached) return;

            if (this.SaveState == EntitySaveState.AttachedNoChanges)
            {
                this.Model.RegisterHasChanges(this);
            }
        }

        internal bool IsPropertyNull(string property)
        {
            object propertyValue = GetEntityDataObject(property);
            return propertyValue == null;
        }

        internal void ForceResolveEntityReference(string property, EntityBase entity)
        {
            EntityReference reference = GetProperty<EntityReference>(property);
            if (reference == null)
                throw new InvalidOperationException();

            reference.ResolveToEntity(entity);
        }

        protected T ResolveEntityReference<T>([CallerMemberName]string property = null, bool isLoadingParent = false) where T : EntityBase
        {
            EntityReference reference = GetProperty<EntityReference>(property);
            if (reference == null)
                return null;

            if (reference.IsReferenceResolved(Model))
            {
                if (reference.ReferencedEntity is T)
                    return (T)reference.ReferencedEntity;
                else
                    return null;
            }
            else
            {
                if (this.IsAttached)
                {
                    T resolvedEntity = LookupRoot.ResolveEntity<T>(Model, reference);
                    if (isLoadingParent)
                    {
                        resolvedEntity.NotifyLoadedFromChild(this);
                    }
                    return resolvedEntity;
                }
                else
                {
                    if (reference.ReferencedEntity != null)
                    {
                        return reference.ReferencedEntity as T;
                    }
                    throw new InvalidOperationException("Cannot Resolve an Unresolved EntityReference when the reference entity is not attached to a model");
                }
            }
        }

        internal virtual void SubEntityCreated(SubEntity subEntity)
        {
            EnsureRegisteredForChanges();
        }

        internal virtual void RegisterHasChanges(SubEntity subEntity)
        {
            EnsureRegisteredForChanges();
        }

        protected void SetEntityReference<T>(T value, [CallerMemberName]string property = null) where T : EntityBase
        {
            if (value == null)
            {
                SetProperty<EntityReference>(null, property);
                return;
            }

            if (this.IsAttached && value.IsAttached && this.Model != null && (this.Model != value.Model))
                throw new InvalidOperationException("You cannot set an entity reference to an Entity attached to a different model");

            //Entity may not be attached to a model yet but it references an entity from this model, when the entity becomes 
            //attached if the model doesn't match it will throw an exception
            if (this.Model == null && value.IsAttached)
                this.Model = value.Model;

            SetProperty<EntityReference>(value.ToEntityReference(), property);
        }

        protected void RaisePropertyChanging(string propertyName)
        {
            PropertyChangingEventArgs args = new PropertyChangingEventArgs(propertyName);
            if (PropertyChanging != null)
                PropertyChanging(this, args);
        }

        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            if (PropertyChanged != null)
                PropertyChanged(this, args);
        }

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public EntityReference ToEntityReference()
        {
            return new EntityReference(this);
        }

        internal IEnumerable<IEntityCollection> EnumerateChildEntityCollections()
        {
            return _childEntityCollections.ToList();
        }

        #region INotifyDataErrorInfo
        private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        public bool HasErrors
        {
            get
            {
                try
                {
                    var propErrorsCount = _errors.Values.FirstOrDefault(errorList => errorList.Count > 0);
                    if (propErrorsCount != null)
                        return true;
                    else
                        return false;
                }
                catch { }
                return true;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
            {
                DataErrorsChangedEventArgs args = new DataErrorsChangedEventArgs(propertyName);
                ErrorsChanged(this, args);
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            List<string> errors = new List<string>();
            if (propertyName != null)
            {
                _errors.TryGetValue(propertyName, out errors);
                return errors;
            }
            else
                return null;
        }
        #endregion

        #region Validation
        private Dictionary<string, List<Func<string>>> _validationRules = new Dictionary<string, List<Func<string>>>();
        private Dictionary<string, List<string>> _dependencies = new Dictionary<string, List<string>>();

        /// <summary>
        /// The indicator to the <see cref="SetProperty{T}(T, string)"/> function that it
        /// should validate properties that are changing.
        /// </summary>
        private bool _validationEnabled;

        /// <summary>
        /// A property that tells the underlying model that validation is enabled or disabled.
        /// This property must be manually set to true by inherited classes.
        /// </summary>
        protected bool ValidationEnabled
        {
            get => _validationEnabled;
            set
            {
                _validationEnabled = value;
                if (value)
                {
                    ValidateAllRules();
                }
                else
                {
                    _errors = new Dictionary<string, List<string>>();

                    foreach (string prop in _validationRules.Keys)
                    {
                        RaiseErrorsChanged(prop);
                    }

                    RaisePropertyChanged(nameof(HasErrors));
                }
            }
        }

        /// <summary>
        /// Registers a property as dependent on the value of another property.  When the value of the source property changes
        /// it is expected the value of the dependent property has also changed.  This is used to trigger a data validation event
        /// on the dependent property.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="dependentProperty"/></typeparam>
        /// <typeparam name="T2">The type of the <paramref name="sourceProperty"/></typeparam>
        /// <param name="dependentProperty">A <see cref="MemberExpression"/> of the property that is dependent on the <paramref name="sourceProperty"/></param>
        /// <param name="sourceProperty">A <see cref="MemberExpression"/> of the property that the <paramref name="dependentProperty"/> depends on.</param>
        protected void RegisterDependency<T, T2>(Expression<Func<T>> dependentProperty, Expression<Func<T2>> sourceProperty)
        {
            var dependent = ExpressionHelper.propertyName(dependentProperty);
            var source = ExpressionHelper.propertyName(sourceProperty);

            List<string> dependencyList;
            if (!_dependencies.TryGetValue(source, out dependencyList))
            {
                dependencyList = new List<string>();
                _dependencies[source] = dependencyList;
            }

            if (!dependencyList.Contains(dependent))
            {
                dependencyList.Add(dependent);
            }
        }

        /// <summary>
        /// Registers a validation rule for a property that will be automatically monitored when
        /// the given property changes. Once an invalid state is detected observers will be notified with
        /// the <see cref="INotifyDataErrorInfo"/> interface.
        /// </summary>
        /// <typeparam name="T">The type of the property that will be validated.</typeparam>
        /// <param name="property">A member <see cref="MemberExpression"/> of the property to be validated.</param>
        /// <param name="IsError">A delegate function that returns if the property is currently in an invalid state.</param>
        /// <param name="errorMessage">The message that will be passed to observers when the property is in an invalid state.</param>
        protected void RegisterValidationRule<T>(Expression<Func<T>> property, Func<bool> IsError, string errorMessage)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            MemberExpression body = property.Body as MemberExpression;

            if (body == null)
                throw new ArgumentException("The body must be a member expression");

            string propertyName = body.Member.Name;

            List<Func<string>> ruleList;
            if (!_validationRules.TryGetValue(propertyName, out ruleList))
            {
                ruleList = new List<Func<string>>();
                _validationRules[propertyName] = ruleList;
            }

            ruleList.Add(() =>
            {
                if (IsError())
                {
                    return errorMessage;
                }
                else
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Runs all Validation rules for properties that depend on a given property and and creates notification for errors 
        /// via the <see cref="INotifyDataErrorInfo"/> interface.
        /// </summary>
        /// <param name="propertyName">The name of the property that other properties depend on.</param>
        protected void ValidateDependencies(string propertyName)
        {
            List<string> dependencyList;
            if (_dependencies.TryGetValue(propertyName, out dependencyList))
            {
                foreach (var dependentProperty in dependencyList)
                {
                    ValidateProperty(dependentProperty);
                }
            }
        }

        /// <summary>
        /// Runs all validation rules for a given property and creates notification for errors 
        /// via the <see cref="INotifyDataErrorInfo"/> interface.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate.</param>
        protected void ValidateProperty(string propertyName)
        {
            List<Func<string>> propertyRules;
            if (_validationRules.TryGetValue(propertyName, out propertyRules))
            {
                ValidateRules(propertyName, propertyRules);
            }
        }

        protected void ValidateRules(string property, List<Func<string>> rules)
        {
            List<string> errors = new List<string>();
            foreach (var rule in rules)
            {
                string result = rule();
                if (!string.IsNullOrEmpty(result))
                {
                    errors.Add(result);
                }
            }
            if (errors.Count > 0)
            {
                _errors[property] = errors;
            }
            else
            {
                _errors.Remove(property);
            }
            RaiseErrorsChanged(property);
            RaisePropertyChanged(nameof(HasErrors));
        }

        protected void ValidateAllRules()
        {
            foreach (string prop in _validationRules.Keys)
            {
                ValidateProperty(prop);
            }
        }

        protected virtual void RegisterValidations()
        {
        }

        protected virtual void RegisterDependencies()
        {
        }

        protected virtual void RegisterCurrencyProperties()
        {

        }

        private List<string> _currencyProperties = new List<string>();

        protected virtual void RegisterCurrencyProperty(string propertyName)
        {
            _currencyProperties.Add(propertyName);
        }

        private void FormatCurrencyProperties(BudgetModel budgetModel)
        {
            int budgetDenominator = budgetModel.CurrencyDenominator;
            foreach (var property in _currencyProperties)
            {
                string denominatorProperty = property + "_Denominator";
                int denominator = GetProperty<int>(denominatorProperty);
                if (denominator == budgetDenominator) continue;
                long longValue = GetProperty<long>(property);

                if (longValue != 0)
                    longValue = CurrencyConverter.ChangeDenominator(longValue, denominator, budgetDenominator);

                SetProperty<long>(longValue, property);
                SetProperty<int>(budgetDenominator, denominatorProperty);
            }
        }

        internal void LoadSubEntities(List<EntitySnapshot> subEntitySnapshots)
        {
            foreach (var subEntity in _subEntities.Values)
            {
                subEntity.LoadCollection(subEntitySnapshots);
            }
        }

        internal void LoadSubEntities()
        {
            foreach (var subEntity in _subEntities.Values)
            {
                subEntity.LoadCollection();
            }
        }

        private protected virtual void SetSortOrderImpl(ISortableEntity sortable, int position)
        {
            var parentCollection = sortable.GetParentCollection();

            if (parentCollection == null || !parentCollection.IsLoaded)
                throw new InvalidBudgetActionException("You cannot set the SortOrder when the parent collection is not loaded.");

            var pendingAdds = parentCollection.GetPendingAdds();

            //Sort categories by their original sort order before setting the order below
            var sortedChildren = parentCollection.EnumerateChildren().Where(c => !pendingAdds.Contains(c)).Select(e => (ISortableEntity)e).OrderBy(e => e.SortOrder).ToList();

            sortable.ForceSetSortOrder(0);

            int index = sortedChildren.IndexOf(sortable);
            if (position == index) return;
            if (index >= 0)
            {
                if (position > index)
                {
                    sortedChildren.Insert(position, sortable);
                    sortedChildren.RemoveAt(index);
                }
                else if (position < index)
                {
                    sortedChildren.Insert(position, sortable);
                    sortedChildren.RemoveAt(index + 1);
                }
            }
            else
            {
                sortedChildren.Insert(position, sortable);
            }

            for (int i = 0; i < sortedChildren.Count; i++)
            {
                if (sortedChildren[i].SortOrder != i)
                {
                    sortedChildren[i].ForceSetSortOrder(i);
                }
            }
        }

        private protected virtual void DetermineSortOrder(BudgetModel budgetModel, ISortableEntity sortable)
        {
            if (!this.IsAttached)
            {
                DetermineSortOrderImpl(budgetModel, sortable);
            }
            else
            {
                if (this.CurrentEvent.Changes.ContainsKey(nameof(EntityBase.Parent)) &&
                    !this.CurrentEvent.Changes.ContainsKey(nameof(Category.SortOrder)))
                {
                    DetermineSortOrderImpl(budgetModel, sortable);
                }
            }
        }

        private protected virtual void DetermineSortOrderImpl(BudgetModel budgetModel, ISortableEntity sortable)
        {
            var parent = Parent;
            var parentCollection = sortable.GetParentCollection();
            if (parent.IsAttached)
            {
                if (parentCollection.IsLoaded)
                {
                    sortable.ForceSetSortOrder(parentCollection.IndexOf(this));
                }
                else
                {
                    var pendingAdds = parentCollection.GetPendingAdds().Select(e => (ISortableEntity)e).ToList();
                    int pendingAddIndex = pendingAdds.IndexOf(sortable);
                    if (pendingAddIndex == 0)
                    {
                        sortable.ForceSetSortOrder(sortable.GetMaxSnapshotSortOrder(budgetModel.BudgetStore.SnapshotStore) + 1);
                    }
                    else if (pendingAddIndex > 0)
                    {
                        ISortableEntity first = pendingAdds[0];
                        sortable.ForceSetSortOrder(first.SortOrder + pendingAddIndex);
                    }
                }
            }
            else
            {
                sortable.ForceSetSortOrder(parentCollection.IndexOf(this));
            }
        }
        #endregion
    }
}

