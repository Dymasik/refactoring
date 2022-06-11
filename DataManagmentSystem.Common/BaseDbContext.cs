namespace DataManagementSystem.Common
{
	using Microsoft.EntityFrameworkCore;
	using DataManagmentSystem.Common.CoreEntities;
	using System.Threading;
	using System.Linq;
	using System.Threading.Tasks;
	using System;
	using DataManagmentSystem.Auth.Injector;
	using DataManagmentSystem.Auth.Injector.User;
	using DataManagementSystem.Common.Audit;
    using DataManagmentSystem.Common.Extensions;

    public class BaseDbContext : DbContext
	{
		private readonly IUserDataAccessor _userDataAccessor;
		private static async Task<UserModel> GetCurrentUserInfo(IUserDataAccessor userDataAccessor) {
			try {
				return await userDataAccessor.GetCurrentUserInfo();
			} catch {
				return null;
			}
		}
		public BaseDbContext(DbContextOptions options, IUserDataAccessor userDataAccessor) : base(options) {
			_userDataAccessor = userDataAccessor;
		}

		public DbSet<LanguageEntity> Languages { get; set; }
        public DbSet<CurrencyEntity> Currencies { get; set; }
        public DbSet<MultiCurrencyAmountEntity> MultiCurrencyAmounts { get; set; }
		public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) {
			var user = await GetCurrentUserInfo(_userDataAccessor);

			ChangeTracker.Entries()
				.ToList()
				.ForEach(entityEntry => {
					if (Audit.ChangeTracker.IsChangeTrackingEnabled(entityEntry)){
						var changeTracker = new ChangeTracker(user, entityEntry, Model);
						var changeTrackingEntity = changeTracker.CreateChangeTrackingEntity();
						if(ChangeApprovalConfigurator.IsApprovalRequired(user, entityEntry)){
							var changeApprovalConfigurator = new ChangeApprovalConfigurator(entityEntry, changeTrackingEntity);
							changeApprovalConfigurator.SetupApproval();
						}
						Add(changeTrackingEntity);
					}
				});

			ChangeTracker.Entries()
				.ToList()
				.ForEach(entityEntry => {
					var auditColumnsUpdater = new AuditColumnsUpdater(user, entityEntry);
					auditColumnsUpdater.UpdateAuditColumns();
					if(ChangeApprover.IsChangeApproval(entityEntry)){
						var changeApprover = new ChangeApprover(user, entityEntry);
						changeApprover.ApproveChanges();
					}
				});

			return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) { 
			base.OnModelCreating(modelBuilder);
			modelBuilder.HasDbFunction(typeof(DbFunction).GetMethod(nameof(DbFunction.GetDistance), new[] { typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal) }))
				.HasName("GetDistance");
		}
	}
}