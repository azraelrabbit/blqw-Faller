using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace blqw
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SourceNameAttribute : Attribute
    {
        public SourceNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public static string GetName(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }
            var attr = Attribute.GetCustomAttribute(member, typeof(Attribute));
            return (attr == null) ? member.Name : ((SourceNameAttribute)attr).Name;
        }
    }
}
