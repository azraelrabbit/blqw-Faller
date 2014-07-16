using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace blqw
{
    public interface IFaller
    {
        string ToWhere(ISaw provider);
        string ToOrderBy(ISaw provider, bool asc);
        string ToSet(ISaw provider);
        ICollection<DbParameter> Parameters { get; }
    }
}
