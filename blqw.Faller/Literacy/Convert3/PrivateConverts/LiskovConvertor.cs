using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    sealed class LiskovConvertor<TBase, TOutput> : AdvancedConvertor<TOutput>
        where TOutput : TBase
    {
        IConvertor<TBase> _conv;
        //Type _outputType;

        public LiskovConvertor()
        {
            _conv = Convert3.GetConvertor<TBase>();
            //_outputType = typeof(TOutput);
        }

        protected override bool Try(object input, Type outputType, out TOutput result)
        {
            if (_conv == null)
            {
                return CObject.TryTo<TOutput>(input, outputType, out result);
            }
            TBase r;
            if (_conv.Try(input, outputType, out r))
            {
                result = (TOutput)r;
                return true;
            }
            result = default(TOutput);
            return false;
        }

        protected override bool Try(string input, Type outputType, out TOutput result)
        {
            if (_conv == null)
            {
                return CObject.TryTo<TOutput>(input, outputType, out result);
            }
            TBase r;
            if (_conv.Try(input, outputType, out r))
            {
                result = (TOutput)r;
                return true;
            }
            result = default(TOutput);
            return false;
        }

    }
}
