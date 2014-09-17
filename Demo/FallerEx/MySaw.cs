using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demo
{
    class MySaw : blqw.SqlServerSaw
    {
        protected override string ColumnName(System.Reflection.MemberInfo member)
        {
            var attr = (DatabaseObjectAttribute)Attribute.GetCustomAttribute(member, typeof(DatabaseObjectAttribute));
            if (attr == null || string.IsNullOrWhiteSpace(attr.Name))
            {
                return base.ColumnName(member);
            }
            return attr.Name;
        }

        protected override string TableName(Type type)
        {
            var attr = (DatabaseObjectAttribute)Attribute.GetCustomAttribute(type, typeof(DatabaseObjectAttribute));
            if (attr == null || string.IsNullOrWhiteSpace(attr.Name))
            {
                return base.TableName(type);
            }
            return attr.Name;
        }
    }
}
