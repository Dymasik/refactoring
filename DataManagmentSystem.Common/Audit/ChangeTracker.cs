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

	public class ChangeTracker
	{
		private readonly UserModel _user;
        private readonly EntityEntry _entityEntry;
        private BaseEntity _entity => (BaseEntity)_entityEntry.Entity;
        private Type _entityType => _entity.GetType();
        private readonly Type _objectType;
        private readonly IModel _model;
		public ChangeTracker(UserModel user, EntityEntry entityEntry, IModel model) {
			_user = user;
            _entityEntry = entityEntry;
            _model = model;
		}

		private Guid GetEntityEntryPrimaryKey() {
			IEntityType entityType = _model.FindEntityType(_entityType);
			string keyName = entityType.FindPrimaryKey().Properties.Select(x => x.Name).Single();
			return (Guid)_entityType.GetProperty(keyName).GetValue(_entity, null);
		}

        public BaseChangeTrackingEntity CreateChangeTrackingEntity() {
            var changeTrackingAttribute = Attribute.GetCustomAttribute(_entityType, typeof(ChangeTrackingStoreAttribute)) as ChangeTrackingStoreAttribute;
			var changeTrackingType = changeTrackingAttribute.ChangeTrackingType;
            var changeTrackingEntity = (BaseChangeTrackingEntity)Activator.CreateInstance(changeTrackingType);
			changeTrackingEntity.RecordId = GetEntityEntryPrimaryKey();
            changeTrackingEntity.Action = GetChangeTrackingAction(_entityEntry);
            foreach(var property in _entityEntry.CurrentValues.Properties) {
				if(!property.IsBaseColumn()){
					changeTrackingType.GetProperty(property.Name).SetValue(changeTrackingEntity, _entityEntry.CurrentValues[property], null);
				}
			}
			return changeTrackingEntity;
        }

        public static bool IsChangeTrackingEnabled(EntityEntry entityEntry) {
            string action = GetChangeTrackingAction(entityEntry);
            if(string.IsNullOrEmpty(action)){
                return false;
            }
            return (Attribute.GetCustomAttribute(entityEntry.Entity.GetType(), typeof(ChangeTrackingStoreAttribute)) as ChangeTrackingStoreAttribute) != null;
        }

        private static bool IsEntityDeleted(EntityEntry entityEntry){
            var deletedFlagProperty = entityEntry.CurrentValues.Properties.FirstOrDefault(property => property.Name == AuditColumns.IS_DELETED_COLUMN_NAME);
			bool isEntityRestored = !(bool)entityEntry.CurrentValues[deletedFlagProperty] && (bool)entityEntry.OriginalValues[deletedFlagProperty];
			return entityEntry.State == EntityState.Deleted || ((bool)entityEntry.CurrentValues[deletedFlagProperty] && !(bool)entityEntry.OriginalValues[deletedFlagProperty]);
        }

        private static bool IsEntityRestored(EntityEntry entityEntry){
            var deletedFlagProperty = entityEntry.CurrentValues.Properties.FirstOrDefault(property => property.Name == AuditColumns.IS_DELETED_COLUMN_NAME);
			return entityEntry.State == EntityState.Modified && !(bool)entityEntry.CurrentValues[deletedFlagProperty] && (bool)entityEntry.OriginalValues[deletedFlagProperty];
        }

        private static string GetChangeTrackingAction(EntityEntry entityEntry){
            if(IsEntityDeleted(entityEntry)){
				return EntityAction.DELETE_ACTION;
			} else if(IsEntityRestored(entityEntry)){
				return EntityAction.RESTORE_ACTION;
			} else if(entityEntry.State == EntityState.Added){
				return EntityAction.CREATE_ACTION;
			} else if(entityEntry.State == EntityState.Modified){
				return EntityAction.EDIT_ACTION;
			} else {
				return null;
			}
        }
	}
}