using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 二元操作符枚举
    /// </summary>
    [Serializable]
    public enum BinaryOperator
    {
        /// <summary> 加
        /// </summary>
        Add,
        /// <summary> 减
        /// </summary>
        Subtract,
        /// <summary> 除
        /// </summary>
        Divide,
        /// <summary> 取余
        /// </summary>
        Modulo,
        /// <summary> 乘
        /// </summary>
        Multiply,
        /// <summary> 幂运算
        /// </summary>
        Power,
        /// <summary> 位移运算 a &lt;&lt; b
        /// </summary>
        LeftShift,
        /// <summary> 位移运算 a &gt;&gt; b
        /// </summary>
        RightShift,
        /// <summary> 与 a &amp;&amp; b 或 a AND b
        /// </summary>
        And,
        /// <summary> 或 a || b 或 a OR b
        /// </summary>
        Or,
        /// <summary> 等于 a == b
        /// </summary>
        Equal,
        /// <summary> 不等于 a != b 或 a <> b
        /// </summary>
        NotEqual,
        /// <summary> 大于 a &gt; b
        /// </summary>
        GreaterThan,
        /// <summary> 大于等于 a &gt;= b
        /// </summary>
        GreaterThanOrEqual,
        /// <summary> 小于 a &lt; b
        /// </summary>
        LessThan,
        /// <summary> 小于等于 a &lt;= b
        /// </summary>
        LessThanOrEqual,
        /// <summary> LIKE操作 a LIKE %b%
        /// </summary>    
        Contains,
        /// <summary> LIKE操作 a LIKE b%
        /// </summary>    
        StartWith,
        /// <summary> LIKE操作 a LIKE %b
        /// </summary>    
        EndWith,
        /// <summary> LIKE操作 a NOT LIKE %b%
        /// </summary>    
        NotContains,
        /// <summary> LIKE操作 a NOT LIKE b%
        /// </summary>    
        NotStartWith,
        /// <summary> LIKE操作 a NOT LIKE %b
        /// </summary>
        NotEndWith,
        /// <summary> 位运算 如 a &amp; b
        /// </summary>
        BitAnd,
        /// <summary> 位运算 如 a | b
        /// </summary>
        BitOr,
        /// <summary> 位运算 如 a ^ b
        /// </summary>
        BitXor,
    }
}
