using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.Extensions;
using DataManagmentSystem.Common.PropertyCache;
using DataManagmentSystem.Common.Request;
using DataManagmentSystem.Common.SelectQuery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DataManagmentSystem.Common.RequestFilter.Function
{
    public class GetDistanceFunctionToExpressionConverter : IFunctionToExpressionConverter {
        private readonly FilterIterator _iterator;

        protected IPropertyCache PropertyCache { get; }

        public SupportedFunction Function => SupportedFunction.GET_DISTANCE;

        public GetDistanceFunctionToExpressionConverter(IPropertyCache propertyCache, FilterIterator iterator) {
            PropertyCache = propertyCache;
            _iterator = iterator;
        }

        public MethodCallExpression Convert(IEnumerable<FunctionParameter> parameters, ParameterExpression parameter, Type type) {
            var functionCall = typeof(DbFunction).GetMethod(nameof(DbFunction.GetDistance),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal) },
                        null
                    );
            return Expression.Call(functionCall, parameters.Select(p => ConvertParameterToExpression(p, parameter, type)));
        }

        private Expression ConvertParameterToExpression(FunctionParameter parameter, ParameterExpression parameterExpression, Type type) {
            if (parameter.IsPredefined) {
                return Expression.Constant(System.Convert.ToDecimal(parameter.Value), typeof(decimal));
            }
            (Expression currentPropertyExpression, _, _) = _iterator.Iterate(parameter.ColumnPath, parameterExpression, type, (_, _, _, _, _) => throw new InvalidOperationException());
            return Expression.Convert(currentPropertyExpression, typeof(decimal));
        }
    }
}
