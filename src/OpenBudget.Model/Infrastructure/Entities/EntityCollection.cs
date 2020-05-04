using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Messaging;
using OpenBudget.Model.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public enum EntityCollectionState
    {
        Unattached,
        Attached
    }

    public class EntityCollection<T> : IList<T>, IReadOnlyList<T>, INotifyCollectionChanged, IEntityCollection, IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent> where T : EntityBase
    {
        private EntityBase _parent;

        private ObservableCollection<T> _loadedEntities = new ObservableCollection<T>();

        private List<Tuple<T, int>> _pendingDeletions;

        public EntityCollectionState CollectionState { get; protected set; }
        public bool IsLoaded { get; protected set; }

        internal EntityCollection(EntityBase parent, bool isLoaded = false)
        {
            _parent = parent;
            _pendingDeletions = new List<Tuple<T, int>>();
            CollectionState = EntityCollectionState.Unattached;
            IsLoaded = isLoaded;
            DetermineCollectionState();
        }

        private void DetermineCollectionState()
        {
            if (_parent.SaveState == EntitySaveState.Unattached)
            {
                CollectionState = EntityCollectionState.Unattached;
            }
            else if (_parent.SaveState == EntitySaveState.AttachedNoChanges)
            {
                CollectionState = EntityCollectionState.Attached;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void EnsureCollectionLoaded()
        {
            if (!IsLoaded) LoadCollection();
        }

        public void LoadCollection()
        {
            if (_model == null)
                throw new InvalidOperationException("This EntityCollection has not been attached to a model");

            if (IsLoaded)
                return;

            var repository = _model.FindRepository<T>();
            var entities = repository.GetEntitiesByParent(_parent.GetType().Name, _parent.EntityID);
            AddRangeInternal(entities);

            IsLoaded = true;
        }

        private void AddRangeInternal(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                AddInternal(entity);
            }
        }

        private void AddInternal(T entity)
        {
            if (entity.IsPropertyNull(nameof(EntityBase.Parent)))
            {
                entity.Parent = _parent;
            }
            else
            {
                if (entity.Parent.EntityID != _parent.EntityID)
                {
                    entity.Parent = _parent;
                }
                else
                {
                    entity.ForceResolveEntityReference(nameof(EntityBase.Parent), _parent);
                }
            }
            _loadedEntities.Add(entity);
        }

        private IMessenger<ModelEvent> _messenger;
        private BudgetModel _model;

        void IEntityCollection.AttachToModel(BudgetModel model) => this.AttachToModel(model);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="InvalidOperationException">When the <see cref="EntityCollection{T}"/> has already been attached to a different model.</exception>
        internal void AttachToModel(BudgetModel model)
        {
            if (_model == model)
                return;

            if (_model != null)
                throw new InvalidOperationException("This EntityCollection has already been attached to a model.  You cannot attach it again.");

            _model = model;
            var messenger = model.InternalMessageBus;
            messenger.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, this);
            messenger.RegisterForMessages<EntityCreatedEvent>(typeof(T).Name, this);

            var externalMessenger = model.MessageBus;
            _externalUpdateHandler = new MessageHandler<EntityUpdatedEvent>(e => HandleDeletedEvent(e));
            externalMessenger.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, _externalUpdateHandler);

            this.CollectionState = EntityCollectionState.Attached;

            _messenger = messenger;

            foreach (var entity in this)
            {
                //entity.AttachToModel(model);
            }
        }

        internal void BeforeSaveChanges()
        {
            foreach (EntityBase entity in this)
            {
                entity.BeforeSaveChanges();
            }
        }

        /*internal IEnumerable<ModelEvent> GetAndSaveChanges()
        {
            foreach (EntityBase entity in this)
            {
                var changes = entity.GetAndSaveChanges();
                foreach (var change in changes)
                {
                    yield return change;
                }
            }

            //Allow deleted entities to broadcast their events
            foreach (EntityBase entity in _pendingDeletions.Select(t => t.Item1))
            {
                var changes = entity.GetAndSaveChanges();
                foreach (var change in changes)
                {
                    yield return change;
                }
            }
    }*/

        public void Handle(EntityUpdatedEvent message)
        {
            HandleParentEvent(message, false);
            HandleDeletedEvent(message);
        }

        public void Handle(EntityCreatedEvent message)
        {
            HandleParentEvent(message, true);
        }

        private MessageHandler<EntityUpdatedEvent> _externalUpdateHandler;

        public int Count => _loadedEntities.Count;

        public bool IsReadOnly => false;

        public T this[int index] { get => _loadedEntities[index]; set => throw new NotSupportedException(); }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _loadedEntities.CollectionChanged += value; }
            remove { _loadedEntities.CollectionChanged -= value; }
        }

        private void HandleDeletedEvent(EntityUpdatedEvent message)
        {
            FieldChange fieldChange;
            if (message.Changes.TryGetValue("IsDeleted", out fieldChange))
            {
                bool isDeleted = (bool)fieldChange.NewValue;
                if (!isDeleted)
                    return;

                var entity = _loadedEntities.SingleOrDefault(e => e.EntityID == message.EntityID);
                if (entity == null) return;

                RemoveInternal(entity);
            }
        }

        private void HandleParentEvent(FieldChangeEvent message, bool noRemove)
        {
            FieldChange fieldChange;
            if (!message.Changes.TryGetValue("Parent", out fieldChange)) return;

            EntityReference parent = (EntityReference)fieldChange.NewValue;
            EntityReference oldParent = (EntityReference)fieldChange.PreviousValue;

            if (parent.EntityID == _parent.EntityID)
            {
                T entity = _model.FindRepository<T>().GetEntity(message.EntityID);

                AddInternal(entity);
            }
            else if (parent.EntityID != _parent.EntityID && oldParent != null && oldParent.EntityID == _parent.EntityID)
            {
                T entity = _loadedEntities.Single(e => e.EntityID == message.EntityID);

                RemoveInternal(entity);
            }
        }

        void IEntityCollection.ForceRemoveChild(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            RemoveInternal(typedChild);
        }

        void IEntityCollection.ForceAddChild(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            AddInternal(typedChild);
        }

        void IEntityCollection.RequestDeletion(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            int childIndex = IndexOf(typedChild);
            _pendingDeletions.Add((typedChild, childIndex).ToTuple());

            RemoveInternal(typedChild);
        }

        private void RemoveInternal(T child)
        {
            _loadedEntities.Remove(child);
        }

        void IEntityCollection.CancelDeletion(EntityBase child)
        {
            Tuple<T, int> deletion = _pendingDeletions.Where(t => t.Item1 == child).FirstOrDefault();
            if (deletion == null) return;

            _pendingDeletions.Remove(deletion);

            _loadedEntities.Insert(deletion.Item2, deletion.Item1);
        }

        public IEnumerable<EntityBase> EnumerateUnattachedEntities()
        {
            return _loadedEntities.Where(e => e.SaveState == EntitySaveState.Unattached);
        }

        public int IndexOf(T item)
        {
            if (!IsLoaded)
                throw new InvalidOperationException("This operation is not supported when the EntityCollection is not loaded.");

            return _loadedEntities.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (!IsLoaded)
                throw new InvalidOperationException("This operation is not supported when the EntityCollection is not loaded.");

            _loadedEntities.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("You cannot manually remove an item from this collection.  You must call EntityBase.Delete() or add the entity to a different collection");
        }

        public void Add(T item)
        {
            AddInternal(item);
            EnsureAddedEntityRegisteredForChanges(item);
        }

        private void EnsureAddedEntityRegisteredForChanges(T entity)
        {
            if (CollectionState == EntityCollectionState.Unattached)
            {
                //Do nothing
            }
            else if (CollectionState == EntityCollectionState.Attached)
            {
                if (entity.SaveState == EntitySaveState.Unattached)
                {
                    _model.RegisterHasChanges(entity);
                }
            }
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return _loadedEntities.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException("You cannot manually remove an item from this collection.  You must call EntityBase.Delete() or add the entity to a different collection");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (_loadedEntities as IEnumerable<T>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _loadedEntities.GetEnumerator();
        }

        IEnumerable<EntityBase> IEntityCollection.EnumerateUnattachedEntities()
        {
            return _loadedEntities.Where(e => e.SaveState == EntitySaveState.Unattached);
        }
    }
}
