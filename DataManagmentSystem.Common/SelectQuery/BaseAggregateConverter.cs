namespace DataManagmentSystem.Common.SelectQuery {
    using DataManagmentSystem.Common.CoreEntities;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public class BaseAggregateConverter<TEntity> : IAggregateConverter<TEntity>
        where TEntity : BaseEntity {

        public const char SEPARATOR = '.';

        private Expression GetPropertyExpression(string columnName, Expression parameter) {
            Expression property = parameter;
            foreach (var column in columnName.Split(SEPARATOR)) {
                property = Expression.Property(parameter, column);
            }
            return property;
        }

        public Expression Aggregate<T>(IQueryable<T> query, AggregationItem item) {
            var targetMethod = typeof(Queryable).GetMethods().FirstOrDefault(
                    m => m.Name == item.Type.ToString()
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsInstanceOfType(query));
            if (targetMethod == null) {
                targetMethod = typeof(Queryable).GetMethods().First(
                    m => m.Name == item.Type.ToString()
                        && m.IsGenericMethod)
                    .MakeGenericMethod(query.ElementType);
            }
            var aggregate = Expression.Call(null, targetMethod, query.Expression);
            return aggregate;
        }

        public IQueryable<T> GetAggregatedByColumnQuery<T>(IQueryable<TEntity> query, AggregationItem item) {
            if (!string.IsNullOrEmpty(item.ColumnName)) {
                var parameter = Expression.Parameter(typeof(TEntity));
                var property = GetPropertyExpression(item.ColumnName, parameter);
                return query.Select(Expression.Lambda<Func<TEntity, T>>(property, parameter));
            }
            return query.Cast<T>();
        }

        public Type GetReturnedType(AggregationItem item) {
            return string.IsNullOrEmpty(item.ColumnName) ? typeof(TEntity) : GetPropertyExpression(item.ColumnName, Expression.Parameter(typeof(TEntity))).Type;
        }
    }
}
