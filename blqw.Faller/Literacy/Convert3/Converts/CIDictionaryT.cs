using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CIDictionary<K, V> : AdvancedConvertor<IDictionary<K, V>>
    {
        protected override bool Try(object input, Type outputType, out IDictionary<K, V> result)
        {
            if (input == null)
            {
                result = null;
                return false;
            }
            if (outputType.IsGenericType)
            {
                if (outputType.IsGenericTypeDefinition)
                {
                    result = null;
                    return false;
                }
                if (outputType.GenericTypeArguments.Length != 2)
                {
                    result = null;
                    return false;
                }
            }

            var reader = input as IDataReader;
            if (reader != null)
            {
                if (reader.IsClosed)
                {
                    throw new InvalidCastException("DataReader已经关闭");
                }
                var arg = new ConvertHelper(outputType);
                var cols = Enumerable.Range(0, reader.FieldCount).Select(i => new { name = reader.GetName(i), i });
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (arg.Add(reader.GetName(i), reader.GetValue, i) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = arg.Dictionary;
                return true;
            }

            var nv = input as NameValueCollection;
            if (nv != null)
            {
                var arg = new ConvertHelper(outputType);
                foreach (string name in nv)
                {
                    if (arg.Add(name, nv[name]) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = arg.Dictionary;
                return true;
            }

            var rv = input as DataRowView;
            DataRow row;
            if (rv != null)
            {
                row = rv.Row;
            }
            else
            {
                row = input as DataRow;
            }
            if (row != null && row.Table != null)
            {
                var arg = new ConvertHelper(outputType);
                var cols = row.Table.Columns;
                foreach (DataColumn col in cols)
                {
                    if (arg.Add(col.ColumnName, row[col]) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = arg.Dictionary;
                return true;
            }

            var dict = input as IDictionary;
            if (dict != null)
            {
                var arg = new ConvertHelper(outputType);
                foreach (DictionaryEntry item in dict)
                {
                    if (arg.Add(item.Key, item.Value) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = arg.Dictionary;
                return true;
            }

            result = null;
            return false;
        }

        struct ConvertHelper
        {
            IConvertor<K> _keyConvertor;
            IConvertor<V> _valueConvertor;
            Type _keyType;
            Type _valueType;
            public readonly IDictionary<K, V> Dictionary;
            public ConvertHelper(Type type)
            {
                var genericTypes = type.GenericTypeArguments;
                if (genericTypes.Length == 0)
                {
                    _keyType = typeof(object);
                    _valueType = typeof(object);
                }
                else
                {
                    _keyType = type.GenericTypeArguments[0];
                    _valueType = type.GenericTypeArguments[1];
                }
                _keyConvertor = Convert3.GetConvertor<K>();
                _valueConvertor = Convert3.GetConvertor<V>();
                Dictionary = (IDictionary<K, V>)Activator.CreateInstance(type);
            }

            public bool Add(object key, object value)
            {
                if (_keyConvertor == null || _valueConvertor == null)
                {
                    return false;
                }
                K k;
                if (_keyConvertor.Try(key, _keyType, out k) == false)
                {
                    return false;
                }
                V v;
                if (_valueConvertor.Try(value, _valueType, out v) == false)
                {
                    return false;
                }
                Dictionary.Add(k, v);
                return true;
            }
            public bool Add<P>(object key, Func<P, object> getValue, P param)
            {
                if (_keyConvertor == null || _valueConvertor == null)
                {
                    return false;
                }

                K k;
                if (_keyConvertor.Try(key, _keyType, out k) == false)
                {
                    return false;
                }
                V v;
                if (_valueConvertor.Try(getValue(param), _valueType, out v) == false)
                {
                    return false;
                }
                Dictionary.Add(k, v);
                return true;
            }
        }

        protected override bool Try(string input, Type outputType, out IDictionary<K, V> result)
        {
            result = null;
            return false;
        }
    }
}
