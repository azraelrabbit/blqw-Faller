using System;
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

            Where<User>(u => u.Name.Contains('a'));
            //OrderBy<User>(u => new { u.Name, u.ID });
            //OrderBy<User>(u => new { u.Name, u.ID });
            //OrderBy<User>(u => u.Name);

            //Set(() => new User { Name = "aaaa" });
            //Set(() => new User { Sex = true });
            //Set(() => new User { ID = 1 });

            Where<User>(u => (u.ID == 1) == false);
            Where<User>(u => (u.ID == 1) == true);
            Where<User>(u => (u.ID == 1) != true);
            Where<User>(u => (u.ID == 1) != false);

            args = new[] { "1", "2", "3", "5" };
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

            Where<User>(u => !arr.Contains(u.ID) &&
                    (!u.Name.Contains("a") == false || u.Name == null) &&
                    (u.ID & 4) == 4 &&
                    u.Birthday < DateTime.Now);

        }

        public static void Set<T>(Expression<Func<T>> expr)
        {
            Console.WriteLine(expr);
        }

        public static void OrderBy<T>(Expression<Func<T, object>> expr)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            var parse = new Faller(expr);
            var sql = parse.ToOrderBy(OracleSaw.Instance, true);
            Console.WriteLine("Parsed : " + sql);
        }



        public static void Where<T>(Expression<Func<T, bool>> expr)
        {
            Console.WriteLine("Expr   : " + expr.Body.ToString());
            Console.WriteLine();
            var parse = new Faller(expr);
            var sql = parse.ToWhere(OracleSaw.Instance);
            Console.WriteLine("Parsed : " + sql);
            Console.WriteLine();
            foreach (var p in parse.Parameters)
            {
                Console.WriteLine("参数 {0} : {1}", p.ParameterName, p.Value);
            }
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
