using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace OpenBudget.Util.Collections
{
    public class TransformingObservableCollection<TSource, TTransformed> : IList<TTransformed>, INotifyCollectionChanged, IDisposable where TTransformed : class
    {
        private IReadOnlyList<TSource> _sourceCollection;
        private INotifyCollectionChanged _collectionChanged;
        private Dictionary<TSource, TTransformed> _mapping;
        private List<TTransformed> _transformedCollection;
        Func<TSource, TTransformed> _onAddAction;
        Action<TTransformed> _onRemovedAction;
        private bool _listenForChanges;

        public TransformingObservableCollection(
            IReadOnlyList<TSource> sourceCollection,
            Func<TSource, TTransformed> onAddAction,
            Action<TTransformed> onRemovedAction)
            : this(sourceCollection, onAddAction, onRemovedAction, null, null, false)
        {
        }

        public TransformingObservableCollection(IReadOnlyList<TSource> sourceCollection, Func<TSource, TTransformed> onAddAction, Action<TTransformed> onRemovedAction, Predicate<TSource> predicate, Comparison<TTransformed> comparison, bool listenForChanges)
        {
            _listenForChanges = listenForChanges;
            _predicate = predicate;
            _comparison = comparison;
            if (_comparison != null)
            {
                _comparer = Comparer<TTransformed>.Create(_comparison);
            }

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

        public event PropertyChangedEventHandler ItemPropertyChanged;

        private void InitializeCollection()
        {
            EnsureItems();
            _collectionChanged.CollectionChanged += SourceCollection_CollectionChanged;
        }

        private void ResetCollection()
        {
            //Cleanup Mapping
            HashSet<TSource> sources = new HashSet<TSource>(_sourceCollection);
            foreach (var mapping in _mapping.ToList())
            {
                if (!sources.Contains(mapping.Key))
                {
                    RemoveSource(mapping.Key);
                }
            }

            //Ensure items are created and filters applied
            EnsureItems();

            //Force a reordering of the collection
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
        private Predicate<TSource> _predicate = null;

        public void Sort<TKey>(Func<TTransformed, TKey> orderFunc)
        {
            IComparer<TKey> comparer = Comparer<TKey>.Default;
            Comparison<TTransformed> comparison = (x, y) => comparer.Compare(orderFunc(x), orderFunc(y));

            Sort(comparison);
        }

        public void SortDescending<TKey>(Func<TTransformed, TKey> orderFunc)
        {
            IComparer<TKey> comparer = Comparer<TKey>.Default;
            Comparison<TTransformed> comparison = (x, y) => -1 * comparer.Compare(orderFunc(x), orderFunc(y));

            Sort(comparison);
        }

        public void Sort(Comparison<TTransformed> comparison)
        {
            _comparison = comparison;
            _comparer = Comparer<TTransformed>.Create(_comparison);

            _transformedCollection.Sort(_comparison);
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Checks to make sure all items are created, if a filter is applied it checks the predicate
        /// before creating and also checks if any items need to be removed
        /// </summary>
        private void EnsureItems()
        {
            if (_predicate == null)
            {
                foreach (var source in _sourceCollection)
                {
                    TTransformed transformed = null;
                    if (!_mapping.TryGetValue(source, out transformed))
                    {
                        AddSource(source);
                    }
                }
            }
            else
            {
                foreach (var source in _sourceCollection)
                {
                    TTransformed transformed = null;
                    if (_mapping.TryGetValue(source, out transformed))
                    {
                        if (!_predicate(source))
                        {
                            RemoveSource(source);
                        }
                    }
                    else
                    {
                        if (_predicate(source))
                        {
                            AddSource(source);
                        }
                    }
                }
            }
        }

        public void FilterSource(Predicate<TSource> predicate)
        {
            _predicate = predicate;
            EnsureItems();
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
                }
            }

            if (e.OldItems != null)
            {
                foreach (TSource source in e.OldItems)
                {
                    RemoveSource(source);
                }
            }
        }

        private TTransformed AddSource(TSource source)
        {
            if (_predicate != null && !_predicate(source))
            {
                return null;
            }

            var transformed = _onAddAction(source);

            _mapping.Add(source, transformed);
            if (_comparer != null)
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
                CollectionChanged?.Invoke(this, args);
            }

            if (_listenForChanges && transformed is INotifyPropertyChanged propChanged)
            {
                propChanged.PropertyChanged += Item_PropertyChanged;
            }

            return transformed;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ItemPropertyChanged?.Invoke(sender, e);
        }

        private TTransformed RemoveSource(TSource source)
        {
            TTransformed transformed = null;
            if (!_mapping.TryGetValue(source, out transformed))
            {
                throw new InvalidOperationException();
            }

            if (_listenForChanges && transformed is INotifyPropertyChanged propChanged)
            {
                propChanged.PropertyChanged -= Item_PropertyChanged;
            }

            TTransformed[] removedItems = new TTransformed[] { transformed };
            int index = _transformedCollection.IndexOf(transformed);

            _transformedCollection.Remove(transformed);
            _mapping.Remove(source);
            _onRemovedAction(transformed);

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index);
            CollectionChanged?.Invoke(this, args);

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

        public bool IsReadOnly => true;

        TTransformed IList<TTransformed>.this[int index]
        {
            get { return _transformedCollection[index]; }
            set { throw new NotImplementedException(); }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            _collectionChanged.CollectionChanged -= SourceCollection_CollectionChanged;
            _transformedCollection.Clear();
            foreach (var transformed in _mapping.Values)
            {
                if (_listenForChanges && transformed is INotifyPropertyChanged propChanged)
                {
                    propChanged.PropertyChanged -= Item_PropertyChanged;
                }
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
