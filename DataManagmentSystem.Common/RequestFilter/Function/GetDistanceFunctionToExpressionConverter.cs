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

        protected IPropertyCache PropertyCache { get; }

        public SupportedFunction Function => SupportedFunction.GET_DISTANCE;

        public GetDistanceFunctionToExpressionConverter(IPropertyCache propertyCache) {
            PropertyCache = propertyCache;
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
            var columnPathChain = parameter.ColumnPath.Split(BaseFilterToExpressionConverter.SEPARATOR);
            var currentPropertyExpression = parameterExpression as Expression;
            PropertyInfo property = null;
            var propertyType = type;
            foreach (var propertyName in columnPathChain) {
                property = PropertyCache.GetPropertyByName(propertyType, propertyName);
                propertyType = property.PropertyType;
                if (property.IsDefined(typeof(MapToExpressionAttribute), true)) {
                    var lambdaMethodName = property.GetCustomAttribute<MapToExpressionAttribute>()
                        ?.ExpressionMethodName;
                    var lambdaBindedToProperty = property.ReflectedType.GetMethod(lambdaMethodName)
                        ?.Invoke(null, new object[0]) as LambdaExpression;
                    if (lambdaBindedToProperty == null) {
                        throw new ArgumentException($"Wrong expression descriptor for property {property.Name}");
                    }
                    currentPropertyExpression = lambdaBindedToProperty.Body.ReplaceParameter(lambdaBindedToProperty.Parameters.Single(), currentPropertyExpression);
                } else {
                    currentPropertyExpression = Expression.Property(currentPropertyExpression, property);
                }
                if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.GenericTypeArguments.Any()) {
                    throw new InvalidOperationException("Impossible to use collection inside function call. Try to use exists filter.");
                }
            }
            return Expression.Convert(currentPropertyExpression, typeof(decimal));
        }
    }
}
