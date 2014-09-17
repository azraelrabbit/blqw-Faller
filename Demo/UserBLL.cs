using blqw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Data.SqlClient;

namespace Demo
{
    public static class UserBLL
    {
        public static List<User> GetUsers(Expression<Func<User, bool>> where)
        {
            var parse = Faller.Create(where);
            var sql = parse.ToWhere(new MySaw());
            using (var conn = new SqlConnection("Data Source=.;Initial Catalog=Test;Integrated Security=True"))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from test where " + sql;
                cmd.Parameters.AddRange(parse.Parameters.ToArray());
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    return Convert2.ToList<User>(reader);
                }
            }
        }
    }
}
