using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace blqw
{
    public class SqlExpr
    {
        public string Sql { get; private set; }
        public static explicit operator string(SqlExpr value)
        {
            if (value == null)
            {
                return null;
            }
            return value.Sql;
        }
        public static implicit operator SqlExpr(string value)
        {
            return new SqlExpr() { Sql = value };
        }
    }
}
