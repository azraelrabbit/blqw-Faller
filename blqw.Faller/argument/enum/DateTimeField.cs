using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 时间字段
    /// </summary>
    [Serializable]
    public enum DateTimeField
    {
        /// <summary> 年
        /// </summary>
        Year,
        /// <summary> 月
        /// </summary>
        Month,
        /// <summary> 日
        /// </summary>
        Day,
        /// <summary> 时
        /// </summary>
        Hour,
        /// <summary> 分
        /// </summary>
        Minute,
        /// <summary> 秒
        /// </summary>
        Second,
        /// <summary> 星期
        /// </summary>
        Week,
    }
}
