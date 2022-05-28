namespace DataManagmentSystem.Common.SelectQuery {
    using DataManagmentSystem.Common.CoreEntities;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public interface IAggregateConverter<TEntity>
        where TEntity : BaseEntity
    {
        Expression Aggregate<T>(IQueryable<T> query, AggregationItem item);
        IQueryable<T> GetAggregatedByColumnQuery<T>(IQueryable<TEntity> query, AggregationItem item);
        Type GetReturnedType(AggregationItem item);
    }
}
