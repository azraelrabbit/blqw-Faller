using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace blqw
{
    /// <summary> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SourceNameAttribute : Attribute
    {

        public string Name { get; set; }

        public static string GetName(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }
            var attr = (SourceNameAttribute)Attribute.GetCustomAttribute(member, typeof(SourceNameAttribute));
            return (attr == null) ? member.Name : attr.Name;
        }
    }
}
