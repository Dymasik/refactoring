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
    using DataManagmentSystem.Common.SelectQuery.Strategy;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class BaseColumnToSelectConverter : IColumnToSelectConverter
    {

        private const string INCLUDED_COLUMN_SEPARATOR = ".";
        private readonly IColumnToExpressionStrategy _baseColumnStrategy;
        private readonly IColumnToExpressionStrategy _relatedColumnStrategy;

        protected IPropertyCache PropertyCache { get; }
        protected string CurrentLanguageCode { get; }

        public BaseColumnToSelectConverter(IPropertyCache propertyCache, IObjectLocalizer localizer,
            IColumnToExpressionStrategy baseColumnStrategy,
            IColumnToExpressionStrategy relatedColumnStrategy)
        {
            PropertyCache = propertyCache;
            _baseColumnStrategy = baseColumnStrategy;
            _relatedColumnStrategy = relatedColumnStrategy;
            CurrentLanguageCode = localizer.GetCulture().TwoLetterISOLanguageName;
        }

        public Expression<Func<TEntity, TEntity>> Convert<TEntity, TQueryRecordsRestrictionAttribute>(SelectColumn columns, bool canSkipLocalization = false, bool isColumnReadingRestricted = true, bool ignoreDeletedRecords = true)
            where TEntity : BaseEntity, new()
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var init = GetMemeberInit<TQueryRecordsRestrictionAttribute>(columns, parameter, typeof(TEntity), isColumnReadingRestricted, ignoreDeletedRecords);
            return Expression.Lambda<Func<TEntity, TEntity>>(init, parameter);
        }

        public Expression GetMemeberInit<TQueryRecordsRestrictionAttribute>(SelectColumn columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords)
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
        {
            List<string> columnNames = new List<string>();
            columnNames.AddRange(columns.ColumnNames);
            columnNames.AddRange(columns.RelatedColumns.Select(relatedColumn => relatedColumn.ColumnName));
            var calculatedIncludeColumns = GetCalculatedIncludedColumns(columnNames, type);
            AddIncludedColumns(columns, calculatedIncludeColumns);
            var bindings = new List<MemberBinding>();
            var columnsExpression = _baseColumnStrategy.Convert(columns.ColumnNames, parameter, type, isColumnReadingRestricted, ignoreDeletedRecords);
            foreach (var columnExpression in columnsExpression)
            {
                bindings.Add(Expression.Bind(columnExpression.Key, columnExpression.Value));
            }
            var relatedColumnsExpression = _relatedColumnStrategy.Convert(columns.RelatedColumns, parameter, type, isColumnReadingRestricted, ignoreDeletedRecords);
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
    }
}
