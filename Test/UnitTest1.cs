using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using blqw;

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
            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) == true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) != true));
            Assert.AreEqual("a.ID <> 1", Parse(u => (u.ID == 1) == false));
            Assert.AreEqual("a.ID = 1", Parse(u => (u.ID == 1) != false));
        }
    }
}
