using System;
using System.Collections;
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
using DataManagmentSystem.Common.PropertyCache;
using DataManagmentSystem.Common.Request;
using DataManagmentSystem.Common.SelectQuery.Factory;

namespace DataManagmentSystem.Common.SelectQuery.Strategy
{
    public class RelatedColumnToExpressionStrategy<TQueryRecordsRestrictionAttribute> : BaseConvertionStrategy
        where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
    {
        private readonly IPropertyCache _propertyCache;
        private readonly IColumnToSelectConverter _columnToSelectConverter;
        private readonly IFilterToExpressionConverter _filterConverter;

        public RelatedColumnToExpressionStrategy(IPropertyCache propertyCache,
            IUserDataAccessor userDataAccessor,
            IColumnToSelectConverter columnToSelectConverter,
            IFilterToExpressionConverter filterConverter) : base(userDataAccessor)
        {
            _propertyCache = propertyCache;
            _columnToSelectConverter = columnToSelectConverter;
            _filterConverter = filterConverter;
        }

        public override Dictionary<MemberInfo, Expression> Convert(IEnumerable<dynamic> columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords)
        {
            var expressions = new Dictionary<MemberInfo, Expression>();
            foreach (var relatedColumn in columns.Cast<SelectRelatedColumn>())
            {
                var property = _propertyCache.GetPropertyByName(type, relatedColumn.ColumnName);
                if (!(property?.IsCalculatedField() ?? true))
                {
                    var isEnumerationProperty = typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                        property.PropertyType.GenericTypeArguments.Any();
                    var memberType = isEnumerationProperty ? property.PropertyType.GenericTypeArguments.Single()
                        : property.PropertyType;
                    var currentPropertyParameter = Expression.Property(parameter, property.Name);
                    var typeParameter = Expression.Parameter(memberType);
                    Expression memberInitParameter;
                    if (isEnumerationProperty)
                    {
                        memberInitParameter = typeParameter;
                    }
                    else
                    {
                        memberInitParameter = currentPropertyParameter;
                    }
                    FillExpressions(expressions, new MemberInfoDto
                    {
                        RelatedColumn = relatedColumn,
                        MemberInitParameter = memberInitParameter,
                        MemberType = memberType,
                        IsColumnReadingRestricted = isColumnReadingRestricted,
                        IgnoreDeletedRecords = ignoreDeletedRecords,
                        IsEnumerationProperty = isEnumerationProperty,
                        TypeParameter = typeParameter,
                        CurrentPropertyParameter = currentPropertyParameter,
                        Parameter = parameter,
                        Type = type,
                        Property = property
                    });
                }
            }
            return expressions;
        }

        private void FillExpressions(Dictionary<MemberInfo, Expression> expressions, MemberInfoDto info)
        {
            var memberInit = _columnToSelectConverter.GetMemeberInit<TQueryRecordsRestrictionAttribute>(info.RelatedColumn,
                        info.MemberInitParameter, info.MemberType, info.IsColumnReadingRestricted, info.IgnoreDeletedRecords);
            var restrictionExpression = GetRestrictionExpression(info.IsEnumerationProperty ? info.TypeParameter.Type : info.CurrentPropertyParameter.Type);
            var assigningExpression = info.IsEnumerationProperty ? GetEnumerableAssigningExpression(info.TypeParameter, memberInit, info.CurrentPropertyParameter, restrictionExpression, info.IgnoreDeletedRecords)
                : GetReferenceEntityAssigningExpression(info.CurrentPropertyParameter, memberInit, restrictionExpression, info.IgnoreDeletedRecords);
            assigningExpression = info.IsColumnReadingRestricted
                ? GetRestrictedColumnExpression(assigningExpression, info.Parameter, info.Type, info.Property)
                : assigningExpression;
            expressions.Add(info.Property, assigningExpression);
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
            if (ignoreDeletedRecords)
            {
                condition = Expression.AndAlso(condition, GetNotDeletedRecordsExpression(memberParameter));
            }
            return Expression.Condition(condition, memberInit, nullExpression);
        }

        private LambdaExpression GetRestrictionExpression(Type entityType)
        {
            var restrictor = QueryRightsRestrictorFactory.BuildRecords<TQueryRecordsRestrictionAttribute>(entityType, _userDataAccessor, _filterConverter);
            if (restrictor?.IsRestricted())
            {
                return restrictor?.GetRightsRestrictionsExpression() as LambdaExpression;
            }
            return null;
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

        private static Expression GetNotDeletedRecordsExpression(Expression memberParameter)
        {
            Expression<Func<BaseEntity, bool>> exp = entity => !entity.IsDeleted;
            return exp.Body.ReplaceParameter(exp.Parameters.Single(), memberParameter);
        }

        private sealed class MemberInfoDto
        {
            public SelectRelatedColumn RelatedColumn { get; set; }
            public Expression MemberInitParameter { get; set; }
            public Type MemberType { get; set; }
            public bool IsColumnReadingRestricted { get; set; }
            public bool IgnoreDeletedRecords { get; set; }
            public bool IsEnumerationProperty { get; set; }
            public ParameterExpression TypeParameter { get; set; }
            public Expression CurrentPropertyParameter { get; set; }
            public Expression Parameter { get; set; }
            public Type Type { get; set; }
            public PropertyInfo Property { get; set; }

        }
    }
}
