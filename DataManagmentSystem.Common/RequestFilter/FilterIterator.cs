using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.PropertyCache;
using DataManagmentSystem.Common.Request;
using DataManagmentSystem.Common.SelectQuery;

namespace DataManagmentSystem.Common.RequestFilter
{
    public class FilterIterator
    {
        public const char SEPARATOR = '.';
        private readonly IPropertyCache _propertyCache;

        public FilterIterator(IPropertyCache propertyCache)
        {
            _propertyCache = propertyCache;
        }

        public (Expression exp, PropertyInfo property, bool isFinish) Iterate(string columnPath, ParameterExpression parameter, Type propertyType, Func<string, Type, Expression, bool, RequestFilterExpression, Expression> enumerableHandler = null, bool useLocaliztion = false, RequestFilterExpression filter = null) {
            var columnPathChain = columnPath.Split(SEPARATOR);
            PropertyInfo property = null;
            var currentPropertyExpression = parameter as Expression;
            foreach (var propertyName in columnPathChain)
            {
                property = _propertyCache.GetPropertyByName(propertyType, propertyName);
                propertyType = property.PropertyType;
                if (property.IsDefined(typeof(MapToExpressionAttribute), true))
                {
                    var lambdaMethodName = property.GetCustomAttribute<MapToExpressionAttribute>()
                        ?.ExpressionMethodName;
                    if (property.ReflectedType.GetMethod(lambdaMethodName)
                        ?.Invoke(null, Array.Empty<object>()) is not LambdaExpression lambdaBindedToProperty)
                    {
                        throw new ArgumentException($"Wrong expression descriptor for property {property.Name}");
                    }
                    currentPropertyExpression = lambdaBindedToProperty.Body.ReplaceParameter(lambdaBindedToProperty.Parameters.Single(), currentPropertyExpression);
                }
                else
                {
                    currentPropertyExpression = Expression.Property(currentPropertyExpression, property);
                }
                if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.GenericTypeArguments.Any())
                {
                    return (enumerableHandler?.Invoke(propertyName, propertyType, currentPropertyExpression, useLocaliztion, filter), null, true);
                }
            }
            return (currentPropertyExpression, property, false);
        }
    }
}
