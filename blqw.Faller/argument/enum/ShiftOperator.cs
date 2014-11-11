using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 位移操作符枚举
    /// </summary>
    [Serializable]
    public enum ShiftOperator
    {
        /// <summary> 位移运算 a &lt;&lt; b
        /// </summary>
        Left,
        /// <summary> 位移运算 a &gt;&gt; b
        /// </summary>
        Right,
    }
}
