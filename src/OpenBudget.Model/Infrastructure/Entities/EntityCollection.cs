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
        UnattachedRegistered,
        AttachedUnloaded,
        AttachedLoaded
    }

    public class EntityCollection<T> : IList<T>, IReadOnlyList<T>, INotifyCollectionChanged, IEntityCollection, IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent> where T : EntityBase
    {
        private EntityBase _parent;

        private List<T> _loadedEntities = new List<T>();

        private List<Tuple<T, int>> _pendingDeletions;

        public EntityCollectionState CollectionState { get; protected set; }

        public EntityCollection(EntityBase parent)
        {
            _parent = parent;
            _pendingDeletions = new List<Tuple<T, int>>();
            CollectionState = EntityCollectionState.Unattached;
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
                CollectionState = EntityCollectionState.AttachedUnloaded;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private bool _isBuilding = false;

        /*private void EntityCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isBuilding)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (T item in e.NewItems)
                {
                    if (_model != null)
                        item.AttachToModel(_model);

                    if (item.Parent != null)
                    {
                        item.Parent.RemoveReferenceToChild(item);
                    }

                    item.Parent = _parent;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (T item in e.NewItems)
                {
                    if (_model != null)
                        item.AttachToModel(_model);

                    item.Parent = null;
                }
            }
        }*/

        private IMessenger<ModelEvent> _messenger;
        private BudgetModel _model;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="InvalidOperationException">When the <see cref="EntityCollection{T}"/> has already been attached to a different model.</exception>
        public void AttachToModel(BudgetModel model)
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

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void HandleDeletedEvent(EntityUpdatedEvent message)
        {
            FieldChange fieldChange;
            if (message.Changes.TryGetValue("IsDeleted", out fieldChange))
            {
                bool isDeleted = (bool)fieldChange.NewValue;
                if (!isDeleted)
                    return;

                var entity = this.Where(e => e.EntityID == message.EntityID).SingleOrDefault();
                if (entity == null) return;

                try
                {
                    _isBuilding = true;
                    this.Remove(entity);
                }
                finally
                {
                    _isBuilding = false;
                }
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
                try
                {
                    _isBuilding = true;
                    T entity = null;//_generator.GetEntity(message.EntityID);

                    if (entity != null)
                        this.Add(entity);
                }
                finally
                {
                    _isBuilding = false;
                }
            }
            else if (parent.EntityID != _parent.EntityID && oldParent != null && oldParent.EntityID == _parent.EntityID)
            {
                try
                {
                    _isBuilding = true;
                    T entity = null;//_generator.GetEntity(message.EntityID);
                    if (entity != null)
                        this.Remove(entity);
                }
                finally
                {
                    _isBuilding = false;
                }
            }
        }

        private void BuildCollection(Action action)
        {
            try
            {
                _isBuilding = true;
                action();
            }
            finally
            {
                _isBuilding = false;
            }
        }

        void IEntityCollection.ForceRemoveChild(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            BuildCollection(() =>
            {
                this.Remove(typedChild);
            });
        }

        void IEntityCollection.ForceAddChild(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            BuildCollection(() =>
            {
                this.Add(typedChild);
            });
        }

        void IEntityCollection.RequestDeletion(EntityBase child)
        {
            T typedChild = child as T;
            if (typedChild == null) return;

            int childIndex = IndexOf(typedChild);
            _pendingDeletions.Add((typedChild, childIndex).ToTuple());

            BuildCollection(() =>
            {
                this.Remove(typedChild);
            });
        }

        void IEntityCollection.CancelDeletion(EntityBase child)
        {
            Tuple<T, int> deletion = _pendingDeletions.Where(t => t.Item1 == child).FirstOrDefault();
            if (deletion == null) return;

            BuildCollection(() =>
            {
                this.Insert(deletion.Item2, deletion.Item1);
            });
        }

        public IEnumerable<EntityBase> EnumerateUnattachedEntities()
        {
            return _loadedEntities.Where(e => e.SaveState == EntitySaveState.Unattached);
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Add(T item)
        {
            _loadedEntities.Add(item);
            EnsureAddedEntityRegisteredForChanges(item);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item.Yield().ToList());
            RaiseCollectionChanged(args);
        }

        private void EnsureAddedEntityRegisteredForChanges(T entity)
        {
            if (CollectionState == EntityCollectionState.Unattached)
            {
                //Do nothing
            }
            else if (CollectionState == EntityCollectionState.UnattachedRegistered || CollectionState == EntityCollectionState.AttachedLoaded || CollectionState == EntityCollectionState.AttachedUnloaded)
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

        void IEntityCollection.AttachToModel(BudgetModel model)
        {
            throw new NotImplementedException();
        }

        IEnumerable<EntityBase> IEntityCollection.EnumerateUnattachedEntities()
        {
            return _loadedEntities.Where(e => e.SaveState == EntitySaveState.Unattached);
        }

        void IHasChanges.BeforeSaveChanges()
        {
            throw new NotImplementedException();
        }

        IEnumerable<ModelEvent> IHasChanges.GetAndSaveChanges()
        {
            throw new NotImplementedException();
        }
    }
}
