namespace DataManagmentSystem.Common.Locale
{
    using DataManagmentSystem.Common.CoreEntities;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public interface ILocalizationReader
    {
        Task<BaseLocalizationEntity> Read(Guid entityId, CultureInfo culture, Type localeType);
    }
}
