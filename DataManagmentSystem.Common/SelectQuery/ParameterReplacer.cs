namespace DataManagmentSystem.Common.SelectQuery {
    using System.Linq;
    using System.Linq.Expressions;

    public static class ParameterReplacer { 
        public static Expression ReplaceParameter
                        (this Expression expression,
                        Expression source,
                        Expression target) {
            return new ParameterReplacerVisitor(source, target)
                        .Visit(expression);
        }

        private sealed class ParameterReplacerVisitor : ExpressionVisitor {
            private readonly Expression _source;
            private readonly Expression _target;

            public ParameterReplacerVisitor
                    (Expression source, Expression target) {
                _source = source;
                _target = target;
            }

            public override Expression Visit(Expression node) {
                return node == _source ? _target : base.Visit(node);
            }
        }
    }
}
