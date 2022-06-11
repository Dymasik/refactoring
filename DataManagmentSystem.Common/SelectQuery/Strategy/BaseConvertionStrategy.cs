using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataManagmentSystem.Auth.Injector;
using DataManagmentSystem.Common.Attributes;
using DataManagmentSystem.Common.Extensions;
using DataManagmentSystem.Common.SelectQuery.Factory;

namespace DataManagmentSystem.Common.SelectQuery.Strategy
{
    public abstract class BaseConvertionStrategy : IColumnToExpressionStrategy
    {
        protected readonly IUserDataAccessor _userDataAccessor;

        protected BaseConvertionStrategy(IUserDataAccessor userDataAccessor) {
            _userDataAccessor = userDataAccessor;
        }

        public abstract Dictionary<MemberInfo, Expression> Convert(IEnumerable<dynamic> columns, Expression parameter, Type type, bool isColumnReadingRestricted, bool ignoreDeletedRecords);

        protected Expression GetRestrictedColumnExpression(Expression columnExpression, Expression entityParameter, Type type, PropertyInfo property)
        {
            Expression canReadPropertyPredicate = GetColumnReadingRestrictionExpression(entityParameter, type, property.Name);
            return canReadPropertyPredicate != null
                ? Expression.Condition(canReadPropertyPredicate, columnExpression, Expression.Constant(property.GetDefaultValue(), property.PropertyType))
                : columnExpression;
        }

        protected Expression GetColumnReadingRestrictionExpression(Expression parameter, Type type, string propertyName)
        {
            var rightsRestrictor = BuildRestrictor(type, propertyName);
            if (rightsRestrictor?.IsRestricted())
            {
                var restrictionExpression = rightsRestrictor?.GetRightsRestrictionsExpression() as LambdaExpression;
                return restrictionExpression?.Body?.ReplaceParameter(restrictionExpression.Parameters.Single(), parameter);
            }
            return null;
        }

        protected dynamic BuildRestrictor(Type type, string columnName) {
            return QueryRightsRestrictorFactory.BuildColumn<AllowReadingColumnAttribute>(type, _userDataAccessor, columnName);
        }
    }
}
