namespace DataManagmentSystem.Common.SelectQuery {
    using DataManagmentSystem.Common.CoreEntities;
    using DataManagmentSystem.Common.Attributes;
    using System;
    using System.Linq.Expressions;

    public interface IColumnToSelectConverter
    {
        Expression<Func<TEntity, TEntity>> Convert<TEntity, TQueryRecordsRestrictionAttribute>(SelectColumn columns, bool canSkipLocalization = false, bool isColumnReadingRestricted = true, bool ignoreDeletedRecords = true)
            where TEntity : BaseEntity, new()
            where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute;
    }
}
