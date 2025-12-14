using System.Linq.Expressions;
using System.Reflection;
using GitLinq.Models;
using GitLinq.Services;

namespace GitLinq;

internal static class AstCompiler
{
    private static readonly HashSet<string> AllowedStringMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contains", "StartsWith", "EndsWith"
    };

    private static Expression<Func<CommitModel, bool>> BuildPredicate(LambdaNode lambda)
    {
        var param = Expression.Parameter(typeof(CommitModel), lambda.ParamName);
        var body = BuildPredicateBody(lambda.Body, param);
        return Expression.Lambda<Func<CommitModel, bool>>(body, param);
    }

    private static Expression BuildPredicateBody(AstNode node, ParameterExpression param)
    {
        switch (node)
        {
            case BinaryOpNode b:
                var left = BuildPredicateBody(b.Left, param);
                var right = BuildPredicateBody(b.Right, param);
                return b.Op switch
                {
                    "==" => Expression.Equal(left, EnsureType(right, left.Type)),
                    "!=" => Expression.NotEqual(left, EnsureType(right, left.Type)),
                    ">" => Expression.GreaterThan(left, EnsureType(right, left.Type)),
                    "<" => Expression.LessThan(left, EnsureType(right, left.Type)),
                    ">=" => Expression.GreaterThanOrEqual(left, EnsureType(right, left.Type)),
                    "<=" => Expression.LessThanOrEqual(left, EnsureType(right, left.Type)),
                    "&&" => Expression.AndAlso(left, right),
                    "||" => Expression.OrElse(left, right),
                    _ => throw new NotSupportedException($"Operator {b.Op} not supported")
                };

            case MemberAccessNode m:
                return BuildMemberAccessExpression(m, param);

            case LiteralNode lit:
                return Expression.Constant(lit.Value, lit.Value?.GetType() ?? typeof(object));

            case InstanceMethodCallNode im:
            {
                var targetExpr = BuildMemberAccessExpression(im.TargetMember, param);
                if (targetExpr.Type != typeof(string))
                    throw new NotSupportedException("Instance methods only supported on string.");
                var methodName = im.MethodName;
                if (!AllowedStringMethods.Contains(methodName))
                    throw new NotSupportedException($"Method {methodName} not allowed");
                var method = typeof(string).GetMethod(methodName, [typeof(string)]);
                if (method == null) throw new Exception($"String.{methodName}(string) not found");
                if (im.Args.Count != 1) throw new Exception($"{methodName} requires one argument");
                var argExpr = BuildPredicateBody(im.Args[0], param);
                if (argExpr.Type != typeof(string)) argExpr = Expression.Convert(argExpr, typeof(string));
                return Expression.Call(targetExpr, method, argExpr);
            }

            default:
                throw new NotSupportedException($"Node type {node.GetType().Name} not supported");
        }
    }

    private static Expression BuildMemberAccessExpression(MemberAccessNode node, ParameterExpression param)
    {
        var path = node.Path.ToList();
        Expression expr = param;
        if (path.Count > 0 && path[0] == param.Name) path = path.Skip(1).ToList();

        const BindingFlags bindingFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
        foreach (var segment in path)
        {
            var memberInfo = expr.Type.GetProperty(segment, bindingFlags)
                             ?? expr.Type.GetField(segment, bindingFlags) as MemberInfo;
 
            if (memberInfo == null)
                throw new Exception($"Member '{segment}' not found on type '{expr.Type.Name}'");

            expr = Expression.PropertyOrField(expr, memberInfo.Name);
        }

        return expr;
    }

    private static Expression EnsureType(Expression expr, Type targetType)
    {
        if (expr.Type == targetType) return expr;
        try
        {
            return Expression.Convert(expr, targetType);
        }
        catch
        {
            return expr;
        }
    }

    public static object? ExecuteQuery(RootQueryNode root, string repoPath)
    {
        if (!string.Equals(root.RootName, "Commits", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Only 'Commits' root is currently supported");

        IEnumerable<CommitModel> source = GitService.GetCommits(repoPath);
        object? current = source;

        foreach (var call in root.Calls)
        {
            var name = call.MethodName;
            if (string.Equals(name, "Where", StringComparison.OrdinalIgnoreCase))
            {
                if (call.Arguments is not [LambdaNode lambda])
                    throw new Exception("Where expects a single lambda argument");

                var predicateExpr = BuildPredicate(lambda);
                var predicate = predicateExpr.Compile();
                current = ((IEnumerable<CommitModel>)current).Where(predicate);
            }
            else if (string.Equals(name, "FirstOrDefault", StringComparison.OrdinalIgnoreCase))
            {
                current = ((IEnumerable<CommitModel>)current).FirstOrDefault();
                break;
            }
            else if (string.Equals(name, "First", StringComparison.OrdinalIgnoreCase))
            {
                current = ((IEnumerable<CommitModel>)current).First();
                break;
            }
            else if (string.Equals(name, "Take", StringComparison.OrdinalIgnoreCase))
            {
                if (call.Arguments.Count != 1 || call.Arguments[0] is not LiteralNode ln || ln.Value is not int n)
                    throw new Exception("Take expects a single numeric literal argument");
                current = ((IEnumerable<CommitModel>)current).Take(n);
            }
            else if (string.Equals(name, "Skip", StringComparison.OrdinalIgnoreCase))
            {
                if (call.Arguments.Count != 1 || call.Arguments[0] is not LiteralNode ln || ln.Value is not int n)
                    throw new Exception("Skip expects a single numeric literal argument");
                current = ((IEnumerable<CommitModel>)current).Skip(n);
            }
            else
            {
                throw new NotSupportedException($"Method '{name}' not supported in this demo");
            }
        }

        return current;
    }
}