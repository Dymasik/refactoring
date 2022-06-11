namespace DataManagmentSystem.Common.RequestFilter.CustomFilters {
    using DataManagmentSystem.Common.Request;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class InFilterExpressionBuilder : IFilterExpressionBuilder {
        public string Type => FilterType.In.ToString();

        public Expression GetExpression(Expression currentExpression, FilterType comparisonType, object value) {
            var targetEnumerationType = GetEnumerationType(currentExpression.Type);
            var convertMethod = GetType().GetMethod(nameof(GetStrongTypedList), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(currentExpression.Type);
            var objectList = convertMethod.Invoke(this, new[] { value });
            return Expression.Call(GetContainsMethod(currentExpression.Type),
                Expression.Constant(objectList, targetEnumerationType), currentExpression);
        }

        private static Type GetEnumerationType(Type type) {
            return type.MakeArrayType();
        }

        private static T[] GetStrongTypedList<T>(IList<object> objectList) {
            var typedObjectList = new T[objectList.Count];
            var index = 0;
            foreach (var item in objectList) {
                typedObjectList[index] = (T) item;
                index++;
            }
            return typedObjectList;
        }

        private MethodInfo GetContainsMethod(Type type) {
            return typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(type);
        }
    }
}
