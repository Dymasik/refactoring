namespace DataManagmentSystem.Common.Locale
{
    using System.Collections.Generic;

    public interface ILocalizableStringCache
    {
        string GetString(string tableName, string key, string languageCode);
        bool Contains(string tableName, string key, string languageCode);
        void AddOrUpdate(string tableName, KeyValuePair<string, string> localizableString, string languageCode);
        void AddOrUpdate(string tableName, IDictionary<string, string> localizableStrings, string languageCode);
        bool TryRemove(string tableName, string key, string languageCode);
    }
}
