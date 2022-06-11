using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.CoreEntities;
using DataManagmentSystem.Common.SelectQuery;

namespace DataManagmentSystem.Common.Extensions
{
    public static class IQueryableOrderExtensions
    {
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> query, IEnumerable<OrderOption> orderColumns)
            where TEntity : BaseEntity, new()
        {
            var entityType = typeof(TEntity);
            var isFirstItem = true;
            foreach (var orderItem in orderColumns) {
                var propertyInfo = entityType.GetProperty(orderItem.ColumnName);
                var orderMethod = GetOrderMethod(orderItem, entityType, propertyInfo?.PropertyType, isFirstItem);
                LambdaExpression orderSelector;
                if (propertyInfo?.IsDefined(typeof(MapToExpressionAttribute), true) ?? false) {
                    var lambdaMethodName = propertyInfo.GetCustomAttribute<MapToExpressionAttribute>()
                        ?.ExpressionMethodName;
                    orderSelector = propertyInfo.ReflectedType.GetMethod(lambdaMethodName)
                        ?.Invoke(null, new object[0]) as LambdaExpression;
                    if (orderSelector == null) {
                        throw new ArgumentException($"Wrong expression descriptor for property {propertyInfo.Name}");
                    }
                } else {
                    orderSelector = GetOrderSelector(orderItem, entityType);
                }
                isFirstItem = false;
                query = (IQueryable<TEntity>) orderMethod.Invoke(orderMethod, new object[] { query, orderSelector });
            }
            return query;
        }

        private static LambdaExpression GetOrderSelector(OrderOption orderItem, Type entityType)
        {
            var arg = Expression.Parameter(entityType);
            var property = Expression.Property(arg, orderItem.ColumnName);
            return Expression.Lambda(property, new ParameterExpression[] { arg });
        }

        private static MethodInfo GetOrderMethod(OrderOption orderItem, Type entityType, Type propertyType, bool isFirstOrder = false)
        {
            var enumarableType = typeof(System.Linq.Queryable);
            var methodName = GetOrderMethodName(orderItem.Direction, isFirstOrder);
            var method = enumarableType.GetMethods()
                 .Single(m => {
                     if (m.Name == methodName && m.IsGenericMethodDefinition) {
                         var parameters = m.GetParameters().ToList();
                         return parameters.Count == 2;
                     } else {
                         return false;
                     }
                 });
            return method.MakeGenericMethod(entityType, propertyType);
        }

        private static string GetOrderMethodName(OrderDirection direction, bool isFirstOrder)
        {
            switch (direction)
            {
                case OrderDirection.ASC:
                    return isFirstOrder ? "OrderBy" : "ThenBy";
                default:
                    return isFirstOrder ? "OrderByDescending" : "ThenByDescending";
            }
        }
    }
}