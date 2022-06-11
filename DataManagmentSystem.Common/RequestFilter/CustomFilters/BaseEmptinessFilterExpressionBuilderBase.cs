namespace DataManagmentSystem.Common.RequestFilter.CustomFilters
{
    using DataManagmentSystem.Common.Request;
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public abstract class BaseEmptinessFilterExpressionBuilderBase : IFilterExpressionBuilder
    {
        public abstract string Type { get; }

        protected abstract Func<Expression, Expression, BinaryExpression> GetComparisonFunc { get; }

        public Expression GetExpression(Expression currentExpression, FilterType comparisonType, object value)
        {
            var valueExpression = Expression.Convert(Expression.Constant(GetDefault(currentExpression.Type)), currentExpression.Type);
            return GetComparisonFunc(currentExpression, valueExpression);
        }

        protected static object GetDefault(Type type)
        {
            if (type.Equals(typeof(string))) {
                return string.Empty;
            } else if (type.Equals(typeof(Guid))) {
                return Guid.Empty;
            } else if (type.GetTypeInfo().IsValueType) {
                return Activator.CreateInstance(type);
            } else {
                return null;
            }
        }
    }
}