namespace DataManagmentSystem.Common.Locale
{
    using System.Collections.Generic;
    using System.Globalization;

    public interface ILanguageResolver
    {
        HashSet<string> RequiredLanguages { get; }
        HashSet<string> OptionalLanguages { get; }
        HashSet<string> SupportedLanguages { get; }

        CultureInfo GetCulture();
        CultureInfo GetDefaultCulture();
        bool IsCultureSupported(CultureInfo culture);
    }
}