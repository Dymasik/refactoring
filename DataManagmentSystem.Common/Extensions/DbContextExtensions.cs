namespace DataManagmentSystem.Common.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    
    public static class DbContextExtensions
    {
        public static Type GetEntityTypeByTableName(this DbContext context, string tableName)
        {
			var baseType = context.Model
                .GetEntityTypes()
                .Single(t => t.GetTableName().Equals(tableName))
                .ClrType;
            var types = Assembly.GetAssembly(baseType).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(baseType));
            var type = types.FirstOrDefault(t => types.All(tt => tt.IsAssignableFrom(t)));
            return type ?? baseType;
        }

        public static Task<List<TSource>> ToListAsyncSafe<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not IAsyncEnumerable<TSource>)
                return Task.FromResult(source.ToList());
            return source.ToListAsync();
        }
    }
}
