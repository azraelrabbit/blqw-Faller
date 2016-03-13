using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    public class CUri : SystemTypeConvertor<Uri>
    {
        protected override bool Try(object input, out Uri result)
        {
            result = null;
            return false;
        }

        protected override bool Try(string input, out Uri result)
        {
            if (input == null || input.Length == 0)
            {
                result = null;
                return false;
            }


            input = input.TrimStart();
            if (input.Length > 10 && input[6] != '/')
            {
                if (Uri.TryCreate("http://" + input, UriKind.Absolute, out result))
                {
                    return true;
                }
            }

            return Uri.TryCreate(input, UriKind.Absolute, out result);
        }
    }
}
