namespace DataManagmentSystem.Common.PropertyCache {
    using System;
    using System.Reflection;

    public interface IPropertyCache {
        PropertyInfo GetPropertyByName(Type entityType, string propertyName); 
    }
}
