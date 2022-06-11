namespace DataManagmentSystem.Common.Extensions {
    using DataManagmentSystem.Common.CoreEntities;
    using DataManagmentSystem.Common.SelectQuery;
    using System.Linq;
    using System.Linq.Expressions;

    public static class IQueryableAggregateExtensions {
        public static object Aggregate<TEntity>(this IQueryable<TEntity> query, AggregationItem aggregation)
            where TEntity : BaseEntity, new() {
            var converter = new BaseAggregateConverter<TEntity>();
            var targetType = converter.GetReturnedType(aggregation);
            var selectQueryMethod = converter.GetType()
                .GetMethod(nameof(BaseAggregateConverter<TEntity>.GetAggregatedByColumnQuery))
                ?.MakeGenericMethod(targetType);
            var newQuery = selectQueryMethod?.Invoke(converter, new object[] { query, aggregation });
            var aggregateMethod = converter.GetType()
                .GetMethod(nameof(BaseAggregateConverter<TEntity>.Aggregate))
                ?.MakeGenericMethod(targetType);
            var expression = aggregateMethod?.Invoke(converter, new object[] { newQuery, aggregation }) as Expression;
            return query.Provider.Execute(expression);
        }
    }
}
