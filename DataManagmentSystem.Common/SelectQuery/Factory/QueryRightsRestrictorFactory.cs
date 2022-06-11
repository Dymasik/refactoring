using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataManagmentSystem.Auth.Injector;
using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.Request;
using DataManagmentSystem.Common.Rights;

namespace DataManagmentSystem.Common.SelectQuery.Factory
{
    public static class QueryRightsRestrictorFactory
    {
        public static dynamic BuildRecords<TQueryRecordsRestrictionAttribute>(Type entityType, IUserDataAccessor userDataAccessor, IFilterToExpressionConverter filterConverter)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var restrictorType = typeof(QueryRecordsRestrictor<,>).MakeGenericType(entityType, typeof(TQueryRecordsRestrictionAttribute));
            return Activator.CreateInstance(restrictorType, userDataAccessor, filterConverter);
        }

        public static dynamic BuildColumn<TColumnOperationRestrictionAttribute>(Type entityType, IUserDataAccessor userDataAccessor, string columnName)
            where TColumnOperationRestrictionAttribute : BaseColumnOperationRestrictionAttribute
        {
            var restrictorType = typeof(ColumnOperationRestrictor<,>).MakeGenericType(entityType, typeof(TColumnOperationRestrictionAttribute));
            return Activator.CreateInstance(restrictorType, userDataAccessor, columnName);
        }
    }
}
