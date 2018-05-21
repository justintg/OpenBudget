using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace OpenBudget.Application.Collections
{
    public class TransformingObservableCollection<TSource, TTransformed> : IReadOnlyList<TTransformed>, INotifyCollectionChanged, IDisposable
    {
        private ObservableCollection<TSource> _sourceCollection;
        private Dictionary<TSource, TTransformed> _mapping;
        private List<TTransformed> _transformedCollection;
        Func<TSource, TTransformed> _onAddAction;
        Action<TTransformed> _onRemovedAction;

        public TransformingObservableCollection(ObservableCollection<TSource> sourceCollection, Func<TSource, TTransformed> onAddAction, Action<TTransformed> onRemovedAction)
        {
            _sourceCollection = sourceCollection;
            _transformedCollection = new List<TTransformed>();
            _mapping = new Dictionary<TSource, TTransformed>();
            _onAddAction = onAddAction;
            _onRemovedAction = onRemovedAction;
            InitializeCollection();
        }

        private void InitializeCollection()
        {
            foreach (var source in _sourceCollection)
            {
                AddSource(source);
            }

            _sourceCollection.CollectionChanged += SourceCollection_CollectionChanged;
        }

        private void ResetCollection()
        {
            foreach (var mapping in _mapping)
            {
                if (!_sourceCollection.Contains(mapping.Key))
                {
                    RemoveSource(mapping.Key);
                }
            }

            foreach (var source in _sourceCollection)
            {
                TTransformed transformed;
                if (!_mapping.TryGetValue(source, out transformed))
                {
                    AddSource(source);
                }
            }


            ReorderCollection();
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, args);
        }

        private void ReorderCollection()
        {
            _transformedCollection.Clear();
            foreach (var source in _sourceCollection)
            {
                TTransformed transformed;
                if (_mapping.TryGetValue(source, out transformed))
                {
                    _transformedCollection.Add(transformed);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetCollection();
                return;
            }
            List<TTransformed> newItems = new List<TTransformed>();
            List<TTransformed> removedItems = new List<TTransformed>();
            if (e.NewItems != null)
            {
                foreach (TSource source in e.NewItems)
                {
                    newItems.Add(AddSource(source));
                }
            }

            if (e.OldItems != null)
            {
                foreach (TSource source in e.OldItems)
                {
                    removedItems.Add(RemoveSource(source));
                }
            }

            if (newItems.Count > 0 && removedItems.Count > 0)
            {
                throw new InvalidOperationException();
            }
            else if (newItems.Count > 0)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems);
                CollectionChanged?.Invoke(this, args);
            }
            else if (removedItems.Count > 0)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems);
                CollectionChanged?.Invoke(this, args);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private TTransformed AddSource(TSource source)
        {
            var transformed = _onAddAction(source);
            _mapping.Add(source, transformed);
            _transformedCollection.Add(transformed);
            return transformed;
        }

        private TTransformed RemoveSource(TSource source)
        {
            TTransformed transformed = default(TTransformed);
            if (!_mapping.TryGetValue(source, out transformed))
            {
                throw new InvalidOperationException();
            }

            _transformedCollection.Remove(transformed);
            _mapping.Remove(source);
            _onRemovedAction(transformed);
            return transformed;
        }

        public TTransformed this[int index] => _transformedCollection[index];

        public int Count => _transformedCollection.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            _mapping.Clear();
            foreach (var transformed in _transformedCollection)
            {
                _onRemovedAction(transformed);
            }
        }

        IEnumerator<TTransformed> IEnumerable<TTransformed>.GetEnumerator()
        {
            return _transformedCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _transformedCollection.GetEnumerator();
        }
    }
}
