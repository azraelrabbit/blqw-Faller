using blqw;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw.Dynamic
{
    public class DynamicSystemObject : DynamicObject
    {
        object _value;
        public DynamicSystemObject(object value)
        {
            _value = value;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
             return new string[0];
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return Convert3.TryChangedType(_value, binder.ReturnType, out result);
        }
    }
}
