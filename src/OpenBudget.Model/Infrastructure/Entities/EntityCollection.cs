using OpenBudget.Model.Entities;
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

    public class EntityCollection<T>
        : IList<T>,
        IReadOnlyList<T>,
        INotifyCollectionChanged,
        IEntityCollection,
        IHandler<EntityCreatedEvent>,
        IHandler<EntityUpdatedEvent>
        where T : EntityBase
    {
        private EntityBase _parent;

        private ObservableCollection<T> _loadedEntities = new ObservableCollection<T>();

        internal ObservableCollection<T> GetInternalCollection() => _loadedEntities;

        private HashSet<string> _loadedEntityIds = new HashSet<string>();
        private Dictionary<string, T> _knownMaterializedChildren = new Dictionary<string, T>();

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
            var entities = repository.GetEntitiesByParent(_parent.GetType().Name, _parent.EntityID, _parent.LookupRoot).Where(e => !e.IsDeleted).ToList();
            ForceResolveParent(entities);
            ReplaceChildrenWithKnownChildren(entities);

            if (entities.Count > 0)
                AddRangeInternal(entities);

            IsLoaded = true;
        }

        private void ForceResolveParent(List<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.ForceResolveEntityReference(nameof(EntityBase.Parent), _parent);
            }
        }

        private void ReplaceChildrenWithKnownChildren(List<T> entities)
        {
            foreach (var child in entities.ToList())
            {
                if (_knownMaterializedChildren.TryGetValue(child.EntityID, out T knownChild))
                {
                    int index = entities.IndexOf(child);
                    entities[index] = knownChild;
                }
            }

            _knownMaterializedChildren.Clear();
        }

        private void AddRangeInternal(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                AddInternal(entity);
            }
        }

        internal List<T> GetPendingAdds()
        {
            return _loadedEntities.Where(e =>
            {
                return e.CurrentEvent.Changes.ContainsKey(nameof(EntityBase.Parent));
            }
            ).ToList();
        }

        private void AddInternal(T entity)
        {
            if (_loadedEntityIds.Contains(entity.EntityID))
                throw new InvalidOperationException("Cannot add an entity that is already in the collection.");

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
            _loadedEntityIds.Add(entity.EntityID);

            if (entity.LookupRoot != _parent.LookupRoot)
            {
                entity.LookupRoot = _parent.LookupRoot;
            }
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
        }

        /*internal void BeforeSaveChanges()
        {
            foreach (EntityBase entity in this)
            {
                entity.BeforeSaveChanges();
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

        public int Count => IsLoaded ? _loadedEntities.Count : throw new InvalidBudgetActionException("You cannot access this property while the collection is not loaded.");

        public bool IsReadOnly => false;



        public T this[int index] { get => _loadedEntities[index]; set => throw new NotSupportedException(); }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _loadedEntities.CollectionChanged += value; }
            remove { _loadedEntities.CollectionChanged -= value; }
        }

        private void HandleDeletedEvent(EntityUpdatedEvent message)
        {
            if (!IsLoaded)
                return;

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
            if (!IsLoaded)
                return;

            FieldChange fieldChange;
            if (!message.Changes.TryGetValue("Parent", out fieldChange)) return;

            EntityReference parent = (EntityReference)fieldChange.NewValue;
            EntityReference oldParent = (EntityReference)fieldChange.PreviousValue;

            if (parent.EntityID == _parent.EntityID)
            {
                if (_loadedEntityIds.Contains(message.EntityID)) return;

                T entity = _model.FindRepository<T>().GetEntity(message.EntityID);

                AddInternal(entity);
            }
            else if (parent.EntityID != _parent.EntityID && oldParent != null && oldParent.EntityID == _parent.EntityID)
            {
                T entity = _loadedEntities.SingleOrDefault(e => e.EntityID == message.EntityID);

                if (entity == null) return;

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
            _loadedEntityIds.Remove(child.EntityID);
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
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            return _loadedEntities.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            _loadedEntities.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            throw new InvalidBudgetActionException("You cannot manually remove an item from this collection.  You must call EntityBase.Delete() or add the entity to a different collection");
        }

        public void Add(T item)
        {
            OnBeforeAddItem(item);
            EnsureDeletedFromPreviousParent(item);
            AddInternal(item);
            EnsureAddedEntityRegisteredForChanges(item);
            OnAfterAddItem(item);
        }

        protected virtual void OnBeforeAddItem(T item)
        {
            if (item is ISortableEntity sortable)
            {
                OnBeforeAddSortable(item, sortable);
            }
        }

        private void OnBeforeAddSortable(T item, ISortableEntity sortable)
        {
            if (item.IsAttached)  //Item is being moved
            {
                var parentCollection = sortable.GetParentCollection();
                if (parentCollection == null || !parentCollection.IsLoaded)
                {
                    throw new InvalidBudgetActionException("You cannot move an entity when the previous parent collection is not loaded.");
                }

                var sortedChildren = parentCollection.EnumerateChildren().Where(c => c != item).Select(c => (ISortableEntity)c).ToList();
                for (int i = 0; i < sortedChildren.Count; i++)
                {
                    sortedChildren[i].ForceSetSortOrder(i);
                }
            }
        }

        bool IEntityCollection.IsLoaded => IsLoaded;

        protected virtual void OnAfterAddItem(T item)
        {
        }

        private void EnsureDeletedFromPreviousParent(T item)
        {
            var reference = item.GetProperty<EntityReference>(nameof(EntityBase.Parent));
            if (reference != null && reference.ReferencedEntity != null)
            {
                reference.ReferencedEntity.RemoveReferenceToChild(item);
            }
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
            throw new InvalidBudgetActionException();
        }

        public bool Contains(T item)
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            return _loadedEntities.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            _loadedEntities.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            throw new InvalidBudgetActionException("You cannot manually remove an item from this collection.  You must call EntityBase.Delete() or add the entity to a different collection");
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            return (_loadedEntities as IEnumerable<T>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            return _loadedEntities.GetEnumerator();
        }

        IEnumerable<EntityBase> IEntityCollection.EnumerateUnattachedEntities()
        {
            return _loadedEntities.Where(e => e.SaveState == EntitySaveState.Unattached);
        }

        void IEntityCollection.EnsureContainsMaterializedChild(EntityBase child)
        {
            if (_knownMaterializedChildren.ContainsKey(child.EntityID))
            {
                throw new InvalidBudgetActionException("More than one copy of an entity cannot be registered as a known child.");
            }

            _knownMaterializedChildren.Add(child.EntityID, (T)child);
        }

        IEnumerable<EntityBase> IEntityCollection.EnumerateChildren()
        {
            if (!IsLoaded)
                throw new InvalidBudgetActionException("This operation is not supported when the EntityCollection is not loaded.");

            foreach (var child in this)
            {
                yield return child;
            }
        }

        IList<EntityBase> IEntityCollection.GetPendingAdds()
        {
            return GetPendingAdds().Select(c => (EntityBase)c).ToList();
        }

        int IEntityCollection.IndexOf(EntityBase child)
        {
            if (child is T typedChild)
            {
                return this.IndexOf(typedChild);
            }

            throw new InvalidOperationException();
        }
    }
}
