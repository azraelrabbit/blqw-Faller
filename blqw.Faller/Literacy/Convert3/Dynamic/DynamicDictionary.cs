using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw.Dynamic
{
    public class DynamicDictionary : DynamicObject, IDictionary<string, object>,IDictionary
    {

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _dict.Keys;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return Convert3.TryChangedType(_dict, binder.ReturnType, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_dict.TryGetValue(binder.Name, out result))
            {
                if (Convert3.TryChangedType(result, binder.ReturnType, out result))
                {
                    result = result as DynamicObject ?? new DynamicSystemObject(result);
                    return true;
                }
            }
            result = binder.ReturnType.GetDefaultValue();
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (IsReadOnly)
            {
                return false;
                //throw new NotSupportedException("当前对象是只读的");
            }
            _dict[binder.Name] = value;
            return true;
        }

        private string GetIndexer0(object[] indexes)
        {
            if (indexes == null || indexes.Length != 1)
            {
                return  null;
            }
            return indexes[0] as string;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var key = GetIndexer0(indexes);
            if (key == null)
            {
                result = binder.ReturnType.GetDefaultValue();
                return true;
            }

            if (_dict.TryGetValue(key, out result))
            {
                if (Convert3.TryChangedType(result, binder.ReturnType, out result))
                {
                    result = result as DynamicObject ?? new DynamicSystemObject(result);
                    return true;
                }
            }
            result = binder.ReturnType.GetDefaultValue();
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (IsReadOnly)
            {
                return false;
                //throw new NotSupportedException("当前对象是只读的");
            }
            var key = GetIndexer0(indexes);
            if (key == null)
            {
                return false;
            }
            _dict[key] = value;
            return true;
        }

        public bool IsReadOnly { get; set; }

        #region 必要属性构造函数
        Dictionary<string, object> _dict;

        public DynamicDictionary()
        {
            _dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public DynamicDictionary(StringComparer comparer)
        {
            _dict = new Dictionary<string, object>(comparer);
        }



        #endregion

        #region 显示实现接口


        void IDictionary<string, object>.Add(string key, object value)
        {
            _dict.Add(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return _dict.Keys; }
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            return _dict.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return _dict.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return _dict.Values; }
        }

        object IDictionary<string, object>.this[string key]
        {
            get
            {
                object value;
                if (_dict.TryGetValue(key, out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    _dict.Remove(key);
                }
                else
                {
                    _dict[key] = value;
                }
            }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            _dict.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_dict).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_dict).CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return _dict.Count; }
        }


        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_dict).Remove(item);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }


        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }


        void IDictionary.Add(object key, object value)
        {
            var k =  key as string;
            if (k == null)
            {
                throw new ArgumentOutOfRangeException("key", "key只能是字符串类型");
            }
            _dict.Add(k, value);
        }

        void IDictionary.Clear()
        {
            _dict.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return _dict.ContainsKey(key as string);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        ICollection IDictionary.Keys
        {
            get { return _dict.Keys; }
        }

        void IDictionary.Remove(object key)
        {
            _dict.Remove(key as string);
        }

        ICollection IDictionary.Values
        {
            get { return _dict.Values; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return ((IDictionary<string,object>)_dict)[key as string];
            }
            set
            {
                ((IDictionary<string,object>)_dict)[key as string] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_dict).CopyTo(array, index);
        }

        int ICollection.Count
        {
            get { return _dict.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return _dict; }
        }
        #endregion

    }
}
