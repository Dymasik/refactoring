namespace DataManagmentSystem.Common.Repository
{
	using DataManagmentSystem.Auth.Injector;
	using DataManagmentSystem.Common.Extensions;
	using DataManagmentSystem.Common.Locale;
	using DataManagmentSystem.Common.CoreEntities;
	using DataManagmentSystem.Common.Attributes;
	using Microsoft.EntityFrameworkCore;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using DataManagmentSystem.Common.Request;
	using DataManagmentSystem.Common.SelectQuery;
	using DataManagmentSystem.Common.Email;
	using MimeKit;
	using DataManagementSystem.ReportService.Configuration;
	using DataManagmentSystem.Auth.Injector.Exceptions;
	using MediatR;
	using DataManagmentSystem.Entity.Messaging.Events;
	using System;
	using System.Reflection;

	public class BaseRepository<TEntity, TContext> : IRepository<TEntity>, IEntityEventHandler<TEntity>
		where TEntity : BaseEntity, new()
		where TContext : DbContext
	{

		protected readonly TContext _context;
		private readonly IUserDataAccessor _userDataAccessor;
		private readonly IMediator _mediator;

		protected IObjectLocalizer Localizer { get; }
		protected ILocalizationEntityResolver LocalizeResolver { get; }
		protected IEmailService<MimeMessage> EmailService { get; }
		protected ReportService ReportService { get; }
		protected IColumnToSelectConverter SelectColumnConverter { get; }
		public IFilterToExpressionConverter FilterConverter { get; }

		public BaseRepository(TContext context,
			IObjectLocalizer localizer,
			ILocalizationEntityResolver localizeResolver,
			IUserDataAccessor userDataAccessor,
			IEmailService<MimeMessage> emailService,
			ReportService reportService,
			IColumnToSelectConverter selectColumnConverter,
			IFilterToExpressionConverter filterConverter,
			IMediator mediator) {
			_context = context;
			Localizer = localizer;
			LocalizeResolver = localizeResolver;
			_userDataAccessor = userDataAccessor;
			EmailService = emailService;
			ReportService = reportService;
			SelectColumnConverter = selectColumnConverter;
			FilterConverter = filterConverter;
			_mediator = mediator;
		}

		public async Task<TEntity> AddEntity(TEntity entity) {
			bool canInsert = await OnInserting(entity);
			if (!canInsert) {
				throw new ForbiddenException();
			}
			var entry = _context.Entry(entity);
			var localizations = LocalizeResolver.GetLocalizations(entry);
			_context.Set<TEntity>().Add(entity);
			await SaveChanges();
			await LocalizeResolver.UpdateLocalizations(entry, localizations);
			await OnInserted(entity);
			return await Localizer.Localize(entity);
		}

		public async Task<TEntity> DeleteEntity(TEntity entity, bool usePhysicalDeletion) {
			var canDelete = await OnDeleting(entity);
			if (!canDelete) {
				throw new ForbiddenException();
			}
			if (!usePhysicalDeletion) {
				entity.IsDeleted = true;
			} else {
				_context.Set<TEntity>().Remove(entity);
			}
			await SaveChanges();
			await OnDeleted(entity);
			return await Localizer.Localize(entity);
		}

		public async Task<IEnumerable<TEntity>> DeleteEntities<TQueryRecordsRestrictionAttribute>(RequestFilter filters, bool canSkipLocalization, bool usePhysicalDeletion, bool ignoreDeletedRecords)
			where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute {
			var query = _context
				.Set<TEntity>()
				.Where<TEntity, AllowDeletingRecordsAttribute>(filters, FilterConverter, _userDataAccessor, canSkipLocalization, ignoreDeletedRecords);
			var entities = await query.AsQueryable()
				.ToListAsyncSafe();
			if (entities == null) {
				return null;
			}

			entities.ForEach(async e => {
				var canDelete = await OnDeleting(e);
				if (!canDelete) {
					throw new ForbiddenException();
				}
			});
			if (!usePhysicalDeletion) {
				entities.ForEach(e => { e.IsDeleted = true; });
			} else {
				_context.Set<TEntity>().RemoveRange(entities);
			}
			await SaveChanges();
			entities.ForEach(async e => await OnDeleted(e));

			return Localizer.Localize(entities);
		}

		public async Task<IEnumerable<TEntity>> GetEntities<TQueryRecordsRestrictionAttribute>(GetEntitiesOptions options)
		where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute {
			var query = _context
				.Set<TEntity>()
				.AsQueryable()
				.Where<TEntity, TQueryRecordsRestrictionAttribute>(options.Filters, FilterConverter, _userDataAccessor, options.CanSkipLocalization, options.IgnoreDeletedRecords);
			if (options.AsNoTracking) {
				query = query.AsNoTracking();
			}
			if (options.OrderOptions != null && options.OrderOptions.Any()) {
				query = query.OrderBy(options.OrderOptions);
			}
			if (options.Columns != null) {
				query = query.Select<TEntity, TQueryRecordsRestrictionAttribute>(options.Columns, SelectColumnConverter, options.CanSkipLocalization, options.IsColumnReadingRestricted, options.IgnoreDeletedRecords);
			}
			if (options.PageSize != default) {
				query = query.Skip(options.PageSize * options.PageIndex).Take(options.PageSize);
			}
			return await query.ToListAsyncSafe();
		}

		public Task<object> GetAggregation<TQueryRecordsRestrictionAttribute>(AggregationItem aggregation, RequestFilter filters, bool canSkipLocalization, bool ignoreDeletedRecords)
			where TQueryRecordsRestrictionAttribute : BaseQueryRecordsRestrictionAttribute {
			var query = _context
				.Set<TEntity>()
				.Where<TEntity, AllowReadingRecordsAttribute>(filters, FilterConverter, _userDataAccessor, canSkipLocalization, ignoreDeletedRecords);
			return Task.FromResult(query.Aggregate(aggregation));
		}

		public async Task<TEntity> UpdateEntity(TEntity entity) {
			var entry = _context.Entry(entity);
			var originalEntity = (TEntity)entry.OriginalValues.ToObject();
			var canUpdate = await OnUpdating(originalEntity, entity);
			if (!canUpdate) {
				throw new ForbiddenException();
			}
			var localizations = LocalizeResolver.GetLocalizations(entry);
			var isEntityChanged = LocalizeResolver.IsEntityModified(entry);
			entry.State = isEntityChanged ? EntityState.Modified : EntityState.Unchanged;
			if (isEntityChanged && !LocalizeResolver.IsDefaultCultureChoosed) {
				LocalizeResolver.UnstageLocalizedProperies(entry);
			}
			await SaveChanges();
			await LocalizeResolver.UpdateLocalizations(entry, localizations);
			await OnUpdated(originalEntity, entity);
			return await Localizer.Localize(entity);
		}

		public TEntity CreateEntity() {
			return new TEntity();
		}

		public virtual async Task<bool> OnInserting(TEntity entity) {
			try {
				(bool canInsertBasedOnEntityEventHandler, Dictionary<string, string> errors) = await _mediator.Send(new EntityInsertingEvent<TEntity> {
					Entity = entity
				});
                return canInsertBasedOnEntityEventHandler;
			} catch {
    			return await Task.FromResult<bool>(true);
			}
		}

		public virtual Task OnInserted(TEntity entity) {
            _mediator.Publish(new EntityInsertedEvent<BaseEntity> {
                Entity = entity
            });
            _mediator.Publish(new EntityInsertedEvent<TEntity> {
                Entity = entity
            });
			return Task.FromResult(0);
		}

		public virtual async Task<bool> OnUpdating(TEntity originalEntity, TEntity entity) {
			try {
				(bool canUpdateBasedOnEntityEventHandler, Dictionary<string, string> errors) = await _mediator.Send(new EntityUpdatingEvent<TEntity> {
					OriginalEntity = originalEntity,
					Entity = entity
				});
                return canUpdateBasedOnEntityEventHandler;
			} catch {
    			return await Task.FromResult<bool>(true);
			}
		}

		public virtual Task OnUpdated(TEntity originalEntity, TEntity entity) {
            _mediator.Publish(new EntityUpdatedEvent<BaseEntity> {
                OriginalEntity = originalEntity,
                Entity = entity
            });
            _mediator.Publish(new EntityUpdatedEvent<TEntity> {
                OriginalEntity = originalEntity,
                Entity = entity
            });
			return Task.FromResult(0);
		}

		public virtual async Task<bool> OnDeleting(TEntity entity) {
			try {
				(bool canDeleteBasedOnEntityEventHandler, Dictionary<string, string> errors) = await _mediator.Send(new EntityInsertingEvent<TEntity> {
					Entity = entity
				});
                return canDeleteBasedOnEntityEventHandler;
			} catch {
    			return await Task.FromResult<bool>(true);
			}
		}

		public virtual Task OnDeleted(TEntity entity) {
            _mediator.Publish(new EntityDeletedEvent<BaseEntity> {
                Entity = entity
            });
            _mediator.Publish(new EntityDeletedEvent<TEntity> {
                Entity = entity
            });
			return Task.FromResult(0);
		}

		protected virtual async Task<int> SaveChanges()
		{
			return await _context.SaveChangesAsync();
		}
	}
}
