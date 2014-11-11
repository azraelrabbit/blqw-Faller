using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> Like操作符枚举
    /// </summary>
    [Serializable]
    public enum LikeOperator
    {
        /// <summary> LIKE操作 a LIKE %b%
        /// </summary>    
        Contains,
        /// <summary> LIKE操作 a LIKE b%
        /// </summary>    
        StartWith,
        /// <summary> LIKE操作 a LIKE %b
        /// </summary>    
        EndWith,
        /// <summary> LIKE操作 a NOT LIKE %b%
        /// </summary>    
        NotContains,
        /// <summary> LIKE操作 a NOT LIKE b%
        /// </summary>    
        NotStartWith,
        /// <summary> LIKE操作 a NOT LIKE %b
        /// </summary>
        NotEndWith,
    }
}
