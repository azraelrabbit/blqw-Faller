using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demo
{
    [DatabaseObject("test")]
    public class User
    {
        [DatabaseObject("id")]
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public bool Sex { get; set; }
    }
}
