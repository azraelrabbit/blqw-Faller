using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 可变值,可自动转换类型
    /// </summary>
    public struct Changeable : IConvertible
    {
        /// <summary> 数据库值
        /// </summary>
        public object Value { get; private set; }
        /// <summary> 是否是经由自定义构造函数创建
        /// </summary>
        private bool _init;

        /// <summary> 初始化 Changeable
        /// </summary>
        /// <param name="value">值</param>
        public Changeable(object value)
            :this()
        {
            _init = true;
            if (value is DBNull || value == null)
            {
                Value = null;
                IsDBNull = true;
            }
            else
            {
                Value = value;
                IsDBNull = false;
            }
        }

        /// <summary> 当前值是否为null
        /// </summary>
        public bool IsDBNull { get; private set; }

        /// <summary> 当前值是否是未定义
        /// </summary>
        public bool IsUndefined
        {
            get
            {
                return _init == false;
            }
        }

        public static bool operator ==(Changeable t1, Changeable t2)
        {
            return object.Equals(t1.Value, t2.Value); 
        }

        public static bool operator !=(Changeable t1, Changeable t2)
        {
            return object.Equals(t1.Value, t2.Value) == false; 
        }

        #region 强转

        public static implicit operator char(Changeable value)
        {
            return Convert3.To<Char>(value.Value);
        }
        public static implicit operator int(Changeable value)
        {
            return Convert3.To<Int32>(value.Value);
        }
        public static implicit operator long(Changeable value)
        {
            return Convert3.To<Int64>(value.Value);
        }
        public static implicit operator bool(Changeable value)
        {
            return Convert3.To<Boolean>(value.Value);
        }
        public static implicit operator string(Changeable value)
        {
            return Convert3.To<String>(value.Value);
        }
        public static implicit operator DateTime(Changeable value)
        {
            return Convert3.To<DateTime>(value.Value);
        }
        public static implicit operator decimal(Changeable value)
        {
            return Convert3.To<Decimal>(value.Value);
        }
        public static implicit operator float(Changeable value)
        {
            return Convert3.To<Single>(value.Value);
        }
        public static implicit operator double(Changeable value)
        {
            return Convert3.To<Double>(value.Value);
        }
        public static implicit operator byte(Changeable value)
        {
            return Convert3.To<Byte>(value.Value);
        }
        public static implicit operator ushort(Changeable value)
        {
            return Convert3.To<UInt16>(value.Value);
        }
        public static implicit operator uint(Changeable value)
        {
            return Convert3.To<UInt32>(value.Value);
        }
        public static implicit operator Guid(Changeable value)
        {
            return Convert3.To<Guid>(value.Value);
        }

        #endregion

        #region ToType
        /// <summary> 将值转为 System.Boolean 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public bool ToBoolean(bool defaultValue)
        {

            return Convert3.To<Boolean>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Byte 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public byte ToByte(byte defaultValue)
        {
            return Convert3.To<Byte>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Char 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public char ToChar(char defaultValue)
        {
            return Convert3.To<Char>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.DateTime 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public DateTime ToDateTime(DateTime defaultValue)
        {
            return Convert3.To<DateTime>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Decimal 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public decimal ToDecimal(decimal defaultValue)
        {
            return Convert3.To<Decimal>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Double 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public double ToDouble(double defaultValue)
        {
            return Convert3.To<Double>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Int16 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public short ToInt16(short defaultValue)
        {
            return Convert3.To<Int16>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Int32 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public int ToInt32(int defaultValue)
        {
            return Convert3.To<Int32>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Int64 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public long ToInt64(long defaultValue)
        {
            return Convert3.To<Int64>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.SByte 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public sbyte ToSByte(sbyte defaultValue)
        {
            return Convert3.To<SByte>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Single 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public float ToSingle(float defaultValue)
        {
            return Convert3.To<Single>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.String 类型,如果值为null,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public string ToString(string defaultValue)
        {
            return Convert3.To<String>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.UInt16 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public ushort ToUInt16(ushort defaultValue)
        {
            return Convert3.To<UInt16>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.UInt32 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public uint ToUInt32(uint defaultValue)
        {
            return Convert3.To<UInt32>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.UInt64 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public ulong ToUInt64(ulong defaultValue)
        {
            return Convert3.To<UInt64>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Guid 类型,如果失败,返回defaultValue
        /// </summary>
        /// <param name="defaultValue">转换失败时返回的值</param>
        public Guid ToGuid(Guid defaultValue)
        {
            return Convert3.To<Guid>(Value, defaultValue);
        }

        /// <summary> 将值转为 System.Byte 数组,如果失败,返回null
        /// </summary>
        public byte[] ToByteArray()
        {
            return Convert3.To<Byte[]>(Value, null);
        }

        /// <summary> 将值转为 System.Boolean 数组,如果失败,返回null
        /// </summary>
        public bool[] ToBooleanArray()
        {
            var bits = ToBitArray();
            if (bits != null)
            {
                var arr = new bool[bits.Length];
                bits.CopyTo(arr, 0);
                return arr;
            }
            return null;
        }

        /// <summary> 将值转为 System.Collections.BitArray 类型,如果失败,返回null
        /// </summary>
        public BitArray ToBitArray()
        {
            return new BitArray(Convert3.To<Byte[]>(Value, null));
        }


        public override string ToString()
        {
            if (Value == null)
            {
                return "";
            }
            return Value.To<String>();
        }
        #endregion

        #region IConvertible
        TypeCode IConvertible.GetTypeCode()
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return TypeCode.Object;
            }
            else
            {
                return conv.GetTypeCode();
            }
        }
        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (bool)this;
            }
            else
            {
                return conv.ToBoolean(provider);
            }
        }
        byte IConvertible.ToByte(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (byte)this;
            }
            else
            {
                return conv.ToByte(provider);
            }
        }
        char IConvertible.ToChar(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (char)this;
            }
            else
            {
                return conv.ToChar(provider);
            }
        }
        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (DateTime)this;
            }
            else
            {
                return conv.ToDateTime(provider);
            }
        }
        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (decimal)this;
            }
            else
            {
                return conv.ToDecimal(provider);
            }
        }
        double IConvertible.ToDouble(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (double)this;
            }
            else
            {
                return conv.ToDouble(provider);
            }
        }
        short IConvertible.ToInt16(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (short)this;
            }
            else
            {
                return conv.ToInt16(provider);
            }
        }
        int IConvertible.ToInt32(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (int)this;
            }
            else
            {
                return conv.ToInt32(provider);
            }
        }
        long IConvertible.ToInt64(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (long)this;
            }
            else
            {
                return conv.ToInt64(provider);
            }
        }
        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (sbyte)this;
            }
            else
            {
                return conv.ToSByte(provider);
            }
        }
        float IConvertible.ToSingle(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (float)this;
            }
            else
            {
                return conv.ToSingle(provider);
            }
        }
        string IConvertible.ToString(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (string)this;
            }
            else
            {
                return conv.ToString(provider);
            }
        }
        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return Convert.ChangeType(this.Value, conversionType, provider);
            }
            else
            {
                return conv.ToType(conversionType, provider);
            }
        }
        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (ushort)this;
            }
            else
            {
                return conv.ToUInt16(provider);
            }
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (uint)this;
            }
            else
            {
                return conv.ToUInt32(provider);
            }
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            var conv = Value as IConvertible;
            if (conv == null)
            {
                return (ulong)(long)this;
            }
            else
            {
                return conv.ToUInt64(provider);
            }
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is Changeable)
            {
                return this == (Changeable)obj;
            }
            return this.Value == null ? obj == null : obj.Equals(this.Value);
        }

        public override int GetHashCode()
        {
            return this.Value == null ? 0 : this.Value.GetHashCode();
        }
    }
}
