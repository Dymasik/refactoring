namespace DataManagmentSystem.Common.Locale
{
    using DataManagmentSystem.Common.CoreEntities;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    public interface IObjectLocalizer : IObjectLocalizationProperties
    {
        ILocalePropertyCache PropertyCache { get; }
        ILocalizableStringCache StringCache { get; }
        ILocalizationReader Reader { get; }

        Task<T> Localize<T>(T item, LocalizationDepth depth = LocalizationDepth.Shallow) where T : BaseEntity, ILocalizable;
        Task<T> Localize<T>(T item, CultureInfo culture, LocalizationDepth depth = LocalizationDepth.Shallow) where T : BaseEntity, ILocalizable;
        IEnumerable<T> Localize<T>(IEnumerable<T> items, LocalizationDepth depth = LocalizationDepth.Shallow) where T : BaseEntity, ILocalizable;
        IEnumerable<T> Localize<T>(IEnumerable<T> items, CultureInfo culture, LocalizationDepth depth = LocalizationDepth.Shallow) where T : BaseEntity, ILocalizable;
    }
}
