using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace blqw
{
    [Serializable]
    public enum DustType
    {
        Undefined = 0,
        Sql = 1,
        Object = 2,
        Number = 3,
        Array = 4,
        Boolean = 5,
        DateTime = 6,
        Binary = 7,
        String = 8,
    }

    public struct SawDust
    {
        internal SawDust(Faller parser, DustType type, Object value)
        {
            Type = type;
            Value = value;
            Parser = parser;
        }

        private readonly Faller Parser;

        public readonly DustType Type;

        public readonly Object Value;

        public string ToSql()
        {
            switch (Type)
            {
                case DustType.Sql:
                    return (string)Value;
                case DustType.Number:
                    return Parser.AddNumber((IConvertible)Value);
                case DustType.Array:
                    throw new NotImplementedException();
                case DustType.Boolean:
                    return Parser.AddBoolean((bool)Value);
                case DustType.Object:
                case DustType.DateTime:
                case DustType.Binary:
                case DustType.String:
                    return Parser.AddObject(Value);
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
                return Value == dust.Value && object.ReferenceEquals(Parser, dust.Parser);
            }
            return false;
        }

    }
}
