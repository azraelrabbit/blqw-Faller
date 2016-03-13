using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class TestSaw : blqw.OracleSaw
    {
        public override string AddBoolean(bool value, ICollection<DbParameter> parameters)
        {
            return value ? "1" : "0";
        }

        public override string AddNumber(IConvertible number, ICollection<DbParameter> parameters)
        {
            return number.ToString();
        }
    }
}
