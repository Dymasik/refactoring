namespace DataManagmentSystem.Common.Repository
{
    using System;
	using DataManagmentSystem.Common.CoreEntities;
	using System.Collections.Generic;
    using System.Threading.Tasks;
    using DataManagmentSystem.Common.Request;
    using DataManagmentSystem.Common.SelectQuery;
    using DataManagmentSystem.Common.Attributes;

    public interface IRepository<TEntity>
        where TEntity : BaseEntity
    {
        Task<IEnumerable<TEntity>> GetEntities<TQueryRecordsRestrictionAttribute>(GetEntitiesOptions options) where TQueryRecordsRestrictionAttribute: BaseQueryRecordsRestrictionAttribute;

        Task<TEntity> AddEntity(TEntity entity);
        Task<TEntity> UpdateEntity(TEntity entity);
        Task<TEntity> DeleteEntity(TEntity entity, bool usePhysicalDeletion);
        Task<IEnumerable<TEntity>> DeleteEntities<TQueryRecordsRestrictionAttribute>(RequestFilter filters, bool canSkipLocalization, bool usePhysicalDeletion, bool ignoreDeletedRecords)  where TQueryRecordsRestrictionAttribute: BaseQueryRecordsRestrictionAttribute;
        TEntity CreateEntity();
        Task<object> GetAggregation<TQueryRecordsRestrictionAttribute>(AggregationItem aggregation, RequestFilter filters, bool canSkipLocalization, bool ignoreDeletedRecords)  where TQueryRecordsRestrictionAttribute: BaseQueryRecordsRestrictionAttribute;
    }
}
