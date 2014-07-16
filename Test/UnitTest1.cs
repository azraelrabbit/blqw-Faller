﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using blqw;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        class User
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public DateTime Birthday { get; set; }
            public bool Sex { get; set; }
        }

        void Where(string expected, Expression<Func<User, bool>> expr)
        {
            var parse = new Faller(expr);
            var sql = parse.ToWhere(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }

        [TestMethod]
        public void WhereTest()
        {
            var args = new[] { "1", "2", "3", "5" };
            object[] arr = { 1, 2, 3, 4, 5 };
            int[,] arr2 = { { 4 } };
            int[][] arr3 = { new[] { 5 } };
            var dict = new Dictionary<string, int>() { { "a", 3 } };
            var date = new DateTime(2014, 7, 15);
            string id = "1";


            //一元表达式转换
            Where("a.ID = 1", u => (u.ID == 1) == true);
            Where("a.ID <> 1", u => (u.ID == 1) != true);
            Where("a.ID <> 1", u => (u.ID == 1) == false);
            Where("a.ID = 1", u => (u.ID == 1) != false);
            Where("a.SEX = 1", u => u.Sex);

            //一般
            Where("a.ID = 1", u => u.ID == 1);
            Where("a.NAME IS NULL", u => u.Name == null);
            //IN
            Where("a.ID IN (1, 2, 3, 4, 5)", u => arr.Contains(u.ID));
            Where("a.ID IN (1, 2, 3, 5)", u => args.Select(int.Parse).Contains(u.ID));
            //LIKE
            Where("a.NAME LIKE '%' || :auto_p0 || '%'", u => u.Name.Contains('a'));
            Where("a.NAME NOT LIKE '%' || :auto_p0 || '%'", u => !u.Name.Contains("a"));
            Where("a.NAME LIKE '%' || :auto_p0", u => u.Name.EndsWith("a"));

            //类型转换
            Where("a.ID = 1", u => u.ID == int.Parse(id));
            Where("a.ID = 1", u => u.ID == Convert.ToInt32(id));
            Where("CAST(a.ID AS NVARCHAR2(100)) = :auto_p0", u => u.ID.ToString() == id);

            //处理时间类型
            Where("a.BIRTHDAY < SYSDATE", u => u.Birthday < DateTime.Now);
            Where("CAST(a.BIRTHDAY AS NVARCHAR2(100)) = CAST(SYSDATE AS NVARCHAR2(100))", u => u.Birthday.ToString() == DateTime.Now.ToString());
            Where("TO_CHAR(a.BIRTHDAY,'HHmiss') = TO_CHAR(SYSDATE,'HHmiss')", u => u.Birthday.ToString("HHmmss") == DateTime.Now.ToString("HHmmss"));
            Where("TO_CHAR(a.BIRTHDAY,'yyyy-MM-dd') = :auto_p0", u => u.Birthday.ToShortDateString() == date.ToShortDateString());
            Where("EXTRACT(DAY FROM a.BIRTHDAY) = 1", u => u.Birthday.Day == 1);



            Where("ltrim(rtrim(a.NAME)) = :auto_p0", u => u.Name.Trim() == "");
            Where("a.NAME IS NULL OR a.NAME == ''", u => string.IsNullOrEmpty(u.Name));

            //组合表达式
            Where("((a.ID IN (1, 2, 3, 4, 5) AND (a.NAME LIKE '%' || :auto_p0 || '%' OR a.NAME IS NULL)) AND BITAND(a.ID, 4) = 4) AND a.BIRTHDAY < SYSDATE",
                u => !arr.Contains(u.ID) &&
                   (!u.Name.Contains("a") == false || u.Name == null) &&
                   (u.ID & 4) == 4 &&
                   u.Birthday < DateTime.Now);


        }

        void OrderBy(string expected, Expression<Func<User, object>> expr, bool asc)
        {
            var parse = new Faller(expr);
            var sql = parse.ToOrderBy(OracleSaw.Instance, asc);
            Assert.AreEqual(expected, sql);
        }

        [TestMethod]
        public void OrderByTest()
        {
            OrderBy("a.NAME ASC, a.ID ASC", u => new { u.Name, u.ID }, true);
            OrderBy("a.NAME ASC, a.ID ASC", u => new object[] { u.Name, u.ID }, true);
            OrderBy("a.NAME ASC", u => u.Name, true);
            OrderBy("SYSDATE DESC", u => DateTime.Now, false);
            OrderBy("rownum DESC", u => (SqlExpr)"rownum", false);
            OrderBy("1 DESC", u => 1, false);
        }

        void Set(string expected, Expression<Func<User>> expr)
        {
            var parse = new Faller(expr);
            var sql = parse.ToSet(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }

        [TestMethod]
        public void SetTest()
        {
            Set("NAME = :auto_p0", () => new User { Name = "aaaa" });
            Set("SEX = 1", () => new User { Sex = true });
            Set("ID = 1", () => new User { ID = 1 });
            Set("ID = 1, NAME = :auto_p0, SEX = 0, BIRTHDAY = SYSDATE", () => new User { ID = 1, Name = "bbbb", Sex = false, Birthday = DateTime.Now });
        }

        void Columns(string expected, Expression<Func<User, object>> expr)
        {
            var parse = new Faller(expr);
            var sql = parse.ToColumns(OracleSaw.Instance);
            Assert.AreEqual(expected, sql);
        }

        [TestMethod]
        public void ColumnsTest()
        {
            Columns("a.*", u => null);
            Columns("a.NAME", u => new { u.Name });
            Columns("a.ID, a.SEX", u => new { u.ID, u.Sex });
            Columns("a.NAME UserName", u => new { UserName = u.Name });
            Columns("SYSDATE DateTime, 1 X", u => new { DateTime = DateTime.Now, X = 1 });
        }



        [TestMethod]
        public void SqlExprTest()
        {
            Columns("rownum row_id", u => new { row_id = (SqlExpr)"rownum" });
            Set("ID = rownum", () => new User { ID = (SqlExpr)"rownum" });
            OrderBy("rownum DESC", u => (SqlExpr)"rownum", false);
            Where("a.ID = rownum", u => u.ID == (SqlExpr)"rownum");
            Where("rownum < 10", u => (SqlExpr)"rownum < 10");
            Where("rownum < 10 AND a.ID > 10", u => (SqlExpr)"rownum < 10" && u.ID > 10);
        }
    }
}
