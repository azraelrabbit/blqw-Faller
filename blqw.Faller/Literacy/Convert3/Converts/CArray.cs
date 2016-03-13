using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CArray : AdvancedConvertor<Array>
    {
        protected override bool Try(object input, Type outputType, out Array result)
        {
            var emab = input as IEnumerable;
            var emtr = emab == null ? input as IEnumerator : emab.GetEnumerator();
            if (emtr == null)
            {
                result = null;
                return false;
            }
            var elementType = outputType.GetElementType();
            var conv = Convert3.GetConvertor(elementType);
            if (conv == null)
            {
                result = null;
                return false;
            }

            var array = new ArrayList();
            while (emtr.MoveNext())
            {
                object value;
                if (conv.Try(emtr.Current, elementType, out value) == false)
                {
                    result = null;
                    return false;
                }
                array.Add(value);
            }
            result = array.ToArray(elementType);
            return true;
        }

        readonly static string[] Separator = { ", ", "," };

        protected override bool Try(string input, Type outputType, out Array result)
        {
            if (input == null)
            {
                result = null;
                return true;
            }
            var elementType = outputType.GetElementType();
            var conv = Convert3.GetConvertor(outputType.GetElementType());
            if (conv == null)
            {
                result = null;
                return false;
            }
            var items = input.Split(Separator, StringSplitOptions.None);
            var array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                object value;
                if (conv.Try(items[i], elementType, out value) == false)
                {
                    result = null;
                    return false;
                }
                array.SetValue(value, i);
            }

            result = array;
            return true;
        }
    }
}
