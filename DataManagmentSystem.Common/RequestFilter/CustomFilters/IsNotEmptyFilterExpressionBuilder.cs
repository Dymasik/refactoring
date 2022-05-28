namespace DataManagmentSystem.Common.RequestFilter.CustomFilters
{
    using DataManagmentSystem.Common.Request;
    using System;
    using System.Linq.Expressions;

    public class IsNotEmptyFilterExpressionBuilder : BaseEmptinessFilterExpressionBuilderBase
    {
        public override string Type => FilterType.IsNotEmpty.ToString();

        protected override Func<Expression, Expression, BinaryExpression> GetComparisonFunc => Expression.NotEqual;
    }
}
