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
    class CNameValueCollection : AdvancedConvertor<NameValueCollection>
    {
        protected override bool Try(object input, Type outputType, out NameValueCollection result)
        {
            if (input == null)
            {
                result = null;
                return false;
            }
            var conv = Convert3.GetConvertor<string>();

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
                result = arg.Collection;
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
                result = arg.Collection;
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
                result = arg.Collection;
                return true;
            }

            result = null;
            return false;
        }


        struct ConvertHelper
        {
            IConvertor<string> _convertor;
            public readonly NameValueCollection Collection;
            public ConvertHelper(Type type)
            {
                _convertor = Convert3.GetConvertor<string>();
                Collection = (NameValueCollection)Activator.CreateInstance(type);
            }

            public bool Add(object key, object value)
            {
                string skey, svalue;
                if (_convertor.Try(key, null, out skey) == false)
                {
                    return false;
                }
                if (_convertor.Try(value, null, out svalue) == false)
                {
                    return false;
                }
                Collection.Add(skey, svalue);
                return true;
            }
        }


        protected override bool Try(string input, Type outputType, out NameValueCollection result)
        {
            result = null;
            return false;
        }
    }
}
