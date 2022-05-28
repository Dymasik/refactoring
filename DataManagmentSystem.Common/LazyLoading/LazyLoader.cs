using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataManagmentSystem.Common.LazyLoading {
    public class LazyLoader : ILazyLoader {
        private bool _disposed;
        private IDictionary<string, bool> _loadedStates;

        public LazyLoader(ICurrentDbContext currentContext) {
            Context = currentContext.Context;
        }

        public virtual void SetLoaded(
            object entity,
            [CallerMemberName] string navigationName = "",
            bool loaded = true) {
            if (_loadedStates == null) {
                _loadedStates = new Dictionary<string, bool>();
            }

            _loadedStates[navigationName] = loaded;
        }

        protected virtual DbContext Context { get; }

        public virtual void Load(object entity, [CallerMemberName] string navigationName = "") {
            if (ShouldLoad(entity, navigationName, out var entry)) {
                entry.Load();
                var a = entry.CurrentValue;
            }
        }

        public virtual Task LoadAsync(
            object entity,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string navigationName = "") {
            return ShouldLoad(entity, navigationName, out var entry)
                ? entry.LoadAsync(cancellationToken)
                : Task.CompletedTask;
        }

        private bool ShouldLoad(object entity, string navigationName, [NotNullWhen(true)] out NavigationEntry? navigationEntry) {
            if (_loadedStates != null
                && _loadedStates.TryGetValue(navigationName, out var loaded)
                && loaded) {
                navigationEntry = null;
                return false;
            }

            if (Context.ChangeTracker.LazyLoadingEnabled) {
                SetLoaded(entity, navigationName, loaded: true);
                var entityEntry = Context.Entry(entity);
                if (entityEntry.State == EntityState.Detached) {
                    entityEntry.State = EntityState.Unchanged;
                }
                var tempNavigationEntry = entityEntry.Navigation(navigationName);
                if (!tempNavigationEntry.IsLoaded) {
                    navigationEntry = tempNavigationEntry;
                    return true;
                }
            }
            navigationEntry = null;
            return false;
        }

        public virtual void Dispose()
            => _disposed = true;
    }
}
