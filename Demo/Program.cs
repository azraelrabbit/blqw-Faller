﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using blqw;
using System.Linq.Expressions;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            Expression<Func<User, object>> expr = u => u.Birthday.DayOfWeek == DayOfWeek.Sunday;

            var sql = Faller.Create(expr).ToWhere(MsSqlSaw.Instance);

            Console.WriteLine(sql);

            Where<User>(u => u.Name.Trim(',', ' ') == "");
            //DemoColumnsAndValues();
            //DemoValues();
            //DemoSqlExpr();
            //DemoWhere();
            //DemoSet();
            //DemoOrderBy();
            //DemoColumns();
        }



        #region Demo

        static void DemoSqlExpr()
        {
            Where<User>(u => (SqlExpr)"rownum < 10" && u.ID > 10);
            Columns<User>(u => new { row_id = (SqlExpr)"rownum" });
            Set(() => new User { ID = (SqlExpr)"rownum" });
            OrderBy<User>(u => (SqlExpr)"rownum", false);
            Where<User>(u => u.ID == (SqlExpr)"rownum");
        }

        static void DemoWhere()
        {
            Where<User>(u => (u.ID == 1) == false);
            Where<User>(u => (u.ID == 1) == true);
            Where<User>(u => (u.ID == 1) != true);
            Where<User>(u => (u.ID == 1) != false);

            var args = new[] { "1", "2", "3", "5" };
            object[] arr = { 1, 2, 3, 4, 5 };
            Where<User>(u => u.ID == (int)arr[3]);

            int[,] arr2 = { { 4 } };
            Where<User>(u => u.ID == arr2[0, 0]);

            int[][] arr3 = { new[] { 5 } };
            Where<User>(u => u.ID == arr3[0][0]);

            Dictionary<string, int> dict = new Dictionary<string, int>() { { "a", 3 } };

            Where<User>(u => u.ID == dict["a"]);


            Where<User>(u => u.ID == 1);
            Where<User>(u => u.Name == null);
            Where<User>(u => arr.Contains(u.ID));
            Where<User>(u => (!u.Name.Contains("a") == false) == true);
            Where<User>(u => args.Select(int.Parse).Contains(u.ID));

            string id = "1";
            Where<User>(u => u.ID == int.Parse(id));
            Where<User>(u => u.ID == Convert.ToInt32(id));
            Where<User>(u => u.ID.ToString() == id);
            Where<User>(u => u.Birthday.ToString() == DateTime.Now.ToString());
            DateTime date = new DateTime(2014, 7, 15);
            Where<User>(u => u.Birthday.ToShortDateString() == date.ToShortDateString());
            Where<User>(u => u.Birthday.Day == 1);

            Where<User>(u => !arr.Contains(u.ID) &&
                    (!u.Name.Contains("a") == false || u.Name == null) &&
                    (u.ID & 4) == 4 &&
                    u.Birthday < DateTime.Now);
        }

        static void DemoSet()
        {
            Set(() => new User { Name = "aaaa" });
            Set(() => new User { Sex = true });
            Set(() => new User { ID = 1 });
            Set(() => new User { ID = 1, Name = "bbbb", Sex = false, Birthday = DateTime.Now });

        }

        static void DemoOrderBy()
        {
            OrderBy<User>(u => new { u.Name, u.ID }, true);
            OrderBy<User>(u => new object[] { u.Name, u.ID }, true);
            OrderBy<User>(u => u.Name, true);
            OrderBy<User>(u => DateTime.Now, false);
            OrderBy<User>(u => (SqlExpr)"rownum", false);
            OrderBy<User>(u => 1, false);
        }

        static void DemoColumns()
        {
            Columns<User>(u => null);
            Columns<User>(u => new { u.Name });
            Columns<User>(u => new { u.ID, u.Sex });
            Columns<User>(u => new { UserName = u.Name });
            Columns<User>(u => new { DateTime = DateTime.Now, X = 1 });
        }

        static void DemoValues()
        {
            Values<User>(u => new object[] { u.Name, u.ID });
            Values<User>(u => DateTime.Now);
            Values<User>(u => (SqlExpr)"'xyz'");
            Values<User>(u => new int[] { 1, 2, 3, 4, 5 });
            Values<User>(u => u.Birthday);
        }

        static void DemoColumnsAndValues()
        {
            ColumnsAndValues<User>(u => new User { ID = (SqlExpr)"seq_table.nextval", Name = "aaaa", Sex = true, Birthday = DateTime.Now });
        }
        #endregion

        public static void Values<T>(Expression<Func<T, object>> expr)
        {
            CommonTo(Create(expr).ToValues);
        }

        public static void ColumnsAndValues<T>(Expression<Func<T, object>> expr)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            var parse = Faller.Create(expr);
            var sqls = parse.ToColumnsAndValues(OracleSaw.Instance);
            Console.WriteLine("Columns : " + sqls.Key);
            Console.WriteLine("Values : " + sqls.Value);
            Console.WriteLine();
            foreach (var p in parse.Parameters)
            {
                Console.WriteLine("参数 {0} : {1}", p.ParameterName, p.Value);
            }
            Console.WriteLine(new string('.', Console.BufferWidth - 1));
        }

        public static void Columns<T>(Expression<Func<T, object>> expr)
        {
            CommonTo(Create(expr).ToSelectColumns);
        }

        public static void Set<T>(Expression<Func<T>> expr)
        {
            CommonTo(Create(expr).ToSets);
        }

        public static void OrderBy<T>(Expression<Func<T, object>> expr, bool asc)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            Console.WriteLine("ASC    : " + asc);
            var parse = Faller.Create(expr);
            var sql = parse.ToOrderBy(OracleSaw.Instance, asc);
            Console.WriteLine("Parsed : " + sql);
            Console.WriteLine(new string('.', Console.BufferWidth - 1));
        }

        public static void Where<T>(Expression<Func<T, bool>> expr)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            Console.WriteLine();
            var parse = Faller.Create(expr);
            var sql = parse.ToWhere(OracleSaw.Instance);
            Console.WriteLine("Parsed : " + sql);
            Console.WriteLine();
            foreach (var p in parse.Parameters)
            {
                Console.WriteLine("参数 {0} : {1}", p.ParameterName, p.Value);
            }
            Console.WriteLine(new string('.', Console.BufferWidth - 1));
        }

        private static IFaller Create(LambdaExpression expr)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            Console.WriteLine();
            return Faller.Create(expr);
        }
        private static void CommonTo(Func<ISaw, string> func)
        {
            var sql = func(OracleSaw.Instance);
            Console.WriteLine("Parsed : " + sql);
            Console.WriteLine(new string('.', Console.BufferWidth - 1));
        }

        class User
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public DateTime Birthday { get; set; }
            public bool Sex { get; set; }
        }
    }
}
