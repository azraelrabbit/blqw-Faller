using System;
namespace blqw
{
    /// <summary> 表达式树解析结果
    /// </summary>
    public interface ISawDust
    {
        /// <summary>  是否是非DustType.Sql和DustType.Undefined类型
        /// </summary>
        bool IsObject { get; }
        /// <summary> 无论结果类型 强制转换为Sql语句,DustType.Undefined抛出异常
        /// </summary>
        string ToSql();
        /// <summary> 结果类型
        /// </summary>
        DustType Type { get; }
        /// <summary> 结果值
        /// </summary>
        object Value { get; }
    }
}
