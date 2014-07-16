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

        string ConcatSqls(params string[] sqls);

        string AddObject(object obj, ICollection<DbParameter> parameters);
        string AddNumber(IConvertible number, ICollection<DbParameter> parameters);
        string AddBoolean(bool value, ICollection<DbParameter> parameters);
        string AddTimeNow(ICollection<DbParameter> parameters);

        string GetTableName(Type type, string alias);
        string GetColumnName(string alias, MemberInfo member);

        string CallMethod(MethodInfo method, SawDust target, SawDust[] args);

        string OrderBy(string _sql, bool asc);

        string OrderBy(string[] sqls, bool asc);
    }
}
