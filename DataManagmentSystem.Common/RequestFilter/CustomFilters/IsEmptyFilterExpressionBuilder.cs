namespace DataManagmentSystem.Common.RequestFilter.CustomFilters
{
    using DataManagmentSystem.Common.Request;
    using System;
    using System.Linq.Expressions;

    public class IsEmptyFilterExpressionBuilder : BaseEmptinessFilterExpressionBuilderBase
    {
        public override string Type => FilterType.IsEmpty.ToString();

        protected override Func<Expression, Expression, BinaryExpression> GetComparisonFunc => Expression.Equal;
    }
}
