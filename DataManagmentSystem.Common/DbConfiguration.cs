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
		private const string _dbProviderConfigurationKeyName = "DBProvider";
		private const string _postgreSqlConnectionStringConfigurationKeyName = "dbPostgreSql";
		private const string _mssqlConnectionStringConfigurationKeyName = "dbMSSQL";
		private const string _postgreSqlMigrationsAssemblyNameConfigurationKeyName = "POSTGRESQL_MIGRATIONS_ASSEMBLY_NAME";
		private const string _mssqlMigrationsAssemblyNameConfigurationKeyName = "MSSQL_MIGRATIONS_ASSEMBLY_NAME";
		private const string _mssqlProviderName = "MSSQL";
		private const string _postgreSqlProviderName = "PostgreSql";
		public static DbProvider GetDbProvider(IConfiguration configuration) {
			var dbProvider = configuration.GetValue(_dbProviderConfigurationKeyName, string.Empty);
			switch (dbProvider) {
				case _postgreSqlProviderName:
					return DbProvider.PostgreSql;
				case _mssqlProviderName:
					return DbProvider.MSSQL;
				default:
					throw new InvalidOperationException($"Unsupported provider: {dbProvider}");
			}
		}

		public static string GetMigrationsAssemblyName(IConfiguration configuration) {
			var dbProvider = GetDbProvider(configuration);
			switch (dbProvider) {
				case DbProvider.PostgreSql:
					return configuration.GetValue(_postgreSqlMigrationsAssemblyNameConfigurationKeyName, string.Empty);
				case DbProvider.MSSQL:
					return configuration.GetValue(_mssqlMigrationsAssemblyNameConfigurationKeyName, string.Empty);
				default:
					throw new InvalidOperationException($"Couldn't determine migrations assembly name for dbProvider: {dbProvider}");
			}
		}

		public static string GetConnectionString(IConfiguration configuration) {
			var dbProvider = GetDbProvider(configuration);
			switch (dbProvider) {
				case DbProvider.PostgreSql:
					return configuration.GetValue(_postgreSqlConnectionStringConfigurationKeyName, string.Empty);
				case DbProvider.MSSQL:
					return configuration.GetValue(_mssqlConnectionStringConfigurationKeyName, string.Empty);
				default:
					throw new InvalidOperationException($"Couldn't determine connection string for dbProvider: {dbProvider}");
			}
		}

		public static DbContextOptionsBuilder InitDbContextOptionsBuilder(IConfiguration configuration, DbContextOptionsBuilder optionsBuilder) {
			var dbProvider = GetDbProvider(configuration);
			switch (dbProvider) {
				case DbProvider.PostgreSql:
					return optionsBuilder.UseNpgsql(GetConnectionString(configuration), x => x.MigrationsAssembly(GetMigrationsAssemblyName(configuration)));
				case DbProvider.MSSQL:
					return optionsBuilder.UseSqlServer(GetConnectionString(configuration), x => x.MigrationsAssembly(GetMigrationsAssemblyName(configuration)));
				default:
					throw new InvalidOperationException($"Unsupported provider: {dbProvider}");
			}
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