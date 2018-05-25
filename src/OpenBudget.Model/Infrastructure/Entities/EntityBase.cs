using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class EntityBase : INotifyPropertyChanged, INotifyPropertyChanging, IHasChanges, INotifyDataErrorInfo
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

        private bool _isAttached;

        public bool IsAttached
        {
            get { return _isAttached; }
            private set { _isAttached = value; }
        }

        public virtual EntityBase Parent
        {
            get { return ResolveEntityReference<EntityBase>(); }
            internal set { SetEntityReference<EntityBase>(value); }
        }

        protected EntityBase(string entityId)
        {
            _entityData = new Dictionary<string, object>();
            _childEntities = new List<IEntityCollection>();
            _subEntities = new Dictionary<string, ISubEntityCollection>();
            CurrentEvent = new EntityCreatedEvent(this.GetType().Name, entityId);
            EntityID = entityId;
            RegisterValidations();
            RegisterDependencies();
        }

        protected EntityBase(EntityCreatedEvent evt)
        {
            _entityData = new Dictionary<string, object>();
            _childEntities = new List<IEntityCollection>();
            _subEntities = new Dictionary<string, ISubEntityCollection>();
            ReplayEvents(evt.Yield());
            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
            RegisterValidations();
            RegisterDependencies();
        }

        public FieldChangeEvent CurrentEvent { get; protected set; }

        private BudgetModel _model;

        public BudgetModel Model => _model;

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

        private void SerializeProperties()
        {
            foreach (var serializedProperty in _serializedProperties.Values)
            {
                if (!serializedProperty.IsLoaded)
                    continue;

                if (serializedProperty.HasChanged)
                {
                    var stringValue = this.GetProperty<string>(serializedProperty.PropertyName);
                    if (serializedProperty.DeserializedObject == null & stringValue != null)
                    {
                        SetProperty<string>(null, serializedProperty.PropertyName);
                    }
                    else if (serializedProperty.DeserializedObject != null)
                    {
                        Serializer ser = new Serializer();
                        string newValue = ser.SerializeToString(serializedProperty.DeserializedObject, serializedProperty.PropertyType);
                        SetProperty<string>(newValue, serializedProperty.PropertyName);
                    }
                }
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
            var childCollection = _childEntities.Where(c => c.GetType() == childCollectionType).FirstOrDefault();
            return childCollection;
        }

        private ISubEntityCollection GetSubEntityCollection(SubEntity child)
        {
            var entityType = child.GetType().Name;
            return _subEntities[entityType];
        }

        void IHasChanges.BeforeSaveChanges()
        {
            this.BeforeSaveChanges();
        }

        internal virtual void BeforeSaveChanges()
        {
            foreach (var childCollection in _childEntities)
            {
                childCollection.BeforeSaveChanges();
            }
        }

        internal virtual IEnumerable<ModelEvent> GetAndSaveChanges()
        {
            SerializeProperties();

            bool groupChangesPublished = false;
            List<FieldChangeEvent> groupedChanges = new List<FieldChangeEvent>();

            foreach (var subEntity in _subEntities.Values)
            {
                groupedChanges.AddRange(subEntity.GetChanges().Where(e => e is FieldChangeEvent).Select(e => e as FieldChangeEvent));
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
                    yield return groupedEvent;
                }
                else
                    yield return evt;

                CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
            }

            if (groupedChanges.Count > 0 && !groupChangesPublished)
            {
                GroupedFieldChangeEvent groupedEvent = new GroupedFieldChangeEvent(this.GetType().Name, EntityID, groupedChanges);
                yield return groupedEvent;
            }

            foreach (var child in _childEntities)
            {
                foreach (var change in child.GetAndSaveChanges())
                {
                    yield return change;
                }
            }
        }

        internal class SerializedProperty
        {
            private IChangeListener _listener;
            private object _deserializedObject;
            private bool _hasChanged = false;

            public SerializedProperty(string propertyName, Type propertyType, IChangeListener listener)
            {
                PropertyName = propertyName;
                PropertyType = propertyType;
                _listener = listener;
            }

            public string PropertyName { get; private set; }
            public Type PropertyType { get; private set; }
            public bool IsLoaded { get; private set; }

            public void NotifyChangesSaved()
            {
                _hasChanged = false;
            }

            public bool HasChanged
            {
                get
                {
                    if (_listener == null)
                        return true;

                    return _hasChanged;
                }
            }

            public object DeserializedObject
            {
                get { return _deserializedObject; }
                set
                {
                    _deserializedObject = value;
                    _hasChanged = true;
                    IsLoaded = true;
                }
            }

            public void DeserializeObjectOnLoad(object obj)
            {
                IsLoaded = true;
                _deserializedObject = obj;
            }

            public void ForceRefresh()
            {
                IsLoaded = false;
                _deserializedObject = null;
            }
        }

        private Dictionary<string, SerializedProperty> _serializedProperties = new Dictionary<string, SerializedProperty>();

        protected void RegisterSerializedProperty<T, TProp>(Expression<Func<T, TProp>> propertyExpression, IChangeListener listener = null) where T : EntityBase
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            var propertyType = (memberExpression.Member as PropertyInfo).PropertyType;
            Func<T, TProp> getter = propertyExpression.Compile();
            var propertyName = memberExpression.Member.Name;
            _serializedProperties.Add(propertyName, new SerializedProperty(propertyName, propertyType, listener));
        }

        protected T GetSerializedProperty<T>([CallerMemberName]string property = null)
        {
            string stringValue = GetProperty<string>(property);
            SerializedProperty serializedProperty = _serializedProperties[property];
            if (serializedProperty.DeserializedObject != null)
            {
                return (T)serializedProperty.DeserializedObject;
            }
            else if (stringValue != null)
            {
                Serializer ser = new Serializer();
                T obj = ser.DeserializeFromString<T>(stringValue);
                serializedProperty.DeserializeObjectOnLoad(obj);
                return obj;
            }
            else
            {
                return default(T);
            }
        }

        protected void SetSerializedProperty<T>(T value, [CallerMemberName]string property = null)
        {
            SerializedProperty serializedProperty = _serializedProperties[property];
            serializedProperty.DeserializedObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="InvalidOperationException">When the entity has already been attached to a different Model or the Parent is attached to a different Model</exception>
        internal virtual void AttachToModel(BudgetModel model)
        {
            if (_isAttached && _model == model)
                return;

            if (_model != null && _model != model)
                throw new InvalidOperationException("This entity has already been attached to a model.  You cannot attach it again.");

            _isAttached = true;
            _model = model;

            var parent = this.Parent;
            if (parent != null && parent.Model != model)
            {
                _isAttached = false;
                _model = null;
                throw new InvalidOperationException("Parent and Child must be attached to the same model");
            }

            model.EnsureIdentityTracked(this);

            foreach (IEntityCollection childCollection in _childEntities)
            {
                childCollection.AttachToModel(model);
            }
        }

        internal T RegisterChildEntityCollection<T>(T childEntityCollection) where T : IEntityCollection
        {
            _childEntities.Add(childEntityCollection);
            return childEntityCollection;
        }

        internal SubEntityCollection<T> RegisterSubEntityCollection<T>(SubEntityCollection<T> subEntityCollection) where T : SubEntity
        {
            _subEntities.Add(typeof(T).Name, subEntityCollection);
            return subEntityCollection;
        }

        private List<IEntityCollection> _childEntities;

        internal Dictionary<string, ISubEntityCollection> _subEntities;


        internal void rebuildEntityData(IEnumerable<FieldChangeEvent> events)
        {
            notifyAllPropertiesChanging();

            _entityData.Clear();
            ReplayEvents(events);

            notifyAllPropertiesChanged();
        }

        protected void notifyAllPropertiesChanging()
        {
            foreach (var prop in _entityData)
            {
                RaisePropertyChanging(prop.Key);
            }
        }

        protected void notifyAllPropertiesChanged()
        {
            foreach (var prop in _entityData)
            {
                RaisePropertyChanged(prop.Key);
            }
        }

        internal virtual void RebuildEntity(IEnumerable<ModelEvent> events)
        {
            _entityData = new Dictionary<string, object>();
            var fieldChangeEvents = events.Where(e => e is FieldChangeEvent).Select(e => (FieldChangeEvent)e);
            ReplayEvents(fieldChangeEvents);
        }

        internal virtual void ReplayEvents(IEnumerable<FieldChangeEvent> events)
        {
            var changedProperties = events.SelectMany(e => e.Changes.Keys).Distinct().ToList();
            foreach (var prop in changedProperties)
                RaisePropertyChanging(prop);

            foreach (var evt in events)
            {
                foreach (var change in evt.Changes)
                {
                    object previousValue = null;
                    _entityData.TryGetValue(change.Key, out previousValue);

                    _entityData[change.Key] = change.Value.NewValue;
                    SerializedProperty serializedProperty = null;
                    if (_serializedProperties.TryGetValue(change.Key, out serializedProperty))
                    {
                        serializedProperty.ForceRefresh();
                    }
                    OnReplayChange(change.Key, previousValue, change.Value);
                }
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

        public void CancelCurrentChanges()
        {
            if (CurrentEvent is EntityCreatedEvent)
                throw new InvalidOperationException("You cant cancel the Entity Created Event");

            var changes = CurrentEvent.Changes;
            foreach (var change in changes)
            {
                _entityData[change.Key] = change.Value.PreviousValue;
                OnCancelChange(change.Key, change.Value);
            }
            notifyAllPropertiesChanged();

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
                var parent = change.TypedPreviousValue.Resolve<EntityBase>(_model);
                var canceledParent = change.TypedNewValue.Resolve<EntityBase>(_model);
                canceledParent.RemoveReferenceToChild(this);
                parent.ForceReferenceToChild(this);
            }
        }

        protected Dictionary<string, object> _entityData = new Dictionary<string, object>();

        protected T GetProperty<T>([CallerMemberName]string property = null)
        {
            object value;
            if (_entityData.TryGetValue(property, out value))
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }

        protected void SetProperty<T>(T value, [CallerMemberName]string property = null)
        {
            RaisePropertyChanging(property);

            T oldValue = GetProperty<T>(property);

            _entityData[property] = value;

            T newValue = value;

            if (_validationEnabled)
            {
                ValidateProperty(property);
                ValidateDependencies(property);
            }

            RaisePropertyChanged(property);
            FieldChange change = FieldChange.Create(oldValue, newValue);
            CurrentEvent.AddChange(property, change);
        }

        protected T ResolveEntityReference<T>([CallerMemberName]string property = null) where T : EntityBase
        {
            EntityReference reference = GetProperty<EntityReference>(property);
            if (reference == null)
                return null;

            if (reference.IsReferenceResolved(_model))
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
                    return reference.Resolve<T>(_model);
                }
                else
                    throw new InvalidOperationException("Cannot Resolve an Unresolved EntityReference when the reference entity is not attached to a model");
            }
        }

        protected void SetEntityReference<T>(T value, [CallerMemberName]string property = null) where T : EntityBase
        {
            if (value == null)
            {
                SetProperty<EntityReference>(null, property);
                return;
            }

            if (this.IsAttached && value.IsAttached && (_model != value.Model))
                throw new InvalidOperationException("You cannot set an entity reference to an Entity attached to a different model");

            //Entity may not be attached to a model yet but it references an entity from this model, when the entity becomes 
            //attached if the model doesn't match it will throw an exception
            if (_model == null && value.IsAttached)
                _model = value.Model;

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

        IEnumerable<ModelEvent> IHasChanges.GetAndSaveChanges()
        {
            return this.GetAndSaveChanges();
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
        #endregion
    }
}
