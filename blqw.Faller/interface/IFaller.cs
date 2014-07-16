using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace blqw
{
    public interface IFaller
    {
        string ToWhere(ISaw saw);
        string ToOrderBy(ISaw saw, bool asc);
        string ToSet(ISaw saw);
        ICollection<DbParameter> Parameters { get; }
    }
}
