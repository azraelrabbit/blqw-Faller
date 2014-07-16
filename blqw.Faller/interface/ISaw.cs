using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Data.Common;
using System.Reflection;

namespace blqw
{
    public interface ISaw
    {
        string BinaryOperator(string left, BinaryOperatorType @operator, string right);
        string Contains(bool not, string element, string[] array);

        string AddObject(object obj, ICollection<DbParameter> parameters);
        string AddNumber(IConvertible number, ICollection<DbParameter> parameters);
        string AddBoolean(bool value, ICollection<DbParameter> parameters);
        string AddTimeNow(ICollection<DbParameter> parameters);

        string GetTable(Type type, string alias);
        string GetColumn(string table, MemberInfo member);
        string GetColumn(string columnName, string alias);

        string CallMethod(MethodInfo method, SawDust target, SawDust[] args);

        string OrderBy(string sql, bool asc);

        string UpdateSet(string column, string value);
    }
}
