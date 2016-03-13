using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CObject : AdvancedConvertor<object>
    {
        protected override bool Try(object input, Type outputType, out object result)
        {
            if (input == null)
            {
                result = null;
                return outputType.IsClass;
            }
            if (outputType.IsInstanceOfType(input))
            {
                result = input;
                return true;
            }

            var nv = input as NameValueCollection;
            if (nv != null)
            {
                var obj = new SetObjectProperty(outputType);
                foreach (string name in nv)
                {
                    if (obj.Set(name, nv[name]) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = obj.Instance;
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
                var obj = new SetObjectProperty(outputType);
                var cols = row.Table.Columns;
                foreach (DataColumn col in cols)
                {
                    if (obj.Set(col.ColumnName, row[col]) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = obj.Instance;
                return true;
            }

            var reader = input as IDataReader;
            if (reader != null)
            {
                if (reader.IsClosed)
                {
                    throw new InvalidCastException("DataReader已经关闭");
                }
                var obj = new SetObjectProperty(outputType);
                var cols = Enumerable.Range(0, reader.FieldCount).Select(i => new { name = reader.GetName(i), i });
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (obj.Set(reader.GetName(i), reader.GetValue, i) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = obj.Instance;
                return true;
            }

            var dict = input as IDictionary;
            if (dict != null)
            {
                var obj = new SetObjectProperty(outputType);
                foreach (DictionaryEntry item in dict)
                {
                    var name = item.Key as string;
                    if (name != null && obj.Set(name, item.Value) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = obj.Instance;
                return true;
            }

            {
                var obj = new SetObjectProperty(outputType);
                foreach (var p in input.GetType().GetProperties())
                {
                    if (p.CanRead && p.GetIndexParameters().Length == 0
                        && obj.Set(p.Name, p.GetValue, input) == false)
                    {
                        result = null;
                        return false;
                    }
                }
                result = obj.Instance;
                return true;
            }

            result = null;
            return false;
        }

        struct SetObjectProperty
        {
            Dictionary<string, PropertyInfo> _propertis;
            public readonly object Instance;
            public SetObjectProperty(Type type)
            {
                _propertis = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in type.GetProperties())
                {
                    if (p.CanWrite && p.GetIndexParameters().Length == 0)
                    {
                        _propertis[p.Name] = p;
                    }
                }
                Instance = Activator.CreateInstance(type);
            }

            public bool Set(string name, object value)
            {
                PropertyInfo p;
                if (_propertis.TryGetValue(name, out p))
                {
                    if (Convert3.TryChangedType(value, p.PropertyType, out value))
                    {
                        p.SetValue(Instance, value);
                        return true;
                    }
                    return false;
                }
                return true;
            }

            public bool Set<P>(string name, Func<P, object> getValue, P param)
            {
                PropertyInfo p;
                if (_propertis.TryGetValue(name, out p))
                {
                    var value = getValue(param);
                    if (Convert3.TryChangedType(value, p.PropertyType, out value))
                    {
                        p.SetValue(Instance, value);
                        return true;
                    }
                    return false;
                }
                return true;
            }
        }

        protected override bool Try(string input, Type outputType, out object result)
        {
            try
            {
                var type = Type.GetType(input, false, true);
                if (type == null)
                {
                    result = null;
                    return false;
                }
                else if (outputType.IsAssignableFrom(type) == false)
                {
                    result = null;
                    return false;
                }

                if ((type.TypeInitializer ?? type.GetConstructor(Type.EmptyTypes)) == null)
                {
                    result = null;
                    return false;
                }
                outputType = type;
                result = Activator.CreateInstance(type);
                return true;
            }
            catch (FileLoadException)
            {
                result = null;
                return false;
            }
            catch (TargetInvocationException ex)
            {
                Trace.TraceInformation(CType.GetDisplayName(outputType) + ",初始化失败:" + ex.Message);
                result = null;
                return false;
            }
        }

        static IConvertor<object> _convertor;

        static IConvertor<object> Convertor
        {
            get
            {
                return _convertor ??
                    (_convertor = Convert3.GetConvertor<object>()) ??
                    (new CObject());
            }
        }

        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        public static bool TryTo<T>(object input, Type outputType, out T result)
        {
            object r;
            if (Convertor.Try(input, typeof(T), out r))
            {
                result = (T)r;
                return true;
            }
            result = default(T);
            return false;
        }

        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        public static bool TryTo<T>(string input, Type outputType, out T result)
        {
            object r;
            if (Convertor.Try(input, typeof(T), out r))
            {
                result = (T)r;
                return true;
            }
            result = default(T);
            return false;
        }
    }
}
