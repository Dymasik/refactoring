namespace DataManagmentSystem.Common.SelectQuery
{
    using DataManagmentSystem.Auth.Injector;
    using DataManagmentSystem.Common.Attributes;
    using DataManagmentSystem.Common.CoreEntities;
    using DataManagmentSystem.Common.Extensions;
    using DataManagmentSystem.Common.Locale;
    using DataManagmentSystem.Common.PropertyCache;
    using DataManagmentSystem.Common.Request;
    using DataManagmentSystem.Common.Rights;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class BaseColumnToSelectConverter : IColumnToSelectConverter
    {

        private const string LOCALIZATIONS_PROPERTY_NAME = "Localizations";
        private const string INCLUDED_COLUMN_SEPARATOR = ".";
        private bool _canSkipLocalization;
        private readonly IFilterToExpressionConverter _filterConverter;
        private readonly IUserDataAccessor _userDataAccessor;

        protected IPropertyCache PropertyCache { get; }
        protected string CurrentLanguageCode { get; }

        public BaseColumnToSelectConverter(IPropertyCache propertyCache, IObjectLocalizer localizer,
            IFilterToExpressionConverter filterConverter,
            IUserDataAccessor userDataAccessor)
        {
            PropertyCache = propertyCache;
            _filterConverter = filterConverter;
            _userDataAccessor = userDataAccessor;
            CurrentLanguageCode = localizer.GetCulture().TwoLetterISOLanguageName;
        }

        public Expression<Func<TEntity, TEntity>> Convert<TEntity, TQueryRecordsRestrictionAttribute>(SelectColumn columns, bool canSkipLocalization = false, bool isColumnReadingRestricted = true, bool ignoreDeletedRecords = true)
            where TEntity : BaseEntity, new()
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            _canSkipLocalization = canSkipLocalization;
            var parameter = Expression.Parameter(typeof(TEntity));
            var init = GetMemeberInit<TQueryRecordsRestrictionAttribute>(columns, parameter, typeof(TEntity), isColumnReadingRestricted, ignoreDeletedRecords);
            return Expression.Lambda<Func<TEntity, TEntity>>(init, parameter);
        }

        private Expression GetMemeberInit<TQueryRecordsRestrictionAttribute>(SelectColumn columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            List<string> columnNames = new List<string>();
            columnNames.AddRange(columns.ColumnNames);
            columnNames.AddRange(columns.RelatedColumns.Select(relatedColumn => relatedColumn.ColumnName));
            var calculatedIncludeColumns = GetCalculatedIncludedColumns(columnNames, type);
            AddIncludedColumns(columns, calculatedIncludeColumns);
            if (isColumnReadingRestricted)
            {
                var restrictionIncludeColumns = GetColumnRestrictionsIncludedColumns<AllowReadingColumnAttribute>(columns.ColumnNames, type);
                AddIncludedColumns(columns, restrictionIncludeColumns);
            }
            var bindings = new List<MemberBinding>();
            var columnsExpression = GetColumnsExpression(columns.ColumnNames, parameter, type, isColumnReadingRestricted);
            foreach (var columnExpression in columnsExpression)
            {
                bindings.Add(Expression.Bind(columnExpression.Key, columnExpression.Value));
            }
            var relatedColumnsExpression = GetRelatedColumnsExpression<TQueryRecordsRestrictionAttribute>(columns.RelatedColumns, parameter, type, isColumnReadingRestricted, ignoreDeletedRecords);
            foreach (var columnExpression in relatedColumnsExpression)
            {
                bindings.Add(Expression.Bind(columnExpression.Key, columnExpression.Value));
            }
            return Expression.MemberInit(Expression.New(type), bindings);
        }

        private IEnumerable<string> GetCalculatedIncludedColumns(IEnumerable<string> columns, Type type)
        {
            IEnumerable<string> includedColumns = null;
            foreach (var column in columns)
            {
                var property = PropertyCache.GetPropertyByName(type, column);
                if (property.IsCalculatedField())
                {
                    var propertyIncludedColumns = property.GetCustomAttribute<IncludeColumnsAttribute>()
                        ?.Columns;
                    if (includedColumns != null && propertyIncludedColumns != null) {
                        includedColumns = includedColumns.Concat(propertyIncludedColumns);
                    } else {
                        includedColumns ??= propertyIncludedColumns;
                    }
                }
            }
            return includedColumns;
        }

        private IEnumerable<string> GetColumnRestrictionsIncludedColumns<TColumnOperationRestrictionAttribute>(IEnumerable<string> columns, Type type)
            where TColumnOperationRestrictionAttribute : BaseColumnOperationRestrictionAttribute
        {
            IEnumerable<string> includedColumns = null;
            foreach (var column in columns) {
                var rightsRestrictor = BuildColumnOperationRightsRestrictor<TColumnOperationRestrictionAttribute>(type, column);
                if (rightsRestrictor == null || !rightsRestrictor.IsRestricted()) {
                    continue;
                }
                var restrictionMethod = rightsRestrictor.GetRestrictionMethod() as MethodInfo;
                var restrictionIncludedColumns = restrictionMethod
                    ?.GetCustomAttribute<IncludeColumnsAttribute>(true)
                    ?.Columns;
                if (includedColumns != null && restrictionIncludedColumns != null) {
                    includedColumns = includedColumns.Concat(restrictionIncludedColumns);
                } else {
                    includedColumns ??= restrictionIncludedColumns;
                }
            }
            return includedColumns;
        }

        private static void AddIncludedColumns(SelectColumn columns, IEnumerable<string> includedColumns)
        {
            if (includedColumns != null && includedColumns.Any())
            {
                foreach (var includedColumn in includedColumns)
                {
                    var splitedColumns = includedColumn.Split(INCLUDED_COLUMN_SEPARATOR);
                    columns.Merge(splitedColumns);
                }
            }
        }

        private Dictionary<MemberInfo, Expression> GetColumnsExpression(IEnumerable<string> columns, Expression parameter, Type type, bool isColumnReadingRestricted)
        {
            var expressions = new Dictionary<MemberInfo, Expression>();
            var localizationProperty = GetLocalizationProperty(type);
            foreach (var propertyName in columns)
            {
                var property = PropertyCache.GetPropertyByName(type, propertyName);
                if (CanConvertProperty(property))
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

        private Dictionary<MemberInfo, Expression> GetRelatedColumnsExpression<TQueryRecordsRestrictionAttribute>(IEnumerable<SelectRelatedColumn> relatedColumns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var expressions = new Dictionary<MemberInfo, Expression>();
            foreach (var relatedColumn in relatedColumns)
            {
                var property = PropertyCache.GetPropertyByName(type, relatedColumn.ColumnName);
                if (CanConvertProperty(property))
                {
                    var isEnumerationProperty = typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                        property.PropertyType.GenericTypeArguments.Any();
                    var memberType = isEnumerationProperty ? property.PropertyType.GenericTypeArguments.Single()
                        : property.PropertyType;
                    var currentPropertyParameter = Expression.Property(parameter, property.Name);
                    var typeParameter = Expression.Parameter(memberType);
                    Expression memberInitParameter;
                    if (isEnumerationProperty){
                        memberInitParameter = typeParameter;
                    } else {
                        memberInitParameter = currentPropertyParameter;
                    }
                    var memberInit = GetMemeberInit<TQueryRecordsRestrictionAttribute>(relatedColumn,
                        memberInitParameter, memberType, isColumnReadingRestricted, ignoreDeletedRecords);
                    var restrictionExpression = GetRestrictionExpression<TQueryRecordsRestrictionAttribute>(isEnumerationProperty ? typeParameter.Type : currentPropertyParameter.Type);
                    var assigningExpression = isEnumerationProperty ? GetEnumerableAssigningExpression(typeParameter, memberInit, currentPropertyParameter, restrictionExpression, ignoreDeletedRecords)
                        : GetReferenceEntityAssigningExpression(currentPropertyParameter, memberInit, restrictionExpression, ignoreDeletedRecords);
                    assigningExpression = isColumnReadingRestricted
                        ? GetRestrictedColumnExpression(assigningExpression, parameter, type, property)
                        : assigningExpression;
                    expressions.Add(property, assigningExpression);
                }
            }
            return expressions;
        }

        private PropertyInfo GetLocalizationProperty(Type type)
        {
            PropertyInfo property;
            try {
                property = PropertyCache.GetPropertyByName(type, LOCALIZATIONS_PROPERTY_NAME);
            } catch {
                return null;
            }
            return property;
        }

        private static bool CanConvertProperty(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }
            return !property.IsCalculatedField();
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

        private static Expression GetReferenceEntityAssigningExpression(Expression memberParameter, Expression memberInit, LambdaExpression restrictionExpression, bool ignoreDeletedRecords)
        {
            var nullExpression = Expression.Constant(null, memberParameter.Type);
            var condition = Expression.MakeBinary(ExpressionType.NotEqual, memberParameter, nullExpression);
            if (restrictionExpression != null)
            {
                var predicate = restrictionExpression.Body.ReplaceParameter(restrictionExpression.Parameters.Single(), memberParameter);
                condition = Expression.AndAlso(condition, predicate);
            }
			if(ignoreDeletedRecords){
				condition = Expression.AndAlso(condition, GetNotDeletedRecordsExpression(memberParameter));
			}
            return Expression.Condition(condition, memberInit, nullExpression);
        }

        private static Expression GetNotDeletedRecordsExpression(Expression memberParameter)
        {
            Expression<Func<BaseEntity, bool>> exp = entity => !entity.IsDeleted;
            return exp.Body.ReplaceParameter(exp.Parameters.Single(), memberParameter);
        }

        private Expression GetEnumerableAssigningExpression(ParameterExpression memberParameter, Expression memberInit, Expression parentParameter, LambdaExpression restrictionExpression, bool ignoreDeletedRecords)
        {
            var type = memberParameter.Type;
            var predicate = restrictionExpression != null ?
                Expression.Lambda(restrictionExpression.Body.ReplaceParameter(restrictionExpression.Parameters.Single(), memberParameter), memberParameter)
                : null;
            var whereExpression = ignoreDeletedRecords ? Expression.Call(typeof(Enumerable), nameof(Enumerable.Where), new[] { type }, 
				parentParameter, Expression.Lambda(GetNotDeletedRecordsExpression(memberParameter), memberParameter)) : null;
            whereExpression = predicate != null ? Expression.Call(typeof(Enumerable), nameof(Enumerable.Where), new[] { type },
                whereExpression, predicate) : whereExpression;
            var selectExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Select), new[] { type, type },
                whereExpression ?? parentParameter, Expression.Lambda(memberInit, memberParameter));
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), new[] { type }, selectExpression);
        }

        private Expression GetRestrictedColumnExpression(Expression columnExpression, Expression entityParameter, Type type, PropertyInfo property)
        {
            Expression canReadPropertyPredicate = GetColumnReadingRestrictionExpression(entityParameter, type, property.Name);
            return canReadPropertyPredicate != null
                ? Expression.Condition(canReadPropertyPredicate, columnExpression, Expression.Constant(property.GetDefaultValue(), property.PropertyType))
                : columnExpression;
        }

        private Expression GetColumnReadingRestrictionExpression(Expression parameter, Type type, string propertyName)
        {
            var rightsRestrictor = BuildColumnOperationRightsRestrictor<AllowReadingColumnAttribute>(type, propertyName);
            if (rightsRestrictor?.IsRestricted())
            {
                var restrictionExpression = rightsRestrictor?.GetRightsRestrictionsExpression() as LambdaExpression;
                return restrictionExpression?.Body?.ReplaceParameter(restrictionExpression.Parameters.Single(), parameter);
            }
            return null;
        }

        private LambdaExpression GetRestrictionExpression<TQueryRecordsRestrictionAttribute>(Type entityType)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var restrictor = BuildQueryRecordsRightsRestrictor<TQueryRecordsRestrictionAttribute>(entityType);
            if (restrictor?.IsRestricted())
            {
                return restrictor?.GetRightsRestrictionsExpression() as LambdaExpression;
            }
            return null;
        }

        private dynamic BuildQueryRecordsRightsRestrictor<TQueryRecordsRestrictionAttribute>(Type entityType)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var restrictorType = typeof(QueryRecordsRestrictor<,>).MakeGenericType(entityType, typeof(TQueryRecordsRestrictionAttribute));
            return Activator.CreateInstance(restrictorType, _userDataAccessor, _filterConverter);
        }
        private dynamic BuildColumnOperationRightsRestrictor<TColumnOperationRestrictionAttribute>(Type entityType, string columnName)
            where TColumnOperationRestrictionAttribute : BaseColumnOperationRestrictionAttribute
        {
            var restrictorType = typeof(ColumnOperationRestrictor<,>).MakeGenericType(entityType, typeof(TColumnOperationRestrictionAttribute));
            return Activator.CreateInstance(restrictorType, _userDataAccessor, columnName);
        }
    }
}
