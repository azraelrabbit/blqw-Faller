using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 按位操作符枚举
    /// </summary>
    [Serializable]
    public enum BitOperator
    {
        /// <summary> 位运算 如 a &amp; b
        /// </summary>
        And,
        /// <summary> 位运算 如 a | b
        /// </summary>
        Or,
        /// <summary> 位运算 如 a ^ b
        /// </summary>
        Xor,
    }
}
