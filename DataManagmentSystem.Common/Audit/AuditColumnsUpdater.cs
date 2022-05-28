namespace DataManagementSystem.Common.Audit
{
	using Microsoft.EntityFrameworkCore;
	using DataManagmentSystem.Common.CoreEntities;
	using System;
	using DataManagmentSystem.Auth.Injector.User;
	using Microsoft.EntityFrameworkCore.ChangeTracking;

	public class AuditColumnsUpdater
	{
		private readonly UserModel _user;
        private readonly EntityEntry _entityEntry;
        private BaseEntity _entity => (BaseEntity)_entityEntry.Entity;
		public AuditColumnsUpdater(UserModel user, EntityEntry entityEntry) {
			_user = user;
            _entityEntry = entityEntry;
		}

        public void UpdateAuditColumns(){
			switch(_entityEntry.State){
				case EntityState.Added:
					_entity.CreatedOn = DateTime.UtcNow;
					_entity.CreatedByUserName = _user?.UserName;
                    _entity.ModifiedOn = DateTime.UtcNow;
					_entity.ModifiedByUserName = _user?.UserName;
					break;
				case EntityState.Modified:
                    _entity.ModifiedOn = DateTime.UtcNow;
					_entity.ModifiedByUserName = _user?.UserName;
					break;
				default:
					return;
			}
        }
	}
}