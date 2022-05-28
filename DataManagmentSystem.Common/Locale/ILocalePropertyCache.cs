namespace DataManagmentSystem.Common.Locale
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface ILocalePropertyCache
    {
        IEnumerable<PropertyInfo> GetOrAdd(Type key);
    }
}
