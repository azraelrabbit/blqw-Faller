namespace blqw
{
    /// <summary> 表达式解析结果类型
    /// </summary>
    [System.Serializable]
    public enum DustType
    {
        /// <summary> 未知
        /// </summary>
        Undefined = 0,
        /// <summary> sql语句
        /// </summary>
        Sql = 1,
        /// <summary> 子表达式 
        /// </summary>
        SubExpression = 2,
        /*大于99的都是object类型*/
        /// <summary> 未知类型
        /// </summary>
        Object = 100,
        /// <summary> 数字
        /// </summary>
        Number = 101,
        /// <summary> 数组
        /// </summary>
        Array = 102,
        /// <summary> 布尔值
        /// </summary>
        Boolean = 103,
        /// <summary> 时间
        /// </summary>
        DateTime = 104,
        /// <summary> 二进制
        /// </summary>
        Binary = 105,
        /// <summary> 字符串
        /// </summary>
        String = 106,
    }
}
