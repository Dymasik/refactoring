namespace DataManagmentSystem.Common.RequestFilter.CustomFilters {
    using DataManagmentSystem.Common.Request;
    using System;
    using System.Linq.Expressions;

    public class DefaultFilterExpressionBuilder : IFilterExpressionBuilder {
        public string Type => string.Empty;

        public Expression GetExpression(Expression currentExpression, FilterType comparisonType, object value) {
            var binaryType = GetExpressionType(comparisonType);
            var valueExpression = Expression.Convert(Expression.Constant(value), currentExpression.Type);
            return Expression.MakeBinary(binaryType, currentExpression, valueExpression);
        }

        private ExpressionType GetExpressionType(FilterType type) {
            return (ExpressionType) Enum.Parse(typeof(ExpressionType), type.ToString());
        }
    }
}
