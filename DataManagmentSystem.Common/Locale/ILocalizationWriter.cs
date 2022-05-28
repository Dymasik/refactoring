namespace DataManagmentSystem.Common.Locale
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ILocalizationWriter
    {
        Task Write(IEnumerable<KeyValuePair<string, string>> localizations, string languageCode, Type localeType, Guid entityId);
    }
}
