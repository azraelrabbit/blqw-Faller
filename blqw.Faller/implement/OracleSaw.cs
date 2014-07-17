using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Reflection;

namespace blqw
{
    public class OracleSaw : ISaw
    {
        public static readonly OracleSaw Instance = new OracleSaw();

        public string BinaryOperator(string left, BinaryOperatorType bot, string right)
        {
            switch (bot)
            {
                case BinaryOperatorType.Add:
                    return string.Concat(left, "+", right);
                case BinaryOperatorType.Subtract:
                    return string.Concat(left, "-", right);
                case BinaryOperatorType.Divide:
                    return string.Concat(left, "/", right);
                case BinaryOperatorType.Multiply:
                    return string.Concat(left, "*", right);
                case BinaryOperatorType.And:
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
                case BinaryOperatorType.Equal:
                    if (right == "NULL" || right == null)
                    {
                        return string.Concat(left, " IS NULL");
                    }
                    return string.Concat(left, " = ", right);
                case BinaryOperatorType.GreaterThan:
                    return string.Concat(left, " > ", right);
                case BinaryOperatorType.GreaterThanOrEqual:
                    return string.Concat(left, " >= ", right);
                case BinaryOperatorType.NotEqual:
                    if (right == "NULL" || right == null)
                    {
                        return string.Concat(left, " IS NOT NULL");
                    }
                    return string.Concat(left, " <> ", right);
                case BinaryOperatorType.Or:
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
                case BinaryOperatorType.LessThan:
                    return string.Concat(left, " < ", right);
                case BinaryOperatorType.LessThanOrEqual:
                    return string.Concat(left, " <= ", right);
                case BinaryOperatorType.Contains:
                    return string.Concat(left, " LIKE '%' || ", right, " || '%'");
                case BinaryOperatorType.StartWith:
                    return string.Concat(left, " LIKE ", right, " || '%'");
                case BinaryOperatorType.EndWith:
                    return string.Concat(left, " LIKE '%' || ", right);
                case BinaryOperatorType.NotContains:
                    return string.Concat(left, " NOT LIKE '%' || ", right, " || '%'");
                case BinaryOperatorType.NotStartWith:
                    return string.Concat(left, " NOT LIKE ", right, " || '%'");
                case BinaryOperatorType.NotEndWith:
                    return string.Concat(left, " NOT LIKE '%' || ", right);
                case BinaryOperatorType.BitAnd:
                    return string.Concat("BITAND(", left, ", ", right, ")");
                case BinaryOperatorType.BitOr:
                    //(x + y) - BITAND(x, y)
                    return string.Concat("((", left, " + ", right, ") - BITAND(", left, ", ", right, "))");
                case BinaryOperatorType.BitXor:
                    //BITAND(x,y) = (x + y) - BITAND(x, y) * 2
                    return string.Concat("((", left, " + ", right, ") - BITAND(", left, ", ", right, ") * 2)");
                case BinaryOperatorType.Modulo:
                case BinaryOperatorType.Power:
                case BinaryOperatorType.LeftShift:
                case BinaryOperatorType.RightShift:
                default:
                    throw new NotImplementedException();
            }
        }

        public string Contains(bool not, string element, string[] array)
        {
            if (array.Length > 1000)
            {
                var flag = not ? " NOT IN " : " IN ";
                var count = (array.Length + 999) / 1000;
                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                for (int i = 0; i < count; i += 1000)
                {
                    sb.Append(element);
                    sb.Append(flag);
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

        public string ConcatSqls(params string[] sqls)
        {
            return string.Join(" || ", sqls);
        }


        public string GetTable(Type type, string alias)
        {
            if (alias == null)
            {
                return type.Name.ToUpper();
            }
            return string.Concat(type.Name.ToUpper(), " ", alias);
        }

        public string GetColumn(string table, MemberInfo member)
        {
            if (table == null)
            {
                return member.Name.ToUpper();
            }
            return string.Concat(table, ".", member.Name.ToUpper());
        }

        public string GetColumn(string columnName, string alias)
        {
            if (alias == null)
            {
                return columnName;
            }
            return string.Concat(columnName, " ", alias);
        }



        public string AddObject(object value, ICollection<DbParameter> parameters)
        {
            if (value == null || value is DBNull)
            {
                return "NULL";
            }
            var name = "auto_p" + parameters.Count;
            var p = new OracleParameter(name, value);
            parameters.Add(new OracleParameter(name, value));
            return ":" + name;
        }

        public string AddNumber(IConvertible number, ICollection<DbParameter> parameters)
        {
            return number.ToString();
        }

        public string AddBoolean(bool value, ICollection<DbParameter> parameters)
        {
            return value ? "1" : "0";
        }

        public string AddTimeNow(ICollection<DbParameter> parameters)
        {
            return "SYSDATE";
        }

        public string CallMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            string sql = null;
            switch (Type.GetTypeCode(method.ReflectedType))
            {
                case TypeCode.Char:
                    sql = CallCharMethod(method, target, args);
                    break;
                case TypeCode.String:
                    sql = CallStringMethod(method, target, args);
                    break;
                case TypeCode.DateTime:
                    sql = CallDateTimeMethod(method, target, args);
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
                    sql = CallNumberMethod(method, target, args);
                    break;
                default:
                    break;
            }
            if (sql == null)
            {
                throw new NotSupportedException("无法解释方法:" + method.ToString());
            }
            return sql;
        }

        private string CallNumberMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    if (args.Length <= 0)
                    {
                        return null;
                    }
                    return string.Concat("CAST(", args[0].ToSql(), " AS NUMBER)");
                case "ToString":
                    return string.Concat("CAST(", target.ToSql(), " AS NVARCHAR2(100))");
                default:
                    break;
            }
            return null;
        }

        private string CallDateTimeMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    if (args.Length == 0)
                    {
                        return null;
                    }
                    return string.Concat("CAST( ", args[0].ToSql(), "AS NVARCHAR(2000))");
                case "ToString":
                    if (args.Length > 0 && args[0].Value is string)
                    {
                        var format = ((string)args[0].Value).Replace("m", "mi").Replace("mimi", "mi");
                        return string.Concat("TO_CHAR(", target.ToSql(), ",'", format, "')");
                    }
                    return string.Concat("CAST(", target.ToSql(), " AS NVARCHAR2(100))");
                case "ToShortTimeString":
                    return string.Concat("TO_CHAR(", target.ToSql(), ",'HH:mi')");
                case "ToShortDateString":
                    return string.Concat("TO_CHAR(", target.ToSql(), ",'yyyy-MM-dd')");
                case "get_Year":
                    return string.Concat("EXTRACT(YEAR FROM ", target.ToSql(), ")");
                case "get_Month":
                    return string.Concat("EXTRACT(MONTH FROM ", target.ToSql(), ")");
                case "get_Day":
                    return string.Concat("EXTRACT(DAY FROM ", target.ToSql(), ")");
                case "get_Hour":
                    return string.Concat("EXTRACT(HOUR FROM ", target.ToSql(), ")");
                case "get_Minute":
                    return string.Concat("EXTRACT(MINUTE FROM ", target.ToSql(), ")");
                case "get_Second":
                    return string.Concat("EXTRACT(SECOND FROM ", target.ToSql(), ")");
                default:
                    break;
            }
            if (method.Name == "Parse")
            {
                return string.Concat("CAST( ", args[0].ToSql(), "AS NVARCHAR(2000))");
            }
            return null;
        }

        private string DateTimeToString(string datetime, string format)
        {
            format = format.Replace("m", "mi").Replace("mimi", "mi");
            return string.Concat("TO_CHAR(", datetime, ",'", format, "')");
        }

        private string CallStringMethod(MethodInfo method, SawDust target, SawDust[] args)
        {

            switch (method.Name)
            {
                case "Trim":
                    return string.Concat("ltrim(rtrim(", target.ToSql(), "))");
                case "TrimEnd":
                    return string.Concat("rtrim(", target.ToSql(), ")");
                case "TrimStart":
                    return string.Concat("ltrim(", target.ToSql(), ")");
                case "IsNullOrWhiteSpace":
                    if (args.Length == 0)
                    {
                        return null;
                    }
                    var sql = args[0].ToSql();
                    return string.Concat(sql, " IS NULL OR ltrim(rtrim(", sql, ")) == ''");
                case "IsNullOrEmpty":
                    if (args.Length == 0)
                    {
                        return null;
                    }
                    sql = args[0].ToSql();
                    return string.Concat(sql, " IS NULL OR ", sql, " == ''");
                case "ToString":
                    return target.ToSql();
                default:
                    break;
            }
            return null;
        }

        private string CallCharMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            switch (method.Name)
            {
                case "Parse":
                    if (args.Length == 0)
                    {
                        return null;
                    }
                    return string.Concat("CAST( ", args[0].ToSql(), "AS NVARCHAR(1))");
                case "ToString":
                    return string.Concat("CAST( ", target.ToSql(), "AS NVARCHAR(1))");
                default:
                    break;
            }
            return null;
        }

        public string OrderBy(string sql, bool asc)
        {
            return string.Concat(sql, asc ? " ASC" : " DESC");
        }

        public string UpdateSet(string column, string value)
        {
            return string.Concat(column, " = ", value);
        }



    }
}
