using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demo
{
    class DatabaseObjectAttribute : Attribute
    {
        public DatabaseObjectAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }
    }
}
