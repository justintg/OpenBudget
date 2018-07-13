using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public class EntityCollection<T> : ObservableCollection<T>, IEntityCollection, IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent> where T : EntityBase
    {
        private EntityBase _parent;

        private List<Tuple<T, int>> _pendingDeletions;

        public EntityCollection(EntityBase parent)
        {
            _parent = parent;
            _pendingDeletions = new List<Tuple<T, int>>();
            this.CollectionChanged += EntityCollection_CollectionChanged;
        }

        private bool _isBuilding = false;

        private void EntityCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
        }

        private EntityGenerator<T> _generator;
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
            _generator = model.FindGenerator<T>();
            _messenger = messenger;

            foreach (var entity in this)
            {
                entity.AttachToModel(model);
            }
        }

        protected override void RemoveItem(int index)
        {
            if (!_isBuilding && _model != null)
                throw new InvalidOperationException("You cannot manually remove an item from this collection.  You must call EntityBase.Delete() or add the entity to a different collection");

            base.RemoveItem(index);
        }

        internal void BeforeSaveChanges()
        {
            foreach (EntityBase entity in this)
            {
                entity.BeforeSaveChanges();
            }
        }

        internal IEnumerable<ModelEvent> GetAndSaveChanges()
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
        }

        void IHasChanges.BeforeSaveChanges()
        {
            this.BeforeSaveChanges();
        }

        IEnumerable<ModelEvent> IHasChanges.GetAndSaveChanges()
        {
            return this.GetAndSaveChanges();
        }

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
                    T entity = _generator.GetEntity(message.EntityID);
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
                    T entity = _generator.GetEntity(message.EntityID);
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
    }
}
