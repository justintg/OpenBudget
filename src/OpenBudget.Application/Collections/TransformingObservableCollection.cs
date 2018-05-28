using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace OpenBudget.Application.Collections
{
    public class TransformingObservableCollection<TSource, TTransformed> : IList<TTransformed>, INotifyCollectionChanged, IDisposable where TTransformed : class
    {
        private IReadOnlyList<TSource> _sourceCollection;
        private INotifyCollectionChanged _collectionChanged;
        private Dictionary<TSource, TTransformed> _mapping;
        private List<TTransformed> _transformedCollection;
        Func<TSource, TTransformed> _onAddAction;
        Action<TTransformed> _onRemovedAction;

        public TransformingObservableCollection(IReadOnlyList<TSource> sourceCollection, Func<TSource, TTransformed> onAddAction, Action<TTransformed> onRemovedAction)
        {
            _sourceCollection = sourceCollection;
            if (!(_sourceCollection is INotifyCollectionChanged collectionChanged))
            {
                throw new ArgumentException(nameof(sourceCollection));
            }
            _collectionChanged = collectionChanged;
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

            _collectionChanged.CollectionChanged += SourceCollection_CollectionChanged;
        }

        private void ResetCollection()
        {
            foreach (var mapping in _mapping.ToList())
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
            if (_comparison != null)
            {
                _transformedCollection.Sort(_comparison);
            }
            else
            {
                _transformedCollection.Clear();
                foreach (var source in _sourceCollection)
                {
                    TTransformed transformed;
                    if (_mapping.TryGetValue(source, out transformed))
                    {
                        _transformedCollection.Add(transformed);
                    }
                }
            }
        }

        private Comparison<TTransformed> _comparison = null;
        private IComparer<TTransformed> _comparer = null;
        private Predicate<TTransformed> _predicate = null;

        public void Sort<TKey>(Func<TTransformed, TKey> orderFunc)
        {
            IComparer<TKey> comparer = Comparer<TKey>.Default;
            _comparison = (x, y) => comparer.Compare(orderFunc(x), orderFunc(y));
            _comparer = Comparer<TTransformed>.Create(_comparison);

            _transformedCollection.Sort(_comparison);
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, args);
        }

        public void SortDescending<TKey>(Func<TTransformed, TKey> orderFunc)
        {
            IComparer<TKey> comparer = Comparer<TKey>.Default;
            _comparison = (x, y) => -1 * comparer.Compare(orderFunc(x), orderFunc(y));

            _transformedCollection.Sort(_comparison);
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, args);
        }

        public void Filter(Predicate<TTransformed> predicate)
        {
            _predicate = predicate;
            _transformedCollection = null;
            foreach (var source in _sourceCollection)
            {
                TTransformed transformed = default(TTransformed);
                if (_mapping.TryGetValue(source, out transformed))
                {
                    if (!predicate(transformed))
                    {
                        _mapping.Remove(source);
                        _onRemovedAction(transformed);
                    }
                }
                else
                {
                    transformed = _onAddAction(source);
                    if (predicate(transformed))
                    {
                        _mapping.Add(source, transformed);
                    }
                    else
                    {
                        _onRemovedAction(transformed);
                    }
                }
            }

            _transformedCollection = _mapping.Values.ToList();
            ReorderCollection();
        }

        private void SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetCollection();
                return;
            }
            List<TTransformed> newItems = new List<TTransformed>();
            List<Tuple<int, TTransformed>> removedItems = new List<Tuple<int, TTransformed>>();
            if (e.NewItems != null)
            {
                foreach (TSource source in e.NewItems)
                {
                    TTransformed newTransformed = AddSource(source);
                    if (newTransformed != null)
                    {
                        newItems.Add(newTransformed);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (TSource source in e.OldItems)
                {
                    var transformed = _mapping[source];
                    int index = _transformedCollection.IndexOf(transformed);
                    removedItems.Add((index, RemoveSource(source)).ToTuple());
                }
            }

            if (newItems.Count > 0 && removedItems.Count > 0)
            {
                throw new InvalidOperationException();
            }
            else if (newItems.Count > 0)
            {
                
            }
            else if (removedItems.Count == 1)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems.Select(t => t.Item2).ToList(), removedItems[0].Item1);
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
            if (_predicate != null && !_predicate(transformed))
            {
                _onRemovedAction(transformed);
                return null;
            }

            _mapping.Add(source, transformed);
            if (_comparison != null)
            {
                int index = _transformedCollection.BinarySearch(transformed, _comparer);
                if (index < 0)
                    index = ~index;

                _transformedCollection.Insert(index, transformed);
                TTransformed[] newItems = new TTransformed[] { transformed };
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToList(), index);
                CollectionChanged?.Invoke(this, args);
            }
            else
            {
                _transformedCollection.Add(transformed);
                TTransformed[] newItems = new TTransformed[] { transformed };
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.ToList());
            }

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

        public TTransformed this[int index]
        {
            get
            {
                return _transformedCollection[index];
            }
        }

        public int Count => _transformedCollection.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        TTransformed IList<TTransformed>.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            _collectionChanged.CollectionChanged -= SourceCollection_CollectionChanged;
            _transformedCollection.Clear();
            foreach (var transformed in _mapping.Values)
            {
                _onRemovedAction(transformed);
            }
            _mapping.Clear();
            _collectionChanged = null;
            _sourceCollection = null;
            _transformedCollection = null;
            _mapping = null;
        }

        IEnumerator<TTransformed> IEnumerable<TTransformed>.GetEnumerator()
        {
            return _transformedCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _transformedCollection.GetEnumerator();
        }

        public int IndexOf(TTransformed item) => _transformedCollection.IndexOf(item);

        public void Insert(int index, TTransformed item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(TTransformed item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(TTransformed item) => _transformedCollection.Contains(item);

        public void CopyTo(TTransformed[] array, int arrayIndex) => _transformedCollection.CopyTo(array, arrayIndex);

        public bool Remove(TTransformed item)
        {
            throw new NotImplementedException();
        }
    }
}
