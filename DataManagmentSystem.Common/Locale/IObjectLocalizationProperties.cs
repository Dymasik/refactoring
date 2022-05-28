namespace DataManagmentSystem.Common.Locale
{
    using System.Collections.Generic;
    using System.Globalization;

    public interface IObjectLocalizationProperties
    {
        IEnumerable<ILanguageResolver> LanguageResolvers { get; }

        CultureInfo GetCulture();
    }
}
