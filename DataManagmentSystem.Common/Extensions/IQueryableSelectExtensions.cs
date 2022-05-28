namespace DataManagmentSystem.Common.Extensions {
    using DataManagmentSystem.Common.Attributes;
    using DataManagmentSystem.Common.CoreEntities;
    using DataManagmentSystem.Common.SelectQuery;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public static class IQueryableSelectExtensions {

        public static IQueryable<TEntity> Select<TEntity, TQueryRecordsRestrictionAttribute>(this IQueryable<TEntity> query, SelectColumn columns, IColumnToSelectConverter converter, bool canSkipLocalization, bool isColumnReadingRestricted = true, bool ignoreDeletedRecords = true)
            where TEntity : BaseEntity, new()
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var expression = converter.Convert<TEntity, TQueryRecordsRestrictionAttribute>(columns, canSkipLocalization, isColumnReadingRestricted, ignoreDeletedRecords);
            if (expression != null) {
                if (expression.CanReduce)
                    expression = expression.Reduce() as Expression<Func<TEntity, TEntity>>;
                query = query.Select(expression);
            }
            return query;
        }
    }
}
