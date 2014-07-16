using System;
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

        string Parse(Expression<Func<User, bool>> expr)
        {
            var parse = new Faller(expr);
            return parse.ToWhere(OracleSaw.Instance);
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
            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) == true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) != true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) == false));
            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) != false));
            Assert.AreEqual("a.SEX = 1", Parse(u => u.Sex));

            //一般
            Assert.AreEqual("a.ID = 1", Parse(u => u.ID == 1));
            Assert.AreEqual("a.NAME IS NULL", Parse(u => u.Name == null));
            //IN
            Assert.AreEqual("a.ID IN (1,2,3,4,5)", Parse(u => arr.Contains(u.ID)));
            Assert.AreEqual("a.ID IN (1,2,3,5)", Parse(u => args.Select(int.Parse).Contains(u.ID)));
            //LIKE
            Assert.AreEqual("a.NAME LIKE '%' || :auto_p0 || '%'", Parse(u => u.Name.Contains('a')));
            Assert.AreEqual("a.NAME NOT LIKE '%' || :auto_p0 || '%'", Parse(u => !u.Name.Contains("a")));
            Assert.AreEqual("a.NAME LIKE '%' || :auto_p0", Parse(u => u.Name.EndsWith("a")));

            //类型转换
            Assert.AreEqual("a.ID = 1", Parse(u => u.ID == int.Parse(id)));
            Assert.AreEqual("a.ID = 1", Parse(u => u.ID == Convert.ToInt32(id)));
            Assert.AreEqual("CAST(a.ID AS NVARCHAR2(100)) = :auto_p0", Parse(u => u.ID.ToString() == id));

            //处理时间类型
            Assert.AreEqual("CAST(a.BIRTHDAY AS NVARCHAR2(100)) = CAST(SYSDATE AS NVARCHAR2(100))", Parse(u => u.Birthday.ToString() == DateTime.Now.ToString()));
            Assert.AreEqual("TO_CHAR(a.BIRTHDAY,'HHmiss') = TO_CHAR(SYSDATE,'HHmiss')", Parse(u => u.Birthday.ToString("HHmmss") == DateTime.Now.ToString("HHmmss")));
            Assert.AreEqual("TO_CHAR(a.BIRTHDAY,'yyyy-MM-dd') = :auto_p0", Parse(u => u.Birthday.ToShortDateString() == date.ToShortDateString()));
            Assert.AreEqual("EXTRACT(DAY FROM a.BIRTHDAY) = 1", Parse(u => u.Birthday.Day == 1));

            //组合表达式
            Assert.AreEqual("((a.ID IN (1,2,3,4,5) AND (a.NAME LIKE '%' || :auto_p0 || '%' OR a.NAME IS NULL)) AND BITAND(a.ID,4) = 4) AND a.BIRTHDAY < SYSDATE",
                Parse(u => !arr.Contains(u.ID) &&
                   (!u.Name.Contains("a") == false || u.Name == null) &&
                   (u.ID & 4) == 4 &&
                   u.Birthday < DateTime.Now));

        }
    }
}
