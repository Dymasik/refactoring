namespace DataManagmentSystem.Common.RequestFilter.CustomFilters {
    using DataManagmentSystem.Common.Request;
    using System.Linq.Expressions;

    public interface IFilterExpressionBuilder {
        string Type { get; }

        Expression GetExpression(Expression currentExpression, FilterType comparisonType, object value);
    }
}
