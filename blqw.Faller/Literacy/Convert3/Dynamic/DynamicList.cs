using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw.Dynamic
{
    public class DynamicList<T> : DynamicObject, IList, IList<T>
    {
        IList<T> _list;

        public DynamicList()
        {
            _list = new List<T>();
        }


        public DynamicList(IList<T> list)
        {
            _list = list ?? new List<T>();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return new string[] { "Count", "Length" };
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return Convert3.TryChangedType(_list, binder.ReturnType, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name.ToLower();
            if ("count,length,".IndexOf(binder.Name + ",", StringComparison.OrdinalIgnoreCase) > -1)
            {
                result = _list.Count;
                return true;
            }
            result = null;
            return false;
        }

        private int Indexer(object[] indexes)
        {
            if (indexes == null || indexes.Length != 1)
            {
                return -1;
            }
            var i = indexes[0].To<int>(-1);
            if (i < 0 || i >= _list.Count)
            {
                return -1;
            }
            return i;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var i = Indexer(indexes);
            if (i >= 0 && Convert3.TryChangedType(_list[i], binder.ReturnType, out result))
            {
                result = result as DynamicObject ?? new DynamicSystemObject(result);
                return true;
            }

            result = binder.ReturnType.GetDefaultValue();
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (IsReadOnly)
            {
                return false;
            }

            var i = Indexer(indexes);
            if (i < 0)
            {
                return false;
            }
            T val;
            if (Convert3.TryTo<T>(value, out val))
            {
                _list[i] = val;
                return true;
            }
            return false;
        }

        public bool IsReadOnly { get; set; }


        int IList.Add(object value)
        {
            _list.Add(value.To<T>());
            return _list.Count;
        }

        void IList.Clear()
        {
            _list.Clear();
        }

        bool IList.Contains(object value)
        {
            if (value is T)
            {
                return _list.Contains((T)value);
            }
            return false;
        }

        int IList.IndexOf(object value)
        {
            if (value is T)
            {
                return _list.IndexOf((T)value);
            }
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            _list.Insert(index, value.To<T>());
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            _list.Remove(value.To<T>());
        }

        void IList.RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                if (index >= 0 && index < _list.Count)
                {
                    return _list[index];
                }
                return null;
            }
            set
            {
                _list[index] = value.To<T>();
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            T[] arr = array as T[];
            if (arr != null)
            {
                _list.CopyTo(arr, index);
                return;
            }

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            arr = new T[array.Length];
            _list.CopyTo(arr, index);
            var elementType = array.GetType().GetElementType();
            for (int i = index; i < arr.Length; i++)
            {
                array.SetValue(arr[i].ChangedType(elementType), i);
            }
        }

        int ICollection.Count
        {
            get { return _list.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return _list; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        int IList<T>.IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        void IList<T>.RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        T IList<T>.this[int index]
        {
            get
            {
                if (index >= 0 && index < _list.Count)
                {
                    return _list[index];
                }
                return default(T);
            }
            set
            {
                _list[index] = value;
            }
        }

        void ICollection<T>.Add(T item)
        {
            _list.Add(item);
        }

        void ICollection<T>.Clear()
        {
            _list.Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            return _list.Contains(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        int ICollection<T>.Count
        {
            get { return _list.Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<T>.Remove(T item)
        {
            return _list.Remove(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
