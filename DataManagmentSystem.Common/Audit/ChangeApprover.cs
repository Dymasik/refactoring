namespace DataManagementSystem.Common.Audit
{
	using Microsoft.EntityFrameworkCore;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Attributes;
	using System.Linq;
	using System;
	using DataManagmentSystem.Auth.Injector.User;
	using Microsoft.EntityFrameworkCore.ChangeTracking;
	using Microsoft.EntityFrameworkCore.Metadata;
	using DataManagmentSystem.Common.Extensions;
	using DataManagmentSystem.Auth.Injector.Exceptions;

	public class ChangeApprover
	{
		private readonly UserModel _user;
        private readonly EntityEntry _entityEntry;
        private BaseChangeTrackingEntity _entity => _entityEntry.Entity as BaseChangeTrackingEntity;
        private Type _entityType => _entity.GetType();
		public ChangeApprover(UserModel user, EntityEntry entityEntry) {
			_user = user;
            _entityEntry = entityEntry;
		}

        private BaseEntity GetTrackedEntity(){
            var foreignKeyFieldName = _entityType.GetProperty("RecordId").GetForeignKeyAttributeValue();
			_entityEntry.Navigation(foreignKeyFieldName).Load();
			return (BaseEntity)_entityType.GetProperty(foreignKeyFieldName).GetValue(_entity, null);
        }

		private string GetTrackingEntityPropertyName(IProperty changeTrackingEntityProperty) {
			if (!changeTrackingEntityProperty.IsBaseColumn()) {
				return changeTrackingEntityProperty.Name;
			} else {
				if (changeTrackingEntityProperty.Name == AuditColumns.CREATEDBY_COLUMN_NAME) {
					return AuditColumns.MODIFIEDBY_COLUMN_NAME;
				} else if (changeTrackingEntityProperty.Name == AuditColumns.CREATEDON_COLUMN_NAME) {
					return AuditColumns.MODIFIEDON_COLUMN_NAME;
				}
				return null;
			}
		}

		public void ApproveChanges() {
			var trackedEntity = GetTrackedEntity();
			if(!CanApproveEntity(trackedEntity)){
				throw new ForbiddenException($"User ({_user?.UserName ?? "anonymous"}) can't approve {_entityType.GetEntityTableName()} with Id = {_entity.Id}");
			}

			UpdateEntityValuesWithChangeTrackingValues(trackedEntity);
			switch(_entity.Action){
				case EntityAction.CREATE_ACTION:
					trackedEntity.IsDeleted = false;
					break;
				case EntityAction.DELETE_ACTION:
					trackedEntity.IsDeleted = true;
					break;
				case EntityAction.RESTORE_ACTION:
					trackedEntity.IsDeleted = false;
					break;
				case EntityAction.EDIT_ACTION:
					break;
				default:
					throw new InvalidOperationException($"Unexpected change tracking entity action: {_entity.Action}");
			}
		}

		private void UpdateEntityValuesWithChangeTrackingValues(BaseEntity trackedEntity){
			foreach(IProperty property in _entityEntry.CurrentValues.Properties) {
				string trackedEntityPropertyName = GetTrackingEntityPropertyName(property);
				if(string.IsNullOrEmpty(trackedEntityPropertyName)){
					continue;
				}
				trackedEntity
					.GetType()
					.GetProperty(property.Name)
					?.SetValue(trackedEntity, _entityEntry.CurrentValues[property], null);
			}
		}

        private bool CanApproveEntity(BaseEntity entity) {
            var changeTrackingAttribute = Attribute.GetCustomAttribute(entity.GetType(), typeof(ChangeTrackingStoreAttribute)) as ChangeTrackingStoreAttribute;
            var moderatorRoles = changeTrackingAttribute.ModeratorRoles;
            return moderatorRoles.Any(role => _user?.Roles.Any(userRole => userRole.Name == role.Name) ?? false);
        }

        public static bool IsChangeApproval(EntityEntry entityEntry){
            return IsChangeTrackingEntity(entityEntry) && IsEntityApproved(entityEntry);
        }

        private static bool IsChangeTrackingEntity(EntityEntry entityEntry){
            return entityEntry.Entity is BaseChangeTrackingEntity;
        }

        private static bool IsEntityApproved(EntityEntry entityEntry){
            var approvedFlagProperty = entityEntry.CurrentValues.Properties.FirstOrDefault(property => property.Name == AuditColumns.ISAPPROVED_COLUMN_NAME);
			return (bool)entityEntry.CurrentValues[approvedFlagProperty] && !(bool)entityEntry.OriginalValues[approvedFlagProperty];
        }
        
        public static void RollbackEntityChanges(EntityEntry entityEntry, BaseEntity entity)
		{
			switch (entityEntry.State)
			{
				case EntityState.Modified:
				case EntityState.Deleted:
					entityEntry.State = EntityState.Modified; //Revert changes made to deleted entity.
					entityEntry.State = EntityState.Unchanged;
					break;
				case EntityState.Added:
					entity.IsDeleted = true;
					break;
			}
		}
	}
}