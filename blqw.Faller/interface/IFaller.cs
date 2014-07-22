using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace blqw
{
    /// <summary> 轻量级表达式树解析器&lt;接口&gt;
    /// </summary>
    public interface IFaller
    {
        /// <summary> 将表达式转为Where语句,不包含Where关键字
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        string ToWhere(ISaw saw);
        /// <summary> 将表达式转为OrderBy语句,不包含OrderBy关键字
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        /// <param name="asc">正序或倒序标识</param>
        string ToOrderBy(ISaw saw, bool asc);
        /// <summary> 将表达式转为Update中Set语句,不包含Set关键字
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        string ToSets(ISaw saw);
        /// <summary> 将表达式转为select语句中的列或列集合的Sql语句
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        string ToSelectColumns(ISaw saw);
        /// <summary> 将表达式转为值或值集合的sql语句
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        string ToValues(ISaw saw);
        /// <summary> 将表达式转为值或值集合的sql语句
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        string ToValues(ISaw saw, Func<string, string> replace);
        /// <summary> 将表达式转为列集合和值集合2个sql语句,可用于拼装insert语句
        /// </summary>
        /// <param name="saw">自定义输出Sql语句的格式的对象</param>
        KeyValuePair<string, string> ToColumnsAndValues(ISaw saw);
        /// <summary> 转换Sql语句过程中产生的参数
        /// </summary>
        ICollection<DbParameter> Parameters { get; }
    }
}
