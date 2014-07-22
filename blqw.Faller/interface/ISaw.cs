using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Data.Common;
using System.Reflection;

namespace blqw
{
    /// <summary> 定义一组方法，它支持自定义Sql语句的格式。
    /// </summary>
    public interface ISaw
    {
        /// <summary> 解释二元操作
        /// </summary>
        /// <param name="left">左元素</param>
        /// <param name="operator">二元操作符</param>
        /// <param name="right">右元素</param>
        string BinaryOperation(string left, BinaryOperator @operator, string right);
        /// <summary> 解释Contains操作
        /// </summary>
        /// <param name="not">是否为not</param>
        /// <param name="item">要在集合中查找的对象</param>
        /// <param name="array">要查找的集合</param>
        string ContainsOperation(bool not, string item, string[] array);
        /// <summary> 向参数集合中追加一个任意类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="obj">需要追加的参数值</param>
        /// <param name="parameters">参数集合</param>
        string AddObject(object obj, ICollection<DbParameter> parameters);
        /// <summary> 向参数集合中追加一个数字类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="number">需要追加的数字</param>
        /// <param name="parameters">参数集合</param>
        string AddNumber(IConvertible number, ICollection<DbParameter> parameters);
        /// <summary> 向参数集合中追加一个布尔类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="obj">需要追加的布尔值</param>
        /// <param name="parameters">参数集合</param>
        string AddBoolean(bool value, ICollection<DbParameter> parameters);
        /// <summary> 向参数集合中追加当前时间,并返回参数名sql表达式
        /// </summary>
        /// <param name="parameters">参数集合</param>
        string AddTimeNow(ICollection<DbParameter> parameters);
        /// <summary> 获取实体类型所映射的表名
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="alias">别名</param>
        string GetTable(Type type, string alias);
        /// <summary> 获取实体属性或字段所映射的列名
        /// </summary>
        /// <param name="table">表名或表别名</param>
        /// <param name="type">实体属性或字段</param>
        string GetColumn(string table, MemberInfo member);
        /// <summary> 获取列名和列别名组合后的sql表达式
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="alias">列别名</param>
        string GetColumn(string columnName, string alias);
        /// <summary> 将.NET中的方法解释为sql表达式
        /// </summary>
        /// <param name="method">需解释的方法</param>
        /// <param name="target">方法调用者</param>
        /// <param name="args">方法参数</param>
        /// <returns></returns>
        string ParseMethod(MethodInfo method, SawDust target, SawDust[] args);
    }
}
