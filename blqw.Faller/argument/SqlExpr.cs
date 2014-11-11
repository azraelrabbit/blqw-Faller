using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> Sql表达式
    /// </summary>
    public struct SqlExpr
    {
        /// <summary> 对象中的Sql语句
        /// </summary>
        public string Sql { get; private set; }

        /// <summary> 强转string到SqlExpr
        /// </summary>
        public static explicit operator SqlExpr(string value)
        {
            return new SqlExpr() { Sql = value };
        }

        public static implicit operator bool(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator byte(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator char(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator DateTime(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator decimal(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator double(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator short(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator int(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator long(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator sbyte(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator float(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator string(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator ushort(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator uint(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator ulong(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator Guid(SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
        public static implicit operator Byte[](SqlExpr value) { throw new NotSupportedException("仅在表达式树中有效"); }
    }
}
