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
        public void TestMethod1()
        {
            var args = new[] { "1", "2", "3", "5" };
            object[] arr = { 1, 2, 3, 4, 5 };
            int[,] arr2 = { { 4 } };
            int[][] arr3 = { new[] { 5 } };
            var dict = new Dictionary<string, int>() { { "a", 3 } };



            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) == true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) != true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) == false));
            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) != false));


            Assert.AreEqual("a.ID = 1", Parse(u => u.ID == 1));
            Assert.AreEqual("a.Name IS NULL", Parse(u => u.Name == null));
            Assert.AreEqual("a.ID IN (1,2,3,4,5)", Parse(u => arr.Contains(u.ID)));
            Assert.AreEqual("a.NAME LIKE '%' || :auto_p0 || '%'", Parse(u => u.Name.Contains('a')));
            Assert.AreEqual("a.NAME NOT LIKE '%' || :auto_p0 || '%'", Parse(u => !u.Name.Contains("a")));
            Assert.AreEqual("a.ID IN (1,2,3,5)", Parse(u => args.Select(int.Parse).Contains(u.ID)));

        }
    }
}
