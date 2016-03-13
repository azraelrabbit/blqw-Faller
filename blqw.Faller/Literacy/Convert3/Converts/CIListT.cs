﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CIList<T> : AdvancedConvertor<ICollection<T>>
    {
        struct DataReaderEnumerable : IEnumerator
        {
            IDataReader _reader;
            public DataReaderEnumerable(IDataReader reader)
            {
                _reader = reader;
            }
            public object Current
            {
                get { return _reader; }
            }

            public bool MoveNext()
            {
                return _reader.Read();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
        private static IEnumerator GetIEnumerator(object input)
        {
            var row = input as DataRow;
            if (row != null)
            {
                return row.ItemArray.GetEnumerator(); ;
            }
            var rv = input as DataRowView;
            if (rv != null)
            {
                return rv.Row.ItemArray.GetEnumerator(); ;
            }
            var reader = input as IDataReader;
            if (reader != null)
            {
                return new DataReaderEnumerable(reader);
            }
            var ls = input as IListSource;
            if (ls != null)
            {
                return ls.GetList().GetEnumerator();
            }
            var emtr = input as IEnumerator;
            if (emtr != null)
            {
                return emtr;
            }
            var emab = input as IEnumerable;
            if (emab != null)
            {
                return emab.GetEnumerator();
            }
            return null;
        }

        protected override bool Try(object input, Type outputType, out ICollection<T> result)
        {
            var emtr = GetIEnumerator(input);
            if (emtr == null)
            {
                result = null;
                return false;
            }

            var elementType = typeof(T);
            var conv = Convert3.GetConvertor<T>();

            var list = (ICollection<T>)Activator.CreateInstance(outputType);
            while (emtr.MoveNext())
            {
                T value;
                if (conv.Try(emtr.Current, elementType, out value) == false)
                {
                    result = null;
                    return false;
                }
                list.Add(value);
            }
            result = list;
            return true;
        }

        readonly static string[] Separator = { ", ", "," };

        protected override bool Try(string input, Type outputType, out ICollection<T> result)
        {
            if (input == null)
            {
                result = null;
                return true;
            }
            var arr = input.Split(Separator, StringSplitOptions.None);
            return Try(arr, outputType, out result);
        }
    }
}
