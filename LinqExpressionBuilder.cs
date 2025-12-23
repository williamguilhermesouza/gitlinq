using GitLinq.AST;
using System.Linq.Expressions;

namespace GitLinq
{
    public static class LinqExpressionBuilder
    {
        public static Expression<Func<T, bool>> BuildExpression<T>(BaseNode node)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = BuildExpression(node, parameter);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, object>> BuildSelector<T>(BaseNode node)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = BuildExpression(node, parameter);
            return Expression.Lambda<Func<T, object>>(Expression.Convert(body, typeof(object)), parameter);
        }

        private static Expression BuildExpression(BaseNode node, ParameterExpression parameter)
        {
            return node switch
            {
                IdentifierNode id => 
                    Expression.Property(parameter, id.Name),
                
                MemberAccessNode member => 
                    Expression.Property(BuildExpression(member.Target, parameter), member.Member),
                
                StringLiteralNode str => 
                    Expression.Constant(str.Value),
                
                MethodCallNode call => 
                    BuildMethodCall(call, parameter),
                
                LambdaNode lambda => 
                    BuildLambda(lambda, parameter),
                
                _ => throw new NotSupportedException($"Node type {node.GetType().Name} is not supported")
            };
        }

        private static Expression BuildMethodCall(MethodCallNode node, ParameterExpression parameter)
        {
            var target = BuildExpression(node.Target, parameter);
            var method = target.Type.GetMethod(node.Method);
            
            if (method == null)
                throw new InvalidOperationException($"Method {node.Method} not found on type {target.Type.Name}");

            var args = node.Arguments.Select(arg => BuildExpression(arg, parameter)).ToArray();
            return Expression.Call(target, method, args);
        }

        private static Expression BuildLambda(LambdaNode node, ParameterExpression parameter)
        {
            var lambdaParam = Expression.Parameter(typeof(object), node.Parameter);
            var body = BuildExpression(node.Body, parameter);
            return Expression.Lambda(body, lambdaParam);
        }
    }
}