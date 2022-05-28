namespace DataManagmentSystem.Common.RequestFilter.CustomFilters {
    using DataManagmentSystem.Common.Request;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System.Linq.Expressions;
    using System.Reflection;

    public class ContainsFilterExpressionBuilder : IFilterExpressionBuilder {
        private readonly bool isPostgreSql;
        
        public string Type => FilterType.Contains.ToString();

        public ContainsFilterExpressionBuilder(IConfiguration configuration) {
            var provider = configuration.GetValue("DBProvider", "PostgreSql");
            isPostgreSql = provider.Equals("PostgreSql");
        }

        public Expression GetExpression(Expression currentExpression, FilterType comparisonType, object value) {
            if (currentExpression.Type == typeof(string)) {
                var efLikeMethod = isPostgreSql ?
                    typeof(NpgsqlDbFunctionsExtensions).GetMethod(nameof(NpgsqlDbFunctionsExtensions.ILike),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(DbFunctions), typeof(string), typeof(string) },
                        null
                    )
                    :
                    typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(DbFunctions), typeof(string), typeof(string) },
                        null
                    );
                var pattern = Expression.Constant($"%{value}%", typeof(string));
                return Expression.Call(efLikeMethod,
                    Expression.Property(null, typeof(EF), nameof(EF.Functions)), currentExpression, pattern);
            } else {
                var valueExpression = Expression.Convert(Expression.Constant(value), currentExpression.Type);
                var method = currentExpression.Type.GetMethod("IndexOf", new[] { currentExpression.Type });
                var indexOf = Expression.Call(currentExpression, method, new[] { valueExpression });
                return Expression.GreaterThanOrEqual(indexOf, Expression.Constant(0));
            }
        }
    }
}
