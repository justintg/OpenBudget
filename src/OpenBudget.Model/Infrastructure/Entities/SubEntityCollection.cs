using OpenBudget.Model.Event;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using OpenBudget.Model.Util;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public interface ISubEntityCollection : IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent>
    {
        IEnumerable<ModelEvent> GetChanges();
        void CancelCurrentChanges();
    }

    public class SubEntityCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, ISubEntityCollection where T : EntityBase
    {
        internal SubEntityCollection(EntityBase parent, Func<T> itemInitializer)
        {
            _itemIntializer = itemInitializer;
        }

        private Dictionary<string, T> _identityMap = new Dictionary<string, T>();
        private ObservableCollection<T> _collection = new ObservableCollection<T>();
        private List<T> _pendingAdds = new List<T>();
        private Func<T> _itemIntializer;

        public T this[int index] => _collection[index];

        public int Count => _collection.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _collection.CollectionChanged += value; }
            remove { _collection.CollectionChanged -= value; }
        }

        public T Create()
        {
            var entity = _itemIntializer();
            _identityMap.Add(entity.EntityID, entity);
            _collection.Add(entity);
            _pendingAdds.Add(entity);
            return entity;
        }

        internal void Clear()
        {
            _identityMap.Clear();
            _collection.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        public IEnumerable<ModelEvent> GetChanges()
        {
            foreach (var entity in _collection)
            {
                foreach (var change in entity.GetAndSaveChanges())
                {
                    yield return change;
                }
            }

            //At this point we assume changes are commited and clear pending changes
            _pendingAdds.Clear();
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
            //entity.AttachToModel(_model);
            _identityMap[entity.EntityID] = entity;
            _collection.Add(entity);
        }

        public void Handle(EntityUpdatedEvent message)
        {
            T entity = this.GetEntity(message.EntityID);
            entity.ReplayEvents(message.Yield());
        }

        public void CancelCurrentChanges()
        {
            foreach (var pendingAdd in _pendingAdds)
            {
                _collection.Remove(pendingAdd);
                _identityMap.Remove(pendingAdd.EntityID);
            }

            _pendingAdds.Clear();

            foreach (var subEntity in _collection)
            {
                subEntity.CancelCurrentChanges();
            }
        }
    }
}
