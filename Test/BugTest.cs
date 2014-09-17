using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using blqw;
using System.Linq.Expressions;

namespace Test
{
    [TestClass]
    public class MyTestClass
    {
        void Where(string expected, Expression<Func<User, bool>> expr)
        {
            var parse = Faller.Create(expr);
            var sql = parse.ToWhere(OracleSaw.Instance);
            //parse.Parameters  //解析过程中得到的参数
            Assert.AreEqual(expected, sql);
        }
        void OrderBy(string expected, Expression<Func<User, object>> expr, bool asc)
        {
            var sql = Faller.Create(expr).ToOrderBy(OracleSaw.Instance, asc);
            Assert.AreEqual(expected, sql);
        }
        void Set(string expected, Expression<Func<User>> expr)
        {
            var sql = Faller.Create(expr).ToSets(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }
        void Columns(string expected, Expression<Func<User, object>> expr)
        {
            var sql = Faller.Create(expr).ToSelectColumns(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }
        void Values(string expected, Expression<Func<User, object>> expr)
        {
            var sql = Faller.Create(expr).ToValues(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }
        void ColumnsAndValues(string expected1, string expected2, Expression<Func<User, object>> expr)
        {
            var sql = Faller.Create(expr).ToColumnsAndValues(OracleSaw.Instance);
            Assert.AreEqual(expected1, sql.Key);
            Assert.AreEqual(expected2, sql.Value);
        }

        [TestMethod]
        public void String_Contains()
        {
            Where("NAME IN (:auto_p0, :auto_p1, :auto_p2)", u => "aaa,bbb,ccc".Split(',').Contains(u.Name));
        }
    }
}
