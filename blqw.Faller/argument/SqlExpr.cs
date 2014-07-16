using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace blqw
{
    public class SqlExpr
    {
        public string Sql { get; private set; }

        public static explicit operator SqlExpr(string value)
        {
            return new SqlExpr() { Sql = value };
        }

        public static implicit operator bool(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator byte(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator char(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator DateTime(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator decimal(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator double(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator short(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator int(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator long(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator sbyte(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator float(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator string(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator ushort(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator uint(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator ulong(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator Guid(SqlExpr value) { throw new NotImplementedException(); }
        public static implicit operator Byte[](SqlExpr value) { throw new NotImplementedException(); }

    }
}
