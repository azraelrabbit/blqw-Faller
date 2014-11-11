using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 表达式树解析结果
    /// </summary>
    public struct SawDust : ISawDust
    {
        /// <summary> 初始化解析结果
        /// </summary>
        /// <param name="faller">解析组件</param>
        /// <param name="type">结果类型</param>
        /// <param name="value">结果值</param>
        internal SawDust(Faller faller, DustType type, Object value)
            :this()
        {
            if (type == DustType.Object && value is SawDust)
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
            _toSqled = false;
            _sql = null;
        }

        /// <summary> 解析组件
        /// </summary>
        private readonly Faller Faller;

        /// <summary> 结果类型
        /// </summary>
        public DustType Type { get; private set; }

        /// <summary> 结果值
        /// </summary>
        public Object Value { get; private set; }

        /// <summary> 是否已经执行了toSql
        /// </summary>
        private bool _toSqled;

        /// <summary> tosql之后的sql语句
        /// </summary>
        private string _sql;

        /// <summary> 无论结果类型 强制转换为Sql语句,DustType.Undefined抛出异常
        /// </summary>
        public string ToSql()
        {
            if (_toSqled)
            {
                return _sql;
            }
            switch (Type)
            {
                case DustType.Sql:
                    _sql = (string)Value;
                    break;
                case DustType.Number:
                    _sql = Faller.AddNumber((IConvertible)Value);
                    break;
                case DustType.Array:
                    _sql = Faller.GetSql(Value);
                    break;
                case DustType.Boolean:
                    _sql = Faller.AddBoolean((bool)Value);
                    break;
                case DustType.Object:
                case DustType.DateTime:
                case DustType.Binary:
                case DustType.String:
                    _sql = Faller.AddObject(Value);
                    break;
                default:
                    throw new NotImplementedException();
            }
            _toSqled = true;
            return _sql;
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
                return object.Equals(Value, dust.Value) && object.ReferenceEquals(Faller, dust.Faller);
            }
            return false;
        }
    }
}
