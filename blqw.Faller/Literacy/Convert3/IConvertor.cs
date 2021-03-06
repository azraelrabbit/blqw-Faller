﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{

    /// <summary> 转换器
    /// </summary>
    public interface IConvertor
    {
        /// <summary> 转换器优先级,只启用优先级最高的转换器
        /// </summary>
        /// <remarks>优先级相同不确定使用哪个</remarks>
        uint Priority { get; }
        /// <summary> 转换器的输出类型
        /// (有可能是泛型定义类型)
        /// </summary>
        Type OutputType { get; }
        /// <summary> 尝试转换,返回转换是否成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为null</param>
        bool Try(object input, Type outputType, out object result);
        /// <summary> 尝试转换,返回转换是否成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为null</param>
        bool Try(string input, Type outputType, out object result);
    }

    /// <summary> 泛型转换器
    /// </summary>
    /// <typeparam name="T">输出类型泛型</typeparam>
    public interface IConvertor<T> : IConvertor
    {
        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        bool Try(object input, Type outputType, out T result);

        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        bool Try(string input, Type outputType, out T result);
    }

}
