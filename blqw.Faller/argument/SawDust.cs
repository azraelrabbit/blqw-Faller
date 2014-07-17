using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace blqw
{
    public struct SawDust
    {
        internal SawDust(Faller faller, DustType type, Object value)
        {
            Type = type;
            Value = value;
            Faller = faller;  
        }

        private readonly Faller Faller;

        public readonly DustType Type;

        public readonly Object Value;

        public string ToSql()
        {
            switch (Type)
            {  
                case DustType.Sql:
                    return (string)Value;
                case DustType.Number:
                    return Faller.AddNumber((IConvertible)Value);
                case DustType.Array:
                    throw new NotImplementedException();
                case DustType.Boolean:
                    return Faller.AddBoolean((bool)Value);
                case DustType.Object:
                case DustType.DateTime:
                case DustType.Binary:
                case DustType.String:
                    return Faller.AddObject(Value);
                default:
                    throw new NotImplementedException();
            }
        }

        public bool IsObject
        {
            get
            {
                return Type < 0 || Type > DustType.Sql;
            }
        }

        public override int GetHashCode()
        {
            if (Type == DustType.Undefined)
            {
                return 0;
            }
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SawDust == false)
            {
                return false;
            }
            var dust = (SawDust)obj;
            if (Type == dust.Type)
            {
                if (Type == 0)
                {
                    return true;
                }
                return Value == dust.Value && object.ReferenceEquals(Faller, dust.Faller);
            }
            return false;
        }

    }
}
