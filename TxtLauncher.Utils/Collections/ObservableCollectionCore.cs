using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using TxtLauncher.Utils.Helpers;
using TxtLauncher.Utils.Interfaces;

namespace TxtLauncher.Utils.Collections
{
    /// <summary>
    /// Обозреваемая коллекция с возвожностью обновления в UI потоке.
    /// </summary>
    /// <typeparam name="T">Тип элемента в коллекции.</typeparam>
    [Serializable]
    public class ObservableCollectionCore<T> : IList<T>, IReadOnlyList<T>, IList, INotifyPropertyChanged,
        INotifyCollectionChanged, ISupportUpdate, ICloneable
    {
        private readonly List<T> _innerList = new List<T>();

        [field: NonSerialized]
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        [field: NonSerialized] private readonly UpdatingHelper _updHelper;

        private static string IndexerName => Binding.IndexerName;

        /// <summary>
        /// Использовать UI поток для работы с коллекцией.
        /// </summary>
        public bool UseUiThread { get; set; } = true;

        public int Count
        {
            get
            {
                using (_locker.EnterReadSection())
                {
                    return _innerList.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        [field: NonSerialized] public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc />
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Создать экземляр класса <see cref="ObservableCollectionCore{T}" />
        /// </summary>
        public ObservableCollectionCore()
        {
            _updHelper = new UpdatingHelper(endUpdateAction: delegate
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(IndexerName);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            });
        }

        /// <summary>
        /// Создать экземляр класса <see cref="ObservableCollectionCore{T}" /> на основе коллекции <paramref name="source" />
        /// </summary>
        /// <param name="source">Исходная коллекция.</param>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="source" /></exception>
        public ObservableCollectionCore(IEnumerable<T> source) : this()
        {
            if (ReferenceEquals(source, null))
            {
                throw new ArgumentNullException(nameof(source));
            }

            _innerList.AddRange(source);
        }

        /// <summary>
        /// Полностью сделать коллекцию равной указанной.
        /// </summary>
        /// <param name="source">Исходная коллекция.</param>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="source" /></exception>
        public void Assign(IList<T> source)
        {
            if (ReferenceEquals(source, null))
            {
                throw new ArgumentNullException(nameof(source));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterUpgradeableReadSection())
                    {
                        if (_innerList.Count == source.Count && _innerList.SequenceEqual(source))
                        {
                            return;
                        }
                        
                        using (_updHelper.DoUpdate())
                        {
                            using (_locker.EnterWriteSection())
                            {
                                _innerList.Clear();
                                _innerList.AddRange(source);
                            }
                        }
                    }
                },
                UseUiThread);
        }
        
        /// <summary>
        /// Добавить несколько элементов.
        /// </summary>
        /// <param name="items">Добавляемые элементы</param>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="items" /></exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (ReferenceEquals(items, null))
            {
                throw new ArgumentNullException(nameof(items));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            foreach (var item in items)
                            {
                                _innerList.Add(item);
                            }
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Добавить уникальные элементы из <paramref name="items" />
        /// </summary>
        /// <param name="items">Коллекция элементов для добавления</param>
        /// <param name="comparer">Сравниватель элементов</param>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="items" /></exception>
        public void AddRangeUnique(IEnumerable<T> items, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(items, null))
            {
                throw new ArgumentNullException(nameof(items));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterUpgradeableReadSection())
                    {
                        var unique = items.Distinct(comparer).Except(_innerList, comparer).ToList();

                        if (unique.None())
                        {
                            return;
                        }

                        using (_updHelper.DoUpdate())
                        {
                            using (_locker.EnterWriteSection())
                            {
                                _innerList.AddRange(unique);
                            }
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Удалить несколько элементов.
        /// </summary>
        /// <param name="items">Удаляемые элементы.</param>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="items" /></exception>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (ReferenceEquals(items, null))
            {
                throw new ArgumentNullException(nameof(items));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            foreach (var item in items)
                            {
                                _innerList.Remove(item);
                            }
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Отсортировать элементы.
        /// </summary>
        /// <param name="comparer">Компарер элементов.</param>
        public void Sort(IComparer<T> comparer = null)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            _innerList.Sort(comparer);
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Отсортировать элементы.
        /// </summary>
        /// <param name="comparison">Сравниватель элементов</param>
        /// <exception cref="ArgumentNullException">Не указан аргумент <paramref name="comparison" /></exception>
        public void Sort(Comparison<T> comparison)
        {
            if (ReferenceEquals(comparison, null))
            {
                throw new ArgumentNullException(nameof(comparison));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            _innerList.Sort(comparison);
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Отсортировать элементы.
        /// </summary>
        /// <param name="keySelector">Селектор по чему соритровать</param>
        /// <param name="comparer">Сортировщик</param>
        /// <typeparam name="TS">Тип по чему сортировать</typeparam>
        /// <exception cref="ArgumentNullException">Не указан параметр <paramref name="keySelector" /></exception>
        public void Sort<TS>(Func<T, TS> keySelector, IComparer<TS> comparer = null) where TS : IComparable<TS>
        {
            if (ReferenceEquals(keySelector, null))
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            var items = new T[_innerList.Count];
                            _innerList.CopyTo(items);

                            _innerList.Clear();
                            _innerList.AddRange(items.OrderBy(keySelector, comparer));
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Добавить элемент, если такого ещё нет в коллекции.
        /// </summary>
        /// <param name="item">Элемент для добавления.</param>
        /// <param name="comparer">Сравниватель элементов</param>
        public void AddUnique(T item, IEqualityComparer<T> comparer = null)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterUpgradeableReadSection())
                    {
                        if (!_innerList.Contains(item, comparer))
                        {
                            using (_locker.EnterWriteSection())
                            {
                                InsertItem(_innerList.Count, item);
                            }
                        }
                    }
                },
                UseUiThread);
        }

        /// <summary>
        /// Производит обновление списка <paramref name="source"/> 
        /// в соответствии с коллекцией <paramref name="target"/>
        /// на основе <paramref name="comparisonIdentityExtractor"/>.
        /// </summary>
        /// <typeparam name="TElement">Тип элементов списка.</typeparam>
        /// <typeparam name="TIdentity">Тип сравнительного идентификатора.</typeparam>
        /// <param name="source">Исходный список.</param>
        /// <param name="target">Коллекция элементов.</param>
        /// <param name="comparisonIdentityExtractor">Метод, возвращающий 
        /// идентификатор типа <typeparamref name="TIdentity"/> по которму 
        /// можно установить соответствие элементов.
        /// <param name="result">Экшен в которые передается количество добавленных и удаленных элементов</param>
        public void UpdateTo<TIdentity>(IList<T> target, Func<T, TIdentity> comparisonIdentityExtractor,
            Action<int, int> result = null)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            var removed = RemoveRangeInternal(target, comparisonIdentityExtractor);
                            var added = AddRangeInternal(target, comparisonIdentityExtractor);

                            result?.Invoke(added, removed);
                        }
                    }
                },
                UseUiThread);
        }

        private int RemoveRangeInternal<TIdentity>(IEnumerable<T> collection,
            Func<T, TIdentity> comparisonIdentityExtractor)
        {
            var comparerSet = EnumerableHelpers.ToHashSet(collection.Select(comparisonIdentityExtractor));

            var beforeRemoveCount = Count;

            foreach (var index in EnumerableHelpers.GetIndicesToRemove(this, comparerSet, comparisonIdentityExtractor))
            {
                _innerList.RemoveAt(index);
            }

            return beforeRemoveCount - Count;
        }

        private int AddRangeInternal<TIdentity>(IEnumerable<T> collection,
            Func<T, TIdentity> comparisonIdentityExtractor)
        {
            var comparerSet = EnumerableHelpers.ToHashSet(this.Select(comparisonIdentityExtractor));

            var countBeforeAdd = Count;

            foreach (var item in collection.Where(v => !comparerSet.Contains(comparisonIdentityExtractor(v))))
            {
                _innerList.Insert(_innerList.Count, item);
            }

            return Count - countBeforeAdd;
        }

        private void ClearItems()
        {
            _innerList.Clear();

            if (!_updHelper.IsUpdating)
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void InsertItem(int index, T item)
        {
            _innerList.Insert(index, item);

            if (!_updHelper.IsUpdating)
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
        }

        private void RemoveItem(int index)
        {
            var item = _innerList[index];
            _innerList.RemoveAt(index);

            if (!_updHelper.IsUpdating)
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        private void MoveItem(int oldIndex, int newIndex)
        {
            var item = _innerList[oldIndex];

            _innerList.RemoveAt(oldIndex);
            _innerList.Insert(newIndex, item);

            if (!_updHelper.IsUpdating)
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item,
                    newIndex, oldIndex));
            }
        }

        private void SetItem(int index, T item)
        {
            var old = _innerList[index];
            _innerList[index] = item;

            if (!_updHelper.IsUpdating)
            {
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item,
                    old, index));
            }
        }

        public void Add(T item)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterWriteSection())
                    {
                        InsertItem(_innerList.Count, item);
                    }
                },
                UseUiThread);
        }

        public void Clear()
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterWriteSection())
                    {
                        ClearItems();
                    }
                },
                UseUiThread);
        }

        public bool Contains(T item)
        {
            using (_locker.EnterReadSection())
            {
                return _innerList.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (_locker.EnterReadSection())
            {
                _innerList.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            return Tools.DispatchedInvoke(delegate
            {
                using (_locker.EnterUpgradeableReadSection())
                {
                    var index = _innerList.IndexOf(item);
                    if (index < 0)
                    {
                        return false;
                    }

                    using (_locker.EnterWriteSection())
                    {
                        RemoveItem(index);
                        return true;
                    }
                }
            });
        }

        public int IndexOf(T item)
        {
            using (_locker.EnterReadSection())
            {
                return _innerList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterWriteSection())
                    {
                        InsertItem(index, item);
                    }
                },
                UseUiThread);
        }

        public void RemoveAt(int index)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterWriteSection())
                    {
                        RemoveItem(index);
                    }
                },
                UseUiThread);
        }

        public void Move(int oldIndex, int newIndex)
        {
            Tools.DispatchedInvoke(delegate
            {
                using (_locker.EnterWriteSection())
                {
                    MoveItem(oldIndex, newIndex);
                }
            }, UseUiThread);
        }

        public T this[int index]
        {
            get
            {
                using (_locker.EnterReadSection())
                {
                    return _innerList[index];
                }
            }
            set
            {
                Tools.DispatchedInvoke(delegate
                    {
                        using (_locker.EnterWriteSection())
                        {
                            SetItem(index, value);
                        }
                    },
                    UseUiThread);
            }
        }

        public void BeginUpdate()
        {
            _updHelper.BeginUpdate();
        }

        public void EndUpdate()
        {
            _updHelper.EndUpdate();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public IEnumerable Clone() => new ObservableCollectionCore<T>(_innerList) {UseUiThread = UseUiThread};

        object ICloneable.Clone() => Clone();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            using (_locker.EnterReadSection())
            {
                var cached = new List<T>(_innerList);
                return cached.GetEnumerator();
            }
        }

        int IReadOnlyCollection<T>.Count => Count;

        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo(array.Cast<T>().ToArray(), index);
        }

        int ICollection.Count => Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection) _innerList).SyncRoot;

        int IList.Add(object value)
        {
            Add((T) value);
            return Count - 1;
        }

        void IList.Clear()
        {
            Clear();
        }

        bool IList.Contains(object value) => Contains((T) value);

        int IList.IndexOf(object value) => IndexOf((T) value);

        void IList.Insert(int index, object value)
        {
            Insert(index, (T) value);
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => IsReadOnly;

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T) value;
        }

        void IList.Remove(object value)
        {
            Remove((T) value);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        #region 4 DevExpress With Love :3

        public bool TryAssign(IList<T> source)
        {
            bool result;

            try
            {
                Assign(source);
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void AssignWithAdd(IList<T> source)
        {
            if (ReferenceEquals(source, null))
            {
                throw new ArgumentNullException(nameof(source));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_locker.EnterUpgradeableReadSection())
                    {
                        if (_innerList.Count == source.Count && _innerList.SequenceEqual(source))
                        {
                            return;
                        }

                        using (_locker.EnterWriteSection())
                        {
                            _innerList.Clear();

                            OnPropertyChanged(nameof(Count));
                            OnPropertyChanged(IndexerName);

                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                            _innerList.AddRange(source);

                            OnPropertyChanged(nameof(Count));
                            OnPropertyChanged(IndexerName);

                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(source.ToList())));
                        }
                    }
                },
                UseUiThread);
        }

        public void AddRangeWithAdd(IEnumerable<T> items)
        {
            if (ReferenceEquals(items, null))
            {
                throw new ArgumentNullException(nameof(items));
            }

            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            _innerList.AddRange(items);

                            OnPropertyChanged(nameof(Count));
                            OnPropertyChanged(IndexerName);

                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(items.ToList())));
                        }
                    }
                },
                UseUiThread);
        }

        public void UpdateToWithAdd<TIdentity>(IList<T> target, Func<T, TIdentity> comparisonIdentityExtractor,
            Action<int, int> result = null)
        {
            Tools.DispatchedInvoke(delegate
                {
                    using (_updHelper.DoUpdate())
                    {
                        using (_locker.EnterWriteSection())
                        {
                            var removed = RemoveRangeInternal(target);
                            var added = AddRangeInternal(target);

                            result?.Invoke(added, removed);
                        }
                    }
                },
                UseUiThread);

            int RemoveRangeInternal(IEnumerable<T> collection)
            {
                var comparerSet = EnumerableHelpers.ToHashSet(collection.Select(comparisonIdentityExtractor));

                var beforeRemoveCount = Count;

                foreach (var index in EnumerableHelpers.GetIndicesToRemove(this, comparerSet, comparisonIdentityExtractor))
                {
                    RemoveItem(index);
                }

                return beforeRemoveCount - Count;
            }

            int AddRangeInternal(IEnumerable<T> collection)
            {
                var comparerSet = EnumerableHelpers.ToHashSet(this.Select(comparisonIdentityExtractor));

                var countBeforeAdd = Count;

                var old = _innerList.ToList();
                var changed = false;
                foreach (var item in collection.Where(v => !comparerSet.Contains(comparisonIdentityExtractor(v))))
                {
                    _innerList.Insert(_innerList.Count, item);
                    changed = true;
                }

                if (changed)
                {
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(IndexerName);

                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        new List<T>(_innerList.Except(old).ToList())));
                }

                return Count - countBeforeAdd;
            }
        }

        #endregion

        private static class EnumerableHelpers
        {
            public static HashSet<TElement> ToHashSet<TElement>(IEnumerable<TElement> source) => source != null
                ? new HashSet<TElement>(source)
                : throw new ArgumentNullException(nameof(source),
                    "Исходная коллекция не может быть null.");

            public static IEnumerable<int> GetIndicesToRemove<TElement, TKey>(IList<TElement> source,
                ISet<TKey> keyCollection, Func<TElement, TKey> keyExtractor)
            {
                for (var i = source.Count - 1; i >= 0; --i)
                {
                    var item = source[i];

                    if (!keyCollection.Contains(keyExtractor(item)))
                    {
                        yield return i;
                    }
                }
            }
        }
    }
}