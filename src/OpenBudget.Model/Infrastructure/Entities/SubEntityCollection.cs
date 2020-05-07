using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Messaging;
using OpenBudget.Model.Infrastructure.UnitOfWork;
using OpenBudget.Model.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface ISubEntityCollection : IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent>
    {
        IEnumerable<EventSaveInfo> GetChanges();
        void CancelCurrentChanges();
        void DeleteChild(SubEntity childEntity);
        void LoadCollection();
    }

    public class SubEntityCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, ISubEntityCollection where T : SubEntity
    {
        internal SubEntityCollection(EntityBase parent, Func<T> itemInitializer)
        {
            _parent = parent;
            _itemIntializer = itemInitializer;
            _collection.CollectionChanged += (sender, e) =>
            {
                RaiseCollectionChanged(e);
            };
        }

        private Dictionary<string, T> _identityMap = new Dictionary<string, T>();
        private ObservableCollection<T> _collection = new ObservableCollection<T>();
        private List<T> _pendingAdds = new List<T>();
        private List<T> _pendingDeletes = new List<T>();
        private Func<T> _itemIntializer;
        private EntityBase _parent;

        public T this[int index] => _collection[index];

        public int Count => _collection.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public T Create()
        {
            var entity = _itemIntializer();
            _identityMap.Add(entity.EntityID, entity);
            _collection.Add(entity);
            _pendingAdds.Add(entity);
            entity.Parent = _parent;
            _parent.SubEntityCreated(entity);
            return entity;
        }

        internal void Clear()
        {
            foreach (var entity in _collection.ToList())
            {
                entity.Delete();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        public IEnumerable<EventSaveInfo> GetChanges()
        {
            List<EventSaveInfo> changes = new List<EventSaveInfo>();
            foreach (var entity in _collection)
            {
                foreach (var change in entity.GetAndSaveChanges())
                {
                    changes.Add(change);
                }
            }

            foreach (var entity in _pendingDeletes)
            {
                foreach (var change in entity.GetAndSaveChanges())
                {
                    changes.Add(change);
                }
            }

            //At this point we assume changes are commited and clear pending changes
            _pendingAdds.Clear();
            _pendingDeletes.Clear();
            return changes;
        }

        public virtual T GetEntity(string entityID)
        {
            T entity;
            if (_identityMap.TryGetValue(entityID, out entity))
            {
                if (entity.IsDeleted)
                    return null;

                return entity;
            }

            return null;
        }

        public void Handle(EntityCreatedEvent message)
        {
            if (_identityMap.ContainsKey(message.EntityID)) return;

            var constructor =
            typeof(T)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(c =>
            {
                var parameters = c.GetParameters().ToList();
                if (parameters.Count == 1 && parameters.Single().ParameterType == typeof(EntityCreatedEvent))
                {
                    return true;
                }

                return false;
            });

            T entity = (T)constructor.Invoke(new object[] { message });
            //entity.AttachToModel(_parent.Model);
            _identityMap[entity.EntityID] = entity;
            _collection.Add(entity);
            entity.Parent = _parent;
        }

        public void Handle(EntityUpdatedEvent message)
        {
            T entity = this.GetEntity(message.EntityID);
            entity.ReplayEvents(message.Yield());

            if (message.Changes.ContainsKey(nameof(SubEntity.IsDeleted)) && entity.IsDeleted)
            {
                _collection.Remove(entity);
            }
        }

        public void CancelCurrentChanges()
        {
            foreach (var pendingAdd in _pendingAdds)
            {
                _collection.Remove(pendingAdd);
                _identityMap.Remove(pendingAdd.EntityID);
            }

            foreach (var pendingDelete in _pendingDeletes)
            {
                _collection.Add(pendingDelete);
            }

            _pendingDeletes.Clear();
            _pendingAdds.Clear();

            foreach (var subEntity in _collection)
            {
                subEntity.CancelCurrentChanges();
            }
        }

        void ISubEntityCollection.DeleteChild(SubEntity childEntity)
        {
            T child = childEntity as T;
            if (child == null) throw new InvalidOperationException();

            if (_pendingAdds.Contains(child))
            {
                _pendingAdds.Remove(child);
                _collection.Remove(child);
            }
            else
            {
                _pendingDeletes.Add(child);
                _collection.Remove(child);
            }
        }

        void ISubEntityCollection.LoadCollection()
        {
            BudgetModel model = _parent.Model;
            var repo = model.FindSubEntityRepository<T>();
            var subEntities = repo.GetEntitiesByParent(_parent.GetType().Name, _parent.EntityID);
            foreach (var subEntity in subEntities)
            {
                _identityMap.Add(subEntity.EntityID, subEntity);
                subEntity.Parent = _parent;
                _collection.Add(subEntity);
            }
        }
    }
}
