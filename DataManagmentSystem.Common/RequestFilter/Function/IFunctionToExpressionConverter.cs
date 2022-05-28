using DataManagmentSystem.Common.Request;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DataManagmentSystem.Common.RequestFilter.Function {
    public interface IFunctionToExpressionConverter {
        SupportedFunction Function { get; }

        MethodCallExpression Convert(IEnumerable<FunctionParameter> parameters, ParameterExpression parameter, Type type);
    }
}
