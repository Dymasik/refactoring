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
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading.Tasks;
	using DataManagmentSystem.Common.Extensions;

	public class QueryRecordsRestrictor<TEntity, TQueryRecordsRestrictionAttribute> : BaseRestrictor<TEntity, TQueryRecordsRestrictionAttribute>
		where TEntity : BaseEntity
		where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
	{

		public QueryRecordsRestrictor(IUserDataAccessor userDataAccessor, IFilterToExpressionConverter filterConverter) : base(userDataAccessor) {
			_filterToExpressionConverter = filterConverter;
		}

		private IFilterToExpressionConverter _filterToExpressionConverter;

		public RequestFilter ApplyRightsRestrictionsForFilters(RequestFilter filter) {
			if (RestrictionMethod.ReturnType == typeof(RequestFilter)) {
				filter.AddRequiredFilter((RequestFilter)RestrictionMethod.Invoke(typeof(TEntity), new object[] { _user }));
			}
			return filter;
		}

		public IQueryable<TEntity> ApplyRightsRestrictionsForQuery(IQueryable<TEntity> query) {
			if (RestrictionMethod.ReturnType == typeof(IQueryable<TEntity>)) {
				query = (IQueryable<TEntity>)RestrictionMethod.Invoke(typeof(TEntity), new object[] { query, _user });
			}
			return query;
		}

		public override LambdaExpression GetRightsRestrictionsExpression() {
			LambdaExpression expression = null;
			var query = Enumerable.Empty<TEntity>().AsQueryable();
			if (RestrictionMethod?.ReturnType == typeof(IQueryable<TEntity>)) {
				query = (IQueryable<TEntity>)RestrictionMethod.Invoke(typeof(TEntity), new object[] { query, _user });
				if (query.Expression is MethodCallExpression exp && exp.Method.Name == nameof(Enumerable.Where)) {
					var whereExpression = (MethodCallExpression)query.Expression;
					var whereExpressionBody = (UnaryExpression)whereExpression.Arguments[1];
					expression = ((Expression<Func<TEntity, bool>>)whereExpressionBody.Operand);
				}
			} else if (RestrictionMethod?.ReturnType == typeof(RequestFilter)) {
				var filters = (RequestFilter)RestrictionMethod.Invoke(typeof(TEntity), new object[] { _user });
				expression = _filterToExpressionConverter.Convert<TEntity>(filters, false);
			}
			return expression;
		}
	}
}