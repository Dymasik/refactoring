namespace DataManagementSystem.Common.Audit
{
	using Microsoft.EntityFrameworkCore;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Attributes;
	using System.Linq;
	using System;
	using DataManagmentSystem.Auth.Injector.User;
	using Microsoft.EntityFrameworkCore.ChangeTracking;

	public class ChangeApprovalConfigurator
	{
        private readonly EntityEntry _entityEntry;
        private BaseEntity _entity => _entityEntry.Entity as BaseEntity;
        private readonly BaseChangeTrackingEntity _changeTrackingEntity;
		public ChangeApprovalConfigurator(EntityEntry entityEntry, BaseChangeTrackingEntity changeTrackingEntity) {
            _entityEntry = entityEntry;
            _changeTrackingEntity = changeTrackingEntity;
		}
        
        public void SetupApproval(){
            _changeTrackingEntity.IsApproved = false;
            RollbackEntityChanges();
        }

        public static bool IsApprovalRequired(UserModel user, EntityEntry entityEntry) {
            var changeTrackingAttribute = Attribute.GetCustomAttribute(entityEntry.Entity.GetType(), typeof(ChangeTrackingStoreAttribute)) as ChangeTrackingStoreAttribute;
            var moderatorRoles = changeTrackingAttribute.ModeratorRoles;
            return moderatorRoles.Any() 
				&& !moderatorRoles.Any(role => user?.Roles.Any(userRole => userRole.Name == role.Name) ?? false);
        }
        
        private void RollbackEntityChanges()
		{
			switch (_entityEntry.State)
			{
				case EntityState.Modified:
				case EntityState.Deleted:
					_entityEntry.State = EntityState.Modified; //Revert changes made to deleted entity.
					_entityEntry.State = EntityState.Unchanged;
					break;
				case EntityState.Added:
					_entity.IsDeleted = true;
					break;
			}
		}
	}
}