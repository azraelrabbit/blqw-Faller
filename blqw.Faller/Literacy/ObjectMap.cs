using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 可以通过索引器/键值对访问对象的属性
    /// </summary>
    public class ObjectMap : IDictionary<string, object>
    {
        /// <summary> 独立缓存,防止被加载非公共属性或静态属性
        /// </summary>
        static readonly Dictionary<Type, Literacy> _Cache = new Dictionary<Type, Literacy>();
        Literacy _lit;
        object _target;
        bool _ignoreCase;
        /// <summary> 将任意对象转换为可以通过索引器/键值对访问的形式
        /// </summary>
        /// <param name="obj">需要通过索引器访问属性的对象</param>
        /// <param name="ignoreCase">访问属性是否忽略大小写</param>
        public ObjectMap(object obj, bool ignoreCase)
        {
            if (obj == null)
            {
                _lit = Literacy.Cache(typeof(object), false);
                _target = new object();
                return;
            }
            _target = obj;
            var type = obj.GetType();
            if (_Cache.TryGetValue(type, out _lit) == false)
            {
                lock (_Cache)
                {
                    if (_Cache.TryGetValue(type, out _lit) == false)
                    {
                        _lit = new Literacy(type, true);
                    }
                }
            }
            Count = _lit.Property.Count;
            _ignoreCase = ignoreCase;
        }

        /// <summary> 判断对象是否包含指定属性
        /// </summary>
        /// <param name="key">属性名</param> 
        public bool ContainsKey(string key)
        {
            var p = _lit.Property[key];
            return p != null && (_ignoreCase || string.Equals(p.Name, key, StringComparison.Ordinal));
        }

        /// <summary> 枚举所有的属性名
        /// </summary>
        public ICollection<string> Keys
        {
            get { return _lit.Property.Names; }
        }

        /// <summary> 通过索引器访问属性的值
        /// </summary>
        /// <param name="key">属性名</param>
        public Changeable this[string key]
        {
            get
            {
                var p = _lit.Property[key];
                if (p != null && (_ignoreCase || string.Equals(p.Name, key, StringComparison.Ordinal)))
                {
                    return new Changeable(p.GetValue(_target));
                }
                return new Changeable();
            }
        }
        /// <summary> 获取当前对象共有多少个属性
        /// </summary>
        public int Count { get; private set; }

        #region 显示实现接口

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            foreach (var p in _lit.Property)
            {
                yield return new KeyValuePair<string, object>(p.Name, p.GetValue(_target));
            }
        }

        private void ThrowReadOnly()
        {
            throw new System.NotSupportedException("集合是只读的");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, object>)this).GetEnumerator();
        }
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ContainsKey(item.Key);
        }
        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            var p = _lit.Property[key];
            if (p != null && (_ignoreCase || string.Equals(p.Name, key, StringComparison.Ordinal)))
            {
                value = p.GetValue(_target);
                return true;
            }
            value = null;
            return false;
        }
        ICollection<object> IDictionary<string, object>.Values
        {
            get
            {
                throw new NotSupportedException("此处不支持枚举操作");
            }
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            ThrowReadOnly();
        }


        bool IDictionary<string, object>.Remove(string key)
        {
            ThrowReadOnly();
            return false;
        }


        object IDictionary<string, object>.this[string key]
        {
            get
            {
                var p = _lit.Property[key];
                if (p != null && (_ignoreCase || string.Equals(p.Name, key, StringComparison.Ordinal)))
                {
                    return p.GetValue(_target);
                }
                return null;
            }
            set
            {
                ThrowReadOnly();
            }
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            var names = new string[_lit.Property.Names.Count];
            _lit.Property.Names.CopyTo(names, arrayIndex);
            for (int i = arrayIndex; i < names.Length; i++)
            {
                var name = names[i];
                array[i] = new KeyValuePair<string, object>(name, _lit.Property[name].GetValue(_target));
            }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ThrowReadOnly();
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            ThrowReadOnly();
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            ThrowReadOnly();
            return false;
        }

        #endregion

    }
}
