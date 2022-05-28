namespace DataManagmentSystem.Common.Locale
{
    using DataManagmentSystem.Common.CoreEntities;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ILocalizationEntityResolver : IObjectLocalizationProperties
    {
        ILocalizationWriter Writer { get; }
        ILocalizableStringCache Cache { get; }
        bool IsDefaultCultureChoosed { get; }

        public IDictionary<string, string> GetLocalizations<TEntity>(EntityEntry<TEntity> entity) where TEntity : BaseEntity, ILocalizable;
        Task UpdateLocalizations<TEntity>(EntityEntry<TEntity> entity, IDictionary<string, string> localizations) where TEntity : BaseEntity, ILocalizable;
        bool IsEntityModified<TEntity>(EntityEntry<TEntity> entity) where TEntity : BaseEntity, ILocalizable;
        void UnstageLocalizedProperies<TEntity>(EntityEntry<TEntity> entity) where TEntity : BaseEntity, ILocalizable;
    }
}
