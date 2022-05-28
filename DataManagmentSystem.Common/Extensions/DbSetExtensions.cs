namespace DataManagmentSystem.Common.Extensions {
    using DataManagmentSystem.Common.CoreEntities;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public static class DbSetExtensions {

        public static IQueryable<TEntity> Include<TEntity>(this DbSet<TEntity> set, IEnumerable<string> columns)
            where TEntity : BaseEntity
        {
            var query = (IQueryable<TEntity>) set;
            foreach (var column in columns) {
                query = query.Include(column);
            }
            return query;
        }
    }
}
