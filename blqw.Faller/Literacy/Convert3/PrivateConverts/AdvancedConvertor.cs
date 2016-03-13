using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    /// <summary> 高级转换器,提供基本类型的转换器基类
    /// <para>高级类型的定义: T 接口,抽象类,或可以被继承的类型,outputType 不一定完全等于 typeof(T)</para>
    /// </summary>
    /// <typeparam name="T">高级类型泛型</typeparam>
    public abstract class AdvancedConvertor<T> : IConvertor<T>
    {
        private readonly static Type _type = typeof(T);

        /// <summary> 转换器优先级,默认0
        /// </summary>
        public virtual uint Priority { get { return 0; } }

        /// <summary> 转换器的输出类型
        /// (有可能是泛型定义类型)
        /// </summary>
        public Type OutputType { get { return _type; } }

        /// <summary> 尝试转换,返回转换是否成功
        /// </summary>
        /// <param name="input">输入对象</param>
        /// <param name="outputType">输出具体类型</param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        protected abstract bool Try(object input, Type outputType, out T result);

        /// <summary> 尝试转换,返回转换是否成功
        /// </summary>
        /// <param name="input">输入对象</param>
        /// <param name="outputType">输出具体类型</param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        protected abstract bool Try(string input, Type outputType, out T result);

        #region 实现接口

        bool IConvertor<T>.Try(object input, Type outputType, out T result)
        {
            if (input is T && outputType.IsInstanceOfType(input))
            {
                result = (T)input;
                return true;
            }
            var str = input as string;
            if (str != null)
            {
                if (Try(str, outputType, out result))
                {
                    return true;
                }
            }
            else
            {
                if (Try(input, outputType, out result))
                {
                    return true;
                }

                if (Convert3.TryTo<string>(input, out str) && Try(str, outputType, out result))
                {
                    return true;
                }
            }
            result = default(T);
            return false;
        }

        bool IConvertor<T>.Try(string input, Type outputType, out T result)
        {
            return Try(input, outputType, out result);
        }

        bool IConvertor.Try(object input, Type outputType, out object result)
        {
            T resultT;
            if (input is T && outputType.IsInstanceOfType(input))
            {
                result = input;
                return true;
            }
            var str = input as string;
            if (str != null)
            {
                if (Try(str, outputType, out resultT))
                {
                    result = resultT;
                    return true;
                }
            }
            else
            {
                if (Try(input, outputType, out resultT))
                {
                    result = resultT;
                    return true;
                }
            }
            if (Convert3.TryTo<string>(input, out str) && Try(str, outputType, out resultT))
            {
                result = resultT;
                return true;
            }
            result = null;
            return false;
        }

        bool IConvertor.Try(string input, Type outputType, out object result)
        {
            T resultT;
            if (Try(input, outputType, out resultT))
            {
                result = resultT;
                return true;
            }
            result = null;
            return false;
        }
        #endregion


    }
}
