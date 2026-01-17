using GitLinq.AST;
using GitLinq.Services;
using System.Linq.Expressions;
using System.Reflection;

namespace GitLinq;

public class LinqExpressionBuilder
{
    private readonly Dictionary<string, ParameterExpression> _parameters = new();
    private readonly string _repositoryPath;

    public LinqExpressionBuilder(string? repositoryPath = null)
    {
        _repositoryPath = repositoryPath ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Builds a LINQ expression from an AST node and compiles it to execute against commits.
    /// Returns the result as an object (can be IEnumerable, single item, or scalar).
    /// </summary>
    public object? Execute(BaseNode node)
    {
        var expression = BuildExpression(node);
        
        // If the result is already a constant, return its value
        if (expression is ConstantExpression constExpr)
        {
            return constExpr.Value;
        }

        // Compile and execute the expression
        var lambda = Expression.Lambda(expression);
        var compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }

    public Expression BuildExpression(BaseNode node)
    {
        return node switch
        {
            StringLiteralNode stringLiteralNode => BuildStringLiteral(stringLiteralNode),
            IdentifierNode identifierNode => BuildIdentifier(identifierNode),
            MemberAccessNode memberAccessNode => BuildMemberAccess(memberAccessNode),
            MethodCallNode methodCallNode => BuildMethodCall(methodCallNode),
            LambdaNode lambdaNode => BuildLambda(lambdaNode),
            _ => throw new NotSupportedException($"Node type {node.GetType().Name} not supported")
        };
    }

    private Expression BuildStringLiteral(StringLiteralNode node)
    {
        return Expression.Constant(node.Value);
    }

    private Expression BuildIdentifier(IdentifierNode node)
    {
        // Check if this identifier is a parameter (from a lambda)
        if (_parameters.TryGetValue(node.Name, out var parameter))
        {
            return parameter;
        }

        // Otherwise, check for known data sources
        return node.Name switch
        {
            "Commits" => Expression.Constant(GetCommits()),
            _ => throw new NotSupportedException($"Unknown identifier: {node.Name}")
        };
    }

    private List<CommitInfo> GetCommits()
    {
        var gitRoot = GitService.FindGitRoot(_repositoryPath) ?? _repositoryPath;
        return new GitService(gitRoot).GetCommits();
    }

    private Expression BuildMemberAccess(MemberAccessNode node)
    {
        var target = BuildExpression(node.Target);
        
        // Get the member (property or field) from the target type
        var memberInfo = target.Type.GetMember(node.Member, 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            .FirstOrDefault();

        if (memberInfo == null)
        {
            throw new NotSupportedException($"Member '{node.Member}' not found on type '{target.Type.Name}'");
        }

        return memberInfo switch
        {
            PropertyInfo prop => Expression.Property(target, prop),
            FieldInfo field => Expression.Field(target, field),
            _ => throw new NotSupportedException($"Member type {memberInfo.MemberType} not supported")
        };
    }

    private Expression BuildMethodCall(MethodCallNode node)
    {
        var target = BuildExpression(node.Target);
        var arguments = node.Arguments.Select(BuildExpression).ToList();

        // Handle LINQ extension methods (Where, Select, etc.)
        if (IsEnumerableType(target.Type))
        {
            return BuildLinqMethodCall(target, node.Method, arguments);
        }

        // Handle instance methods (like string.Contains)
        return BuildInstanceMethodCall(target, node.Method, arguments);
    }

    private Expression BuildLinqMethodCall(Expression source, string methodName, List<Expression> arguments)
    {
        var elementType = GetEnumerableElementType(source.Type);
        
        return methodName switch
        {
            "Where" => BuildWhereCall(source, arguments, elementType),
            "Select" => BuildSelectCall(source, arguments, elementType),
            "OrderBy" => BuildOrderByCall(source, arguments, elementType, ascending: true),
            "OrderByDescending" => BuildOrderByCall(source, arguments, elementType, ascending: false),
            "Take" => BuildTakeCall(source, arguments, elementType),
            "Skip" => BuildSkipCall(source, arguments, elementType),
            "First" => BuildFirstCall(source, arguments, elementType),
            "FirstOrDefault" => BuildFirstOrDefaultCall(source, arguments, elementType),
            "Any" => BuildAnyCall(source, arguments, elementType),
            "Count" => BuildCountCall(source, arguments, elementType),
            _ => throw new NotSupportedException($"LINQ method '{methodName}' not supported")
        };
    }

    private Expression BuildWhereCall(Expression source, List<Expression> arguments, Type elementType)
    {
        if (arguments.Count != 1 || arguments[0] is not LambdaExpression predicate)
            throw new ArgumentException("Where requires a lambda predicate");

        var whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(whereMethod, source, predicate);
    }

    private Expression BuildSelectCall(Expression source, List<Expression> arguments, Type elementType)
    {
        if (arguments.Count != 1 || arguments[0] is not LambdaExpression selector)
            throw new ArgumentException("Select requires a lambda selector");

        var resultType = selector.ReturnType;
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, resultType);

        return Expression.Call(selectMethod, source, selector);
    }

    private Expression BuildOrderByCall(Expression source, List<Expression> arguments, Type elementType, bool ascending)
    {
        if (arguments.Count != 1 || arguments[0] is not LambdaExpression keySelector)
            throw new ArgumentException("OrderBy requires a lambda key selector");

        var keyType = keySelector.ReturnType;
        var methodName = ascending ? "OrderBy" : "OrderByDescending";
        var orderByMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, keyType);

        return Expression.Call(orderByMethod, source, keySelector);
    }

    private Expression BuildTakeCall(Expression source, List<Expression> arguments, Type elementType)
    {
        if (arguments.Count != 1)
            throw new ArgumentException("Take requires a count argument");

        var takeMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Take" && m.GetParameters().Length == 2 
                        && m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(elementType);

        var countExpr = arguments[0].Type == typeof(int) 
            ? arguments[0] 
            : Expression.Convert(arguments[0], typeof(int));

        return Expression.Call(takeMethod, source, countExpr);
    }

    private Expression BuildSkipCall(Expression source, List<Expression> arguments, Type elementType)
    {
        if (arguments.Count != 1)
            throw new ArgumentException("Skip requires a count argument");

        var skipMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Skip" && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(elementType);

        var countExpr = arguments[0].Type == typeof(int)
            ? arguments[0]
            : Expression.Convert(arguments[0], typeof(int));

        return Expression.Call(skipMethod, source, countExpr);
    }

    private Expression BuildFirstCall(Expression source, List<Expression> arguments, Type elementType)
    {
        MethodInfo firstMethod;
        
        if (arguments.Count == 0)
        {
            firstMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "First" && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);
            return Expression.Call(firstMethod, source);
        }

        if (arguments[0] is not LambdaExpression predicate)
            throw new ArgumentException("First with arguments requires a lambda predicate");

        firstMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "First" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(firstMethod, source, predicate);
    }

    private Expression BuildFirstOrDefaultCall(Expression source, List<Expression> arguments, Type elementType)
    {
        MethodInfo firstOrDefaultMethod;
        
        if (arguments.Count == 0)
        {
            firstOrDefaultMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);
            return Expression.Call(firstOrDefaultMethod, source);
        }

        if (arguments[0] is not LambdaExpression predicate)
            throw new ArgumentException("FirstOrDefault with arguments requires a lambda predicate");

        firstOrDefaultMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(firstOrDefaultMethod, source, predicate);
    }

    private Expression BuildAnyCall(Expression source, List<Expression> arguments, Type elementType)
    {
        MethodInfo anyMethod;
        
        if (arguments.Count == 0)
        {
            anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);
            return Expression.Call(anyMethod, source);
        }

        if (arguments[0] is not LambdaExpression predicate)
            throw new ArgumentException("Any with arguments requires a lambda predicate");

        anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(anyMethod, source, predicate);
    }

    private Expression BuildCountCall(Expression source, List<Expression> arguments, Type elementType)
    {
        MethodInfo countMethod;
        
        if (arguments.Count == 0)
        {
            countMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);
            return Expression.Call(countMethod, source);
        }

        if (arguments[0] is not LambdaExpression predicate)
            throw new ArgumentException("Count with arguments requires a lambda predicate");

        countMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Count" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(countMethod, source, predicate);
    }

    private Expression BuildInstanceMethodCall(Expression target, string methodName, List<Expression> arguments)
    {
        var argTypes = arguments.Select(a => a.Type).ToArray();
        
        var method = target.Type.GetMethod(methodName, 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase,
            null, argTypes, null);

        if (method == null)
        {
            // Try to find a method with compatible parameter types
            method = target.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) 
                                     && m.GetParameters().Length == arguments.Count);
        }

        if (method == null)
        {
            throw new NotSupportedException($"Method '{methodName}' not found on type '{target.Type.Name}'");
        }

        return Expression.Call(target, method, arguments);
    }

    private Expression BuildLambda(LambdaNode node)
    {
        // We need to infer the parameter type from context
        // For now, assume CommitInfo type for commit queries
        var paramType = typeof(CommitInfo);
        var parameter = Expression.Parameter(paramType, node.Parameter);
        
        // Register the parameter so it can be resolved in the body
        _parameters[node.Parameter] = parameter;
        
        try
        {
            var body = BuildExpression(node.Body);
            return Expression.Lambda(body, parameter);
        }
        finally
        {
            // Clean up parameter after building
            _parameters.Remove(node.Parameter);
        }
    }

    private static bool IsEnumerableType(Type type)
    {
        return type.IsGenericType && 
               (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetInterfaces().Any(i => i.IsGenericType && 
                                              i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    private static Type GetEnumerableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
    }
}