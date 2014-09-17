using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace blqw
{
    /// <summary> 支持自定义Sql语句格式基类。
    /// </summary>
    public abstract class BaseSaw : ISaw
    {
        DbProviderFactory _factory;
        string _aliasSeparator;

        private static void NotNull<T>(T value, string argName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        /// <summary> 初始化并提供 DbProviderFactory
        /// </summary>
        /// <param name="factory"></param>
        protected BaseSaw(DbProviderFactory factory)
        {
            NotNull(factory, "factory");
            _aliasSeparator = AliasSeparator;
            if (_aliasSeparator == null)
            {
                _aliasSeparator = " ";
            }
            else
            {
                _aliasSeparator = _aliasSeparator.Trim();
                if (_aliasSeparator.Length == 0)
                {
                    _aliasSeparator = " ";
                }
                else
                {
                    _aliasSeparator = " " + _aliasSeparator + " ";
                }
            }
            _factory = factory;
        }

        /// <summary> 解释二元操作
        /// </summary>
        /// <param name="left">左元素</param>
        /// <param name="operator">二元操作符</param>
        /// <param name="right">右元素</param>
        public string BinaryOperation(string left, BinaryOperator bot, string right)
        {
            NotNull(left, "left");
            NotNull(right, "right");
            switch (bot)
            {
                case BinaryOperator.Add:
                    return string.Concat(left, " + ", right);
                case BinaryOperator.Subtract:
                    return string.Concat(left, " - ", right);
                case BinaryOperator.Divide:
                    return string.Concat(left, " / ", right);
                case BinaryOperator.Multiply:
                    return string.Concat(left, " * ", right);
                case BinaryOperator.And:
                    var a = left.Contains(" OR ") || left.Contains(" AND ");
                    var b = right.Contains(" OR ") || right.Contains(" AND ");
                    if (a && b)
                    {
                        return string.Concat("(", left, ") AND (", right, ")");
                    }
                    else if (a)
                    {
                        return string.Concat("(", left, ") AND ", right);
                    }
                    else if (b)
                    {
                        return string.Concat(left, " AND (", right, ")");
                    }
                    return string.Concat(left, " AND ", right);
                case BinaryOperator.Equal:
                    if (right == "NULL" || right == null)
                    {
                        return string.Concat(left, " IS NULL");
                    }
                    return string.Concat(left, " = ", right);
                case BinaryOperator.GreaterThan:
                    return string.Concat(left, " > ", right);
                case BinaryOperator.GreaterThanOrEqual:
                    return string.Concat(left, " >= ", right);
                case BinaryOperator.NotEqual:
                    if (right == "NULL" || right == null)
                    {
                        return string.Concat(left, " IS NOT NULL");
                    }
                    return string.Concat(left, " <> ", right);
                case BinaryOperator.Or:
                    var c = left.Contains(" OR ") || left.Contains(" AND ");
                    var d = right.Contains(" OR ") || right.Contains(" AND ");
                    if (c && d)
                    {
                        return string.Concat("(", left, ") OR (", right, ")");
                    }
                    else if (c)
                    {
                        return string.Concat("(", left, ") OR ", right);
                    }
                    else if (d)
                    {
                        return string.Concat(left, " OR (", right, ")");
                    }
                    return string.Concat(left, " OR ", right);
                case BinaryOperator.LessThan:
                    return string.Concat(left, " < ", right);
                case BinaryOperator.LessThanOrEqual:
                    return string.Concat(left, " <= ", right);
                case BinaryOperator.Contains:
                    return LikeOperation(left, right, LikeOperator.Contains);
                case BinaryOperator.StartWith:
                    return LikeOperation(left, right, LikeOperator.StartWith);
                case BinaryOperator.EndWith:
                    return LikeOperation(left, right, LikeOperator.EndWith);
                case BinaryOperator.NotContains:
                    return LikeOperation(left, right, LikeOperator.NotContains);
                case BinaryOperator.NotStartWith:
                    return LikeOperation(left, right, LikeOperator.NotStartWith);
                case BinaryOperator.NotEndWith:
                    return LikeOperation(left, right, LikeOperator.NotEndWith);
                case BinaryOperator.BitAnd:
                    return BitOperation(left, right, BitOperator.And);
                case BinaryOperator.BitOr:
                    return BitOperation(left, right, BitOperator.Or);
                case BinaryOperator.BitXor:
                    return BitOperation(left, right, BitOperator.Xor);
                case BinaryOperator.Modulo:
                    return ModuloOperation(left, right);
                case BinaryOperator.Power:
                    return PowerOperation(left, right);
                case BinaryOperator.LeftShift:
                    return ShiftOperation(left, right, ShiftOperator.Left);
                case BinaryOperator.RightShift:
                    return ShiftOperation(left, right, ShiftOperator.Right);
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary> 获取实体类型所映射的表名
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="alias">别名</param>
        public string GetTable(Type type, string alias)
        {
            NotNull(type, "type");
            if (alias == null)
            {
                return TableName(type);
            }
            return GetColumn(TableName(type), alias);
        }
        /// <summary> 获取实体属性或字段所映射的列名
        /// </summary>
        /// <param name="table">表名或表别名</param>
        /// <param name="type">实体属性或字段</param>
        public string GetColumn(string table, MemberInfo member)
        {
            NotNull(member, "member");
            if (table == null)
            {
                return ColumnName(member);
            }
            return string.Concat(table, ".", ColumnName(member));
        }
        /// <summary> 获取列名和列别名组合后的sql表达式
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="alias">列别名</param>
        public string GetColumn(string columnName, string alias)
        {
            NotNull(columnName, "columnName");
            if (alias == null)
            {
                return columnName;
            }
            return string.Concat(columnName, _aliasSeparator, alias);
        }
        /// <summary> 将.NET中的方法解释为sql表达式
        /// </summary>
        /// <param name="method">需解释的方法</param>
        /// <param name="target">方法调用者</param>
        /// <param name="args">方法参数</param>
        /// <returns></returns>
        public string ParseMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            NotNull(method, "method");
            string sql = null;
            switch (Type.GetTypeCode(method.ReflectedType))
            {
                case TypeCode.Char:
                    sql = ParseCharMethod(method, target, args);
                    break;
                case TypeCode.String:
                    sql = ParseStringMethod(method, target, args);
                    break;
                case TypeCode.DateTime:
                    sql = ParseDateTimeMethod(method, target, args);
                    break;
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    sql = ParseNumberMethod(method, target, args);
                    break;
                default:
                    break;
            }
            if (sql == null)
            {
                sql = ParseOtherMethod(method, target, args);
            }
            return sql;
        }

        #region private ParseMethods
        /// <summary> 解释与数字类型相关的方法,并将方法分配给指定的方法
        /// </summary>
        private string ParseNumberMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    return SrtingToNumber(args[0].ToSql());
                case "ToString":
                    if (args.Length == 0)
                    {
                        return ObjectToString(DustType.Number, target.ToSql(), null);
                    }
                    var format = args[0].Value as string;
                    if (format != null && format.Contains("'"))
                    {
                        format = format.Replace("'", "''");
                    }
                    return ObjectToString(DustType.Number, target.ToSql(), "'" + format + "'");
                default:
                    break;
            }
            return null;
        }

        /// <summary> 解释与时间类型相关的方法,并将方法分配给指定的方法
        /// </summary>
        private string ParseDateTimeMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    return ObjectToString(DustType.DateTime, target.ToSql(), null);
                case "ParseExact":
                    var format = args[0].Value as string;
                    if (format != null && format.Contains("'"))
                    {
                        format = format.Replace("'", "''");
                    }
                    return ObjectToString(DustType.DateTime, args[0].ToSql(), "'" + format + "'");
                case "ToString":
                    if (args.Length == 0)
                    {
                        return ObjectToString(DustType.DateTime, target.ToSql(), null);
                    }
                    format = args[0].Value as string;
                    if (format != null && format.Contains("'"))
                    {
                        format = format.Replace("'", "''");
                    }
                    return ObjectToString(DustType.DateTime, target.ToSql(), "'" + format + "'");
                case "ToShortTimeString":
                    return ObjectToString(DustType.DateTime, target.ToSql(), "'HH:mm'");
                case "ToShortDateString":
                    return ObjectToString(DustType.DateTime, target.ToSql(), "'yyyy-MM-dd'");
                case "get_Year":
                    return DateTimeToField(target.ToSql(), DateTimeField.Year);
                case "get_Month":
                    return DateTimeToField(target.ToSql(), DateTimeField.Month);
                case "get_Day":
                    return DateTimeToField(target.ToSql(), DateTimeField.Day);
                case "get_Hour":
                    return DateTimeToField(target.ToSql(), DateTimeField.Hour);
                case "get_Minute":
                    return DateTimeToField(target.ToSql(), DateTimeField.Minute);
                case "get_Second":
                    return DateTimeToField(target.ToSql(), DateTimeField.Second);
                case "get_DayOfWeek":
                    return DateTimeToField(target.ToSql(), DateTimeField.Week);
                default:
                    break;
            }
            return null;
        }

        /// <summary> 解释与字符类型相关的方法,并将方法分配给指定的方法
        /// </summary>
        private string ParseCharMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    return args[0].ToSql();
                case "ToString":
                    return target.ToSql();
                default:
                    break;
            }
            return null;
        }

        /// <summary> 解释与字符串类型相关的方法,并将方法分配给指定的方法
        /// </summary>
        private string ParseStringMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Trim":
                    return StringTrim(target.ToSql(), GetTrimArg(args, 0));
                case "TrimEnd":
                    return StringTrimEnd(target.ToSql(), GetTrimArg(args, 0));
                case "TrimStart":
                    return StringTrimStart(target.ToSql(), GetTrimArg(args, 0));
                case "IsNullOrWhiteSpace":
                    return StringIsNullOrWhiteSpace(args[0].ToSql());
                case "IsNullOrEmpty":
                    return StringIsNullOrEmpty(args[0].ToSql());
                case "get_Length":
                    return StringLength(target.ToSql());
                case "ToString":
                    return target.ToSql();
                default:
                    break;
            }
            return null;
        }

        /// <summary> 获取并处理Trim方法所需的参数
        /// </summary>
        private string GetTrimArg(SawDust[] args, int index)
        {
            if (args == null || args.Length <= index)
            {
                return null;
            }
            var arr = (SawDust[])args[index].Value;
            var buff = new string[arr.Length + 2];
            buff[0] = "'";
            buff[buff.Length - 1] = "'";
            for (int i = 0; i < arr.Length; i++)
            {
                var it = arr[i];
                if (it.Type == DustType.Sql)
                {
                    throw new NotSupportedException("不支持当前操作");
                }
                else if (object.Equals(it.Value, '\''))
                {
                    buff[i + 1] = "''";
                }
                else
                {
                    buff[i + 1] = it.Value.ToString();
                }
            }
            return string.Concat(buff);
        }

        #endregion

        #region virtual system
        /// <summary> 解释Contains操作
        /// </summary>
        /// <param name="not">是否为not</param>
        /// <param name="item">要在集合中查找的对象</param>
        /// <param name="array">要查找的集合</param>
        public virtual string ContainsOperation(bool not, string element, string[] array)
        {
            NotNull(array, "array");
            NotNull(element, "element");

            if (array.Length > 1000)
            {
                var @in = not ? " NOT IN " : " IN ";
                var count = (array.Length + 999) / 1000;
                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                for (int i = 0; i < count; i += 1000)
                {
                    sb.Append(element);
                    sb.Append(@in);
                    sb.Append("(");
                    var jc = Math.Min(1000, count - i);
                    sb.Append(string.Join(", ", array, i, jc));
                    sb.Append(")");
                }
                sb.Append(")");
                return sb.ToString();
            }
            else if (not)
            {
                return string.Concat(element, " NOT IN (", string.Join(", ", array), ")");
            }
            else
            {
                return string.Concat(element, " IN (", string.Join(", ", array), ")");
            }
        }
        /// <summary> 向参数集合中追加一个任意类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="obj">需要追加的参数值</param>
        /// <param name="parameters">参数集合</param>
        public virtual string AddObject(object value, ICollection<DbParameter> parameters)
        {
            if (value == null || value is DBNull)
            {
                return "NULL";
            }
            NotNull(parameters, "parameters");
            var p = GetDbParameter(value);
            var name = "auto_p" + parameters.Count;
            p.ParameterName = name;
            parameters.Add(p);
            return ParameterPreFix + name;
        }
        /// <summary> 向参数集合中追加一个数字类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="number">需要追加的数字</param>
        /// <param name="parameters">参数集合</param>
        public virtual string AddNumber(IConvertible number, ICollection<DbParameter> parameters)
        {
            NotNull(number, "number");
            return number.ToString();
        }
        /// <summary> 向参数集合中追加一个布尔类型的参数,并返回参数名sql表达式
        /// </summary>
        /// <param name="obj">需要追加的布尔值</param>
        /// <param name="parameters">参数集合</param>
        public virtual string AddBoolean(bool value, ICollection<DbParameter> parameters)
        {
            return value ? "true" : "false";
        }
        /// <summary> 向参数集合中追加当前时间,并返回参数名sql表达式
        /// </summary>
        /// <param name="parameters">参数集合</param>
        public virtual string AddTimeNow(ICollection<DbParameter> parameters)
        {
            return TimeNow;
        }

        /// <summary> 解释其他方法,可重写
        /// </summary>
        /// <param name="method">需解释的方法</param>
        /// <param name="target">方法调用者</param>
        /// <param name="args">方法参数</param>
        protected virtual string ParseOtherMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            throw new NotSupportedException("无法解释方法:" + method.ToString());
        }
        /// <summary> 获取新的数据库参数
        /// </summary>
        /// <param name="obj">参数值</param>
        protected virtual DbParameter GetDbParameter(object obj)
        {
            var p = _factory.CreateParameter();
            p.Value = obj;
            return p;
        }
        /// <summary> 别名分隔符 默认 AS ,如 Table AS it
        /// </summary>
        protected virtual string AliasSeparator { get { return "AS"; } }
        #endregion

        #region abstract

        /// <summary> 返回实体类型所映射的表名
        /// </summary>
        /// <param name="type">实体类型</param>
        protected abstract string TableName(Type type);
        /// <summary> 返回实体属性或字段所映射的列名
        /// </summary>
        /// <param name="member">实体属性或字段</param>
        protected abstract string ColumnName(MemberInfo member);

        /// <summary> 参数的前缀符号,如SqlServer中的@,Oracle中的:
        /// </summary>
        protected abstract string ParameterPreFix { get; }
        /// <summary> 在数据库中表示当前时间的表达式,如SqlServer中的getdate,Oracle中的SYSDATE
        /// </summary>
        protected abstract string TimeNow { get; }

        /// <summary> 解释Like操作
        /// </summary>
        /// <param name="val1">操作数1</param>
        /// <param name="val2">操作数2</param>
        /// <param name="opt">按位操作符</param>
        protected abstract string LikeOperation(string val1, string val2, LikeOperator opt);

        #endregion

        #region 不支持当前操作,如有需要请重新实现

        /// <summary> 解释按位运算 与,或,非
        /// </summary>
        /// <param name="val1">操作数1</param>
        /// <param name="val2">操作数2</param>
        /// <param name="opt">按位操作符</param>
        protected virtual string BitOperation(string val1, string val2, BitOperator opt)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 BitOperation");
        }
        /// <summary> 解释取余运算
        /// </summary>
        /// <param name="val1">操作数1</param>
        /// <param name="val2">操作数2</param>
        /// <returns></returns>
        protected virtual string ModuloOperation(string val1, string val2)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 ModuloOperation");
        }
        /// <summary> 解释幂运算
        /// </summary>
        /// <param name="val1">操作数1</param>
        /// <param name="val2">操作数2</param>
        /// <returns></returns>
        protected virtual string PowerOperation(string val1, string val2)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 PowerOperation");
        }
        /// <summary> 解释位移运算
        /// </summary>
        /// <param name="val1">操作数1</param>
        /// <param name="val2">操作数2</param>
        /// <param name="opt">按位操作符</param>
        protected virtual string ShiftOperation(string val1, string val2, ShiftOperator opt)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 ShiftOperation");
        }
        /// <summary> 解释 String.Trim 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        /// <param name="arg">方法参数</param>
        protected virtual string StringTrim(string target, string arg)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodTrim");
        }
        /// <summary> 解释 String.TrimEnd 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        /// <param name="arg">方法参数</param>
        protected virtual string StringTrimEnd(string target, string arg)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodTrimEnd");
        }
        /// <summary> 解释 String.TrimStart 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        /// <param name="arg">方法参数</param>
        protected virtual string StringTrimStart(string target, string arg)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodTrimStart");
        }
        /// <summary> 解释 String.IsNullOrEmpty 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        protected virtual string StringIsNullOrEmpty(string target)
        {
            return string.Concat(target, " IS NULL OR ", target, " = ''");
        }
        /// <summary> 解释 String.IsNullOrWhiteSpace 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        protected virtual string StringIsNullOrWhiteSpace(string target)
        {
            return string.Concat(target, " IS NULL OR ", StringTrim(target, null), " = ''");
        }
        /// <summary> 解释 String.Length 方法
        /// </summary>
        /// <param name="target">方法调用者</param>
        protected virtual string StringLength(string target)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodLength");
        }
        /// <summary> 解释 Object.ToString 方法
        /// </summary>
        /// <param name="type">调用者类型</param>
        /// <param name="target">方法调用者</param>
        /// <param name="format">格式化参数</param>
        protected virtual string ObjectToString(DustType type, string target, string format)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodToString");
        }
        /// <summary> 解释 DateTime 中的数据
        /// </summary>
        /// <param name="datetime">方法调用者</param>
        protected virtual string DateTimeToField(string datetime, DateTimeField field)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodToMonthr");
        }
        /// <summary> 解释各种 Number.Parse 方法
        /// </summary>
        /// <param name="target">string对象</param>
        protected virtual string SrtingToNumber(string target)
        {
            throw new NotImplementedException("不支持当前操作,或请重新实现 MethodToNumber");
        } 

        #endregion
    }
}
