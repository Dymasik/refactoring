namespace DataManagmentSystem.Common.Rights
{
	using DataManagmentSystem.Auth.Injector;
	using DataManagmentSystem.Auth.Injector.Exceptions;
	using DataManagmentSystem.Auth.Injector.User;
	using DataManagmentSystem.Common.Attributes;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Request;
	using System.Collections.Generic;
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading.Tasks;
    using DataManagmentSystem.Common.Extensions;

    public abstract class BaseRestrictor<TEntity, TRestrictionAttribute>
		where TEntity : BaseEntity
		where TRestrictionAttribute : BaseRestrictionAttribute
	{

		protected BaseRestrictor(IUserDataAccessor userDataAccessor) {
            _user = GetCurrentUserInfo(userDataAccessor);
        }

		public bool IsRestricted() =>
			typeof(TEntity)
				.GetMethods()
				.Any(method => GetRestrictionAttributes(method).Any());


		protected virtual IEnumerable<TRestrictionAttribute> GetRestrictionAttributes(MethodInfo method) 
			=> method.GetCustomAttributes<TRestrictionAttribute>(true);

		private static async Task<UserModel> GetCurrentUserInfoAsync(IUserDataAccessor userDataAccessor) {
			try {
				return await userDataAccessor.GetCurrentUserInfo();
			} catch (AggregateException ae) {
                ae.Handle(e => {
                    return e is UnauthorizedAccessException;
                });
                return null;
            }
		}

		private static UserModel GetCurrentUserInfo(IUserDataAccessor userDataAccessor) {
			return GetCurrentUserInfoAsync(userDataAccessor).Result;
		}
		
		protected UserModel _user { get; set; }

		private MethodInfo _restrictionMethod;
		protected MethodInfo RestrictionMethod {
			get {
				if (_restrictionMethod == null) {
					_restrictionMethod = GetRestrictionMethod();
				}
				return _restrictionMethod;
			}
		}

		public MethodInfo GetRestrictionMethod() {
			return typeof(TEntity)
				.GetMethods()
				.SelectMany(method => GetRestrictionAttributes(method)
					.Select(attr => new {
						Method = method,
						RestrictionAttribute = attr
					})
				)
				.Where(_ => _.RestrictionAttribute != null)
				.OrderBy(_ => _.RestrictionAttribute.Position)
				.FirstOrDefault(_ =>
					(_user?.Roles.Any(userRole =>
						_.RestrictionAttribute.Roles.Any(role => userRole.Name == role.Name)) ?? false)
					|| !_.RestrictionAttribute.Roles.Any())?.Method;
		}

		public void ThrowExceptionIfUserNotAllowedToAccessData() {
			if (RestrictionMethod == null) {
				if (_user == null) {
					throw new UnauthorizedAccessException();
				} else {
					throw new ForbiddenException();
				}
			}
		}

		public abstract LambdaExpression GetRightsRestrictionsExpression();
	}
}