using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataManagmentSystem.Auth.Injector;
using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.CoreEntities;
using DataManagmentSystem.Common.Extensions;
using DataManagmentSystem.Common.Locale;
using DataManagmentSystem.Common.PropertyCache;
using DataManagmentSystem.Common.Rights;
using DataManagmentSystem.Common.SelectQuery.Factory;

namespace DataManagmentSystem.Common.SelectQuery.Strategy
{
    public class BaseColumnToExpressionStrategy : BaseConvertionStrategy
    {
        private const string LOCALIZATIONS_PROPERTY_NAME = "Localizations";
        private readonly IPropertyCache _propertyCache;
        private readonly bool _canSkipLocalization;

        public string CurrentLanguageCode { get; }

        public BaseColumnToExpressionStrategy(IPropertyCache propertyCache,
            IObjectLocalizer localizer,
            IUserDataAccessor userDataAccessor,
            bool canSkipLocalization) : base(userDataAccessor)
        {
            _propertyCache = propertyCache;
            _canSkipLocalization = canSkipLocalization;
            CurrentLanguageCode = localizer.GetCulture().TwoLetterISOLanguageName;
        }

        public override Dictionary<MemberInfo, Expression> Convert(IEnumerable<dynamic> columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords)
        {
            var expressions = new Dictionary<MemberInfo, Expression>();
            var localizationProperty = GetLocalizationProperty(type);
            foreach (var propertyName in columns.Cast<string>())
            {
                var property = _propertyCache.GetPropertyByName(type, propertyName);
                if (!(property?.IsCalculatedField() ?? true))
                {
                    var isLocalizedProperty = property.IsDefined(typeof(LocalizedAttribute), true) && localizationProperty != null;
                    var propertyExpression = isLocalizedProperty ? GetLocalizedPropertyExpression(property, parameter, localizationProperty) :
                        Expression.Property(parameter, propertyName);
                    propertyExpression = isColumnReadingRestricted
                        ? GetRestrictedColumnExpression(propertyExpression, parameter, type, property)
                        : propertyExpression;
                    expressions.Add(property, propertyExpression);
                }
            }
            return expressions;
        }

        private PropertyInfo GetLocalizationProperty(Type type)
        {
            PropertyInfo property;
            try
            {
                property = _propertyCache.GetPropertyByName(type, LOCALIZATIONS_PROPERTY_NAME);
            }
            catch
            {
                return null;
            }
            return property;
        }

        private Expression GetLocalizedPropertyExpression(PropertyInfo property, Expression parameter, PropertyInfo localizationProperty)
        {
            var valueExpression = Expression.Convert(Expression.Constant(CurrentLanguageCode), typeof(string));
            var localeType = localizationProperty.PropertyType.GenericTypeArguments.Single();
            var localeParameter = Expression.Parameter(localeType);
            var propertyExpression = Expression.Property(localeParameter, nameof(BaseLocalizationEntity.Language));
            propertyExpression = Expression.Property(propertyExpression, nameof(BaseLocalizationEntity.Language.Code));
            var predicate = Expression.MakeBinary(ExpressionType.Equal, propertyExpression, valueExpression);
            var predicateExpression = Expression.Lambda(predicate, localeParameter);
            var whereExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Where), new[] { localeType },
                Expression.Property(parameter, localizationProperty), predicateExpression);
            var selectExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Select), new[] { localeType, property.PropertyType },
                whereExpression, Expression.Lambda(Expression.Property(localeParameter, property.Name), localeParameter));
            var localizedValueExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.FirstOrDefault), new[] { property.PropertyType },
                selectExpression);
            if (_canSkipLocalization)
            {
                var nullExpression = Expression.Constant(null, property.PropertyType);
                var notNullCondition = Expression.MakeBinary(ExpressionType.NotEqual, localizedValueExpression, nullExpression);
                return Expression.Condition(notNullCondition, localizedValueExpression, Expression.Property(parameter, property.Name));
            }
            else
            {
                return localizedValueExpression;
            }
        }
    }
}
