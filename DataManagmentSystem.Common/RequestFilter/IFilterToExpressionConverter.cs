namespace DataManagmentSystem.Common.Request {
    using DataManagmentSystem.Common.CoreEntities;
    using System;
    using System.Linq.Expressions;

    public interface IFilterToExpressionConverter  
    {
        Expression<Func<TEntity, bool>> Convert<TEntity>(RequestFilter filter, bool canSkipLocalization = false) where TEntity : BaseEntity;
    }
}
