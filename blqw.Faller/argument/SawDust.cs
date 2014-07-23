using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace blqw
{
    /// <summary> 表达式树解析结果
    /// </summary>
    public struct SawDust
    {
        /// <summary> 初始化解析结果
        /// </summary>
        /// <param name="faller">解析组件</param>
        /// <param name="type">结果类型</param>
        /// <param name="value">结果值</param>
        internal SawDust(Faller faller, DustType type, Object value)
        {
            if (value is SawDust)
            {
                var dust = (SawDust)value;
                Value = dust.Value;
                Type = dust.Type;
            }
            else
            {
                Type = type;
                Value = value;
            }
            Faller = faller;  
        }

        /// <summary> 解析组件
        /// </summary>
        private readonly Faller Faller;

        /// <summary> 结果类型
        /// </summary>
        public readonly DustType Type;

        /// <summary> 结果值
        /// </summary>
        public readonly Object Value;

        /// <summary> 无论结果类型 强制转换为Sql语句,DustType.Undefined抛出异常
        /// </summary>
        public string ToSql()
        {
            switch (Type)
            {  
                case DustType.Sql:
                    return (string)Value;
                case DustType.Number:
                    return Faller.AddNumber((IConvertible)Value);
                case DustType.Array:
                    return Faller.GetSql(Value);
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

        /// <summary>  是否是非DustType.Sql和DustType.Undefined类型
        /// </summary>
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
