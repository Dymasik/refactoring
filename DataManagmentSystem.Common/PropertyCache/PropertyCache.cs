namespace DataManagmentSystem.Common.PropertyCache {
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;

    public class PropertyCache : IPropertyCache {

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _entityTypes;

        public PropertyCache() {
            _entityTypes = new ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>>();
        }

        public PropertyInfo GetPropertyByName(Type entityType, string propertyName) {
            var properties = _entityTypes.GetOrAdd(entityType, new ConcurrentDictionary<string, PropertyInfo>());
            var property = properties.GetOrAdd(propertyName, (_) => {
                var entityProperty = entityType.GetProperty(propertyName);
                return entityProperty ?? throw new ArgumentException(
                    $"In entity with type ${entityType.Name} there are no properties with name {propertyName}");
            });
            return property;
        }
    }
}
