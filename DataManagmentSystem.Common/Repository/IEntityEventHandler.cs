namespace DataManagmentSystem.Common.Repository
{
	using DataManagmentSystem.Common.CoreEntities;
    using System.Threading.Tasks;

    public interface IEntityEventHandler<TEntity>
        where TEntity : BaseEntity
    {
        Task<bool> OnInserting(TEntity entity);
        Task OnInserted(TEntity entity);
        Task<bool> OnUpdating(TEntity originalEntity, TEntity entity);
        Task OnUpdated(TEntity originalEntity, TEntity entity);
        Task<bool> OnDeleting(TEntity entity);
        Task OnDeleted(TEntity entity);
    }
}
