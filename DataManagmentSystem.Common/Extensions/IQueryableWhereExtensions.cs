namespace DataManagmentSystem.Common.Extensions
{
	using DataManagmentSystem.Auth.Injector;
	using DataManagmentSystem.Common.Attributes;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Request;
	using DataManagmentSystem.Common.Rights;
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading.Tasks;

	public static class IQueryableWhereExtensions
	{

		public static async Task<IQueryable<TEntity>> Where<TEntity, TQueryRecordsRestrictionAttribute>(this IQueryable<TEntity> query, RequestFilter filter, IFilterToExpressionConverter converter, IUserDataAccessor userDataAccessor, bool canSkipLocalization, bool ignoreDeletedRecords)
			where TEntity : BaseEntity
			where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute
		{
			var queryRecordsRestrictor = new QueryRecordsRestrictor<TEntity, TQueryRecordsRestrictionAttribute>(userDataAccessor, converter);
			if (queryRecordsRestrictor.IsRestricted()) {
				queryRecordsRestrictor.ThrowExceptionIfUserNotAllowedToAccessData();
				query = queryRecordsRestrictor.ApplyRightsRestrictionsForQuery(query);
				filter = queryRecordsRestrictor.ApplyRightsRestrictionsForFilters(filter);
			}
			var expression = converter.Convert<TEntity>(filter, canSkipLocalization);
			if (expression != null) {
				if (expression.CanReduce)
					expression = expression.Reduce() as Expression<Func<TEntity, bool>>;
				query = query.Where(expression);
			}
			if(ignoreDeletedRecords){
				query = query.Where(entity => !entity.IsDeleted);
			}
			return query;
		}
	}
}
