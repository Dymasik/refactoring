namespace DataManagmentSystem.Common.Request {
    using DataManagmentSystem.Common.Attributes;
    using DataManagmentSystem.Common.CoreEntities;
    using DataManagmentSystem.Common.Extensions;
    using DataManagmentSystem.Common.Locale;
    using DataManagmentSystem.Common.Macros;
    using DataManagmentSystem.Common.PropertyCache;
    using DataManagmentSystem.Common.RequestFilter.CustomFilters;
    using DataManagmentSystem.Common.RequestFilter.Function;
    using DataManagmentSystem.Common.SelectQuery;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class BaseFilterToExpressionConverter : IFilterToExpressionConverter {

        private bool _canSkipLocalization;
        private readonly IEnumerable<IMacrosValueProvider> _macrosValueProviders;
        private readonly IEnumerable<IFunctionToExpressionConverter> _functionToExpressionConverters;
        private readonly IEnumerable<IFilterExpressionBuilder> _filterBuilders;
        public const char SEPARATOR = '.';
        public const string LOCALIZATIONS_PROP_NAME = "Localizations";

        protected IPropertyCache PropertyCache { get; }
        protected string CurrentLanguageCode { get; }

        public BaseFilterToExpressionConverter(IPropertyCache propertyCache,
            IObjectLocalizer localizer,
            IEnumerable<IMacrosValueProvider> macrosValueProviders,
            IEnumerable<IFunctionToExpressionConverter> functionToExpressionConverters,
            IEnumerable<IFilterExpressionBuilder> filterBuilders)
        {
            PropertyCache = propertyCache;
            CurrentLanguageCode = localizer.GetCulture().TwoLetterISOLanguageName;
            _macrosValueProviders = macrosValueProviders;
            _functionToExpressionConverters = functionToExpressionConverters;
            _filterBuilders = filterBuilders;
        }

        public Expression<Func<TEntity, bool>> Convert<TEntity>(RequestFilter filter, bool canSkipLocalization = false)
            where TEntity : BaseEntity
        {
            _canSkipLocalization = canSkipLocalization;
            if (typeof(TEntity).IsLocalizedEntity() && !_canSkipLocalization) {
                filter.ApplyLocalizations(CurrentLanguageCode);
            }
            var parameter = Expression.Parameter(typeof(TEntity));
            var expression = ConvertFilterToExpression(filter, parameter, typeof(TEntity)) ?? Expression.Constant(true);
            return Expression.Lambda<Func<TEntity, bool>>(expression, parameter);
        }

        private static ExpressionType GetLogicalOperator(FilterLogicalOperator @operator) {
            return @operator == FilterLogicalOperator.AND ? ExpressionType.AndAlso : ExpressionType.OrElse;
        }

        private Expression ConvertNodeExpressionToExpression(RequestFilterExpression filter, ParameterExpression parameter, Type type, bool useLocalization = false) {
            var currentPropertyExpression = parameter as Expression;
            var propertyType = type;
            Expression primitiveExpression = null;
            if (filter.Function != null) {
                var converter = GetFunctionToExpressionConverter(filter.Function.Operation);
                var functionCallExpression = converter.Convert(filter.Function.Parameters, parameter, type);
                propertyType = functionCallExpression.Method.ReturnType;
                currentPropertyExpression = functionCallExpression;
            } else {
                var columnPathChain = filter.ColumnPath.Split(SEPARATOR);
                PropertyInfo property = null;
                foreach (var propertyName in columnPathChain) {
                    property = PropertyCache.GetPropertyByName(propertyType, propertyName);
                    propertyType = property.PropertyType;
                    if (property.IsDefined(typeof(MapToExpressionAttribute), true)) {
                        var lambdaMethodName = property.GetCustomAttribute<MapToExpressionAttribute>()
                            ?.ExpressionMethodName;
                        if (property.ReflectedType.GetMethod(lambdaMethodName)
                            ?.Invoke(null, Array.Empty<object>()) is not LambdaExpression lambdaBindedToProperty)
                        {
                            throw new ArgumentException($"Wrong expression descriptor for property {property.Name}");
                        }
                        currentPropertyExpression = lambdaBindedToProperty.Body.ReplaceParameter(lambdaBindedToProperty.Parameters.Single(), currentPropertyExpression);
                    } else {
                        currentPropertyExpression = Expression.Property(currentPropertyExpression, property);
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.GenericTypeArguments.Any()) {
                        var enumType = propertyType.GenericTypeArguments.First();
                        var anyExpression = GetEnumerableAnyExpression(currentPropertyExpression, enumType, new RequestFilterExpression {
                            ColumnPath = filter.ColumnPath.Split(propertyName + SEPARATOR).Last(),
                            Value = filter.Value,
                            SubFilter = filter.SubFilter,
                            ComparisonType = filter.ComparisonType
                        }, useLocalization);
                        return anyExpression;
                    }
                }
                if ((property?.IsDefined(typeof(LocalizedAttribute), true) ?? false) && !_canSkipLocalization) {
                    return ConvertNodeExpressionToExpression(new RequestFilterExpression {
                        ColumnPath = $"{filter.ColumnPath.Split(property.Name).First()}{LOCALIZATIONS_PROP_NAME}.{property.Name}",
                        Value = filter.Value,
                        ComparisonType = filter.ComparisonType
                    }, parameter, type, true);
                }
                if (useLocalization) {
                    primitiveExpression = GetLanguageExpression(parameter);
                }
            }
            if (primitiveExpression != null) {
                primitiveExpression = Expression.MakeBinary(ExpressionType.AndAlso, primitiveExpression, GetPrimitiveExpression(currentPropertyExpression, filter, propertyType));
            } else {
                primitiveExpression = GetPrimitiveExpression(currentPropertyExpression, filter, propertyType);
            }
            return primitiveExpression;
        }

        private Expression GetPrimitiveExpression(Expression currentPropertyExpression, RequestFilterExpression filter, Type type) {
            var value = GetTypedValue(filter.Value, type);
            var filterBuilder = _filterBuilders.SingleOrDefault(builder => builder.Type == filter.ComparisonType.ToString());
            filterBuilder ??= _filterBuilders.Single(builder => builder.Type == string.Empty);
            return filterBuilder.GetExpression(currentPropertyExpression, filter.ComparisonType, value);
        }

        private object GetTypedValue(object value, Type type) {
            var macrosValueProvider = GetMacrosValueProvider(value?.ToString());
            var macrosValue = macrosValueProvider?.GetValue();
            if (macrosValue != null) {
                value = macrosValue;
            }
            if (value != null && value.GetType() is IList) {
                var typedList = new List<object>();
                foreach (var singleValue in (value as IEnumerable<object>)) {
                    typedList.Add(GetTypedSingleValue(singleValue, type));
                }
                return typedList;
            } else {
                return GetTypedSingleValue(value, type);
            }
        }

        private static object GetTypedSingleValue(object value, Type type) {
            object typedValue;
            if (type.IsEquivalentTo(typeof(Guid)) || type.IsEquivalentTo(typeof(Guid?))) {
                if (value != null && Guid.TryParse(value.ToString(), out Guid guidValue)) {
                    typedValue = guidValue;
                } else if (value == null) {
                    typedValue = null;
                } else {
                    throw new InvalidCastException($"Cannot convert {value} to Guid type");
                }
            } else {
                typedValue = System.Convert.ChangeType(value, type);
            }
            return typedValue;
        }

        private Expression GetEnumerableAnyExpression(Expression propertyExpression, Type type, RequestFilterExpression requestFilterExpression, bool useLocalization) {
            var newParameter = Expression.Parameter(type);
            Expression predicate;
            if (requestFilterExpression.IsExistingExpression) {
                predicate = ConvertFilterToExpression(requestFilterExpression.SubFilter, newParameter, type);
            } else {
                predicate = ConvertNodeExpressionToExpression(requestFilterExpression, newParameter, type, useLocalization);
            }
            var expression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { type },
                propertyExpression, Expression.Lambda(predicate, newParameter));
            if(requestFilterExpression.ComparisonType == FilterType.NotExists) {
                return Expression.Not(expression);
            }
            return expression;
        }

        private Expression GetLanguageExpression(ParameterExpression parameter) {
            var valueExpression = Expression.Convert(Expression.Constant(CurrentLanguageCode), typeof(string));
            var propertyExpression = Expression.Property(parameter, nameof(BaseLocalizationEntity.Language));
            propertyExpression = Expression.Property(propertyExpression, nameof(BaseLocalizationEntity.Language.Code));
            return Expression.MakeBinary(ExpressionType.Equal, propertyExpression, valueExpression);
        }

        private Expression ConvertFilterToExpression(RequestFilter filter, ParameterExpression parameter, Type type) {
            Expression expression = Expression.Constant(filter.LogicalOperator == FilterLogicalOperator.AND);
            if (filter?.IsEmpty ?? true) {
                return expression;
            }
            if (filter.IsFilterExpression) {
                expression = ConvertNodeExpressionToExpression(filter.Expression, parameter, type);
            } else {
                var logicalOperator = GetLogicalOperator(filter.LogicalOperator);
                foreach (var filterItem in filter.Filters) {
                    if (expression == null) {
                        expression = ConvertFilterToExpression(filterItem, parameter, type);
                    } else {
                        expression = Expression.MakeBinary(logicalOperator, expression, ConvertFilterToExpression(filterItem, parameter, type));
                    }
                }
            }
            return expression;
        }

        private IMacrosValueProvider GetMacrosValueProvider(string macros) {
            return _macrosValueProviders.SingleOrDefault(m => m.IsApplicableTo(macros));
        }

        private IFunctionToExpressionConverter GetFunctionToExpressionConverter(SupportedFunction function) {
            return _functionToExpressionConverters.Single(f => f.Function == function);
        }
    }
}
