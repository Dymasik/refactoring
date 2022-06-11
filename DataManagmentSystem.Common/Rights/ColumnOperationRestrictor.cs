namespace DataManagmentSystem.Common.Rights
{
	using DataManagmentSystem.Auth.Injector;
	using DataManagmentSystem.Auth.Injector.Exceptions;
	using DataManagmentSystem.Auth.Injector.User;
	using DataManagmentSystem.Common.Attributes;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Request;
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq.Expressions;
    using DataManagmentSystem.Common.Extensions;

    public class ColumnOperationRestrictor<TEntity, TColumnOperationRestrictionAttribute> : BaseRestrictor<TEntity, TColumnOperationRestrictionAttribute>
		where TEntity : BaseEntity
		where TColumnOperationRestrictionAttribute : BaseColumnOperationRestrictionAttribute
	{

		public ColumnOperationRestrictor(IUserDataAccessor userDataAccessor, string columnName) : base(userDataAccessor) {
            _columnName = columnName;
		}

		protected override IEnumerable<TColumnOperationRestrictionAttribute> GetRestrictionAttributes(MethodInfo method) 
			=> method.GetCustomAttributes<TColumnOperationRestrictionAttribute>(true)
					.Where(attr => attr.ColumnName == _columnName);
		
        private readonly string _columnName;

		public override LambdaExpression GetRightsRestrictionsExpression() {
			LambdaExpression expression = null;
			if (RestrictionMethod?.ReturnType == typeof(LambdaExpression)) {
				expression = (LambdaExpression)RestrictionMethod.Invoke(typeof(TEntity), new object[] { _user });
			}
			return expression;
		}
	}
}