using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CGuid : SystemTypeConvertor<Guid>
    {
        protected override bool Try(object input, out Guid result)
        {
            var bs = input as byte[];
            if (bs != null && bs.Length == 16)
            {
                result = new Guid(bs);
                return true;
            }
            result = default(Guid);
            return false;
        }

        protected override bool Try(string input, out Guid result)
        {
            if (input == null || input.Length == 0)
            {
                result = Guid.Empty;
                return false;
            }

#if !NF2
            if (Guid.TryParse(input, out result))
            {
                return true;
            }
#else
            var l = input.Length;
            var a = input[0];
            var b = input[l - 1];
            if (a == ' ' || b == ' ')
            {
                input = input.Trim();
                a = input[0];
                b = input[l - 1];
            }

            if ((a == '{' && b == '}') || l == 32 || l == 36)
            {
                try
                {
                    result = new Guid(input);
                    return true;
                }
                catch { }
            }
#endif
            else
            {
                try
                {
                    var bs = Convert.FromBase64String(input);
                    if (bs.Length == 16)
                    {
                        result = new Guid(bs);
                        return true;
                    }
                }
                catch { }
            }
            result = Guid.Empty;
            return false;
        }
    }
}
