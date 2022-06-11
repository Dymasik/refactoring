using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataManagmentSystem.Common.SelectQuery.Strategy
{
    public interface IColumnToExpressionStrategy
    {
        Dictionary<MemberInfo, Expression> Convert(IEnumerable<dynamic> columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords);
    }
}
