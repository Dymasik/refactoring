namespace DataManagmentSystem.Common
{
	using Microsoft.Extensions.Configuration;
	using System;
	using Microsoft.EntityFrameworkCore;

	public enum DbProvider
	{
		PostgreSql = 0,
		MSSQL = 1
	}

	public static class DbConfiguration
	{
		private const string DbProviderConfigurationKeyName = "DBProvider";
		private const string PostgreSqlConnectionStringConfigurationKeyName = "dbPostgreSql";
		private const string MssqlConnectionStringConfigurationKeyName = "dbMSSQL";
		private const string PostgreSqlMigrationsAssemblyNameConfigurationKeyName = "POSTGRESQL_MIGRATIONS_ASSEMBLY_NAME";
		private const string MssqlMigrationsAssemblyNameConfigurationKeyName = "MSSQL_MIGRATIONS_ASSEMBLY_NAME";
		private const string MssqlProviderName = "MSSQL";
		private const string PostgreSqlProviderName = "PostgreSql";
		public static DbProvider GetDbProvider(IConfiguration configuration) {
			var dbProvider = configuration.GetValue(DbProviderConfigurationKeyName, string.Empty);
            return dbProvider switch
            {
                PostgreSqlProviderName => DbProvider.PostgreSql,
                MssqlProviderName => DbProvider.MSSQL,
                _ => throw new InvalidOperationException($"Unsupported provider: {dbProvider}"),
            };
        }

		public static string GetMigrationsAssemblyName(IConfiguration configuration) {
			var dbProvider = GetDbProvider(configuration);
            return dbProvider switch
            {
                DbProvider.PostgreSql => configuration.GetValue(PostgreSqlMigrationsAssemblyNameConfigurationKeyName, string.Empty),
                DbProvider.MSSQL => configuration.GetValue(MssqlMigrationsAssemblyNameConfigurationKeyName, string.Empty),
                _ => throw new InvalidOperationException($"Couldn't determine migrations assembly name for dbProvider: {dbProvider}"),
            };
        }

		public static string GetConnectionString(IConfiguration configuration) {
			var dbProvider = GetDbProvider(configuration);
            return dbProvider switch
            {
                DbProvider.PostgreSql => configuration.GetValue(PostgreSqlConnectionStringConfigurationKeyName, string.Empty),
                DbProvider.MSSQL => configuration.GetValue(MssqlConnectionStringConfigurationKeyName, string.Empty),
                _ => throw new InvalidOperationException($"Couldn't determine connection string for dbProvider: {dbProvider}"),
            };
        }

		public static DbContextOptionsBuilder InitDbContextOptionsBuilder(IConfiguration configuration, DbContextOptionsBuilder optionsBuilder) {
			var dbProvider = GetDbProvider(configuration);
            return dbProvider switch
            {
                DbProvider.PostgreSql => optionsBuilder.UseNpgsql(GetConnectionString(configuration), x => x.MigrationsAssembly(GetMigrationsAssemblyName(configuration))),
                DbProvider.MSSQL => optionsBuilder.UseSqlServer(GetConnectionString(configuration), x => x.MigrationsAssembly(GetMigrationsAssemblyName(configuration))),
                _ => throw new InvalidOperationException($"Unsupported provider: {dbProvider}"),
            };
        }

		public static void AfterMigration(DbContext context, IConfiguration configuration) {
			var dbProvider = GetDbProvider(configuration);
			switch (dbProvider) {
				case DbProvider.PostgreSql:
					AfterMigrationPostgreSql(context);
					break;
				case DbProvider.MSSQL:
					AfterMigrationMsSql(context);
					break;
				default:
					throw new InvalidOperationException($"Unsupported provider: {dbProvider}");
			}
		}

        private static void AfterMigrationMsSql(DbContext context) {
			context.Database.ExecuteSqlInterpolated($@"
							CREATE FUNCTION dbo.GetDistance (
								@lng1 float,
								@lat1 float,
								@lng2 float,
								@lat2 float
							)
							RETURNS float
							AS
							BEGIN
								RETURN(0.0);
							END;
						");
		}

        private static void AfterMigrationPostgreSql(DbContext context) {
			context.Database.ExecuteSqlInterpolated($@"CREATE EXTENSION IF NOT EXISTS earthdistance CASCADE;");
			context.Database.ExecuteSqlInterpolated($@"
							CREATE OR REPLACE FUNCTION ""GetDistance""(
								lng1 numeric,
								lat1 numeric,
								lng2 numeric,
								lat2 numeric)
							RETURNS numeric
							LANGUAGE plpgsql
							AS
							$$
							BEGIN
								RETURN (point(lng1, lat1) <@> point(lng2, lat2));
							END;
							$$
						");
		}
    }
}