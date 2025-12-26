using GitLinq.AST;
using GitLinq.Services;
using System.Linq.Expressions;

namespace GitLinq;

public static class LinqExpressionBuilder
{
    public static Expression BuildExpression(BaseNode node)
    {
        switch (node)
        {
            case StringLiteralNode stringLiteralNode:
                return Expression.Constant(stringLiteralNode.Value);
            case IdentifierNode identifierNode:
                if (identifierNode.Name == "Commits")
                    return Expression.Constant(new GitService(Directory.GetCurrentDirectory()).GetCommits().ToList());
                else
                    throw new NotSupportedException("Only commits supported");
            case MemberAccessNode memberAccessNode:
                Console.Write(memberAccessNode.Target);
                Console.WriteLine($".{memberAccessNode.Member}");
                return Expression.Constant(null);
            case MethodCallNode methodCallNode:
                Console.Write(methodCallNode.Target);
                foreach(var arg in methodCallNode.Arguments)
                {
                    Console.Write(", ");
                    Console.Write(arg);
                }
                Console.WriteLine($".{methodCallNode.Method}");
                
                return Expression.Constant(null);
            case LambdaNode lambda:
                Console.Write("lambda: ");
                Console.Write("params: " + lambda.Parameter);                
                Console.WriteLine(lambda.Body);
                return Expression.Constant(null);
            default:
                throw new NotSupportedException("Node type not found");
        }
    }
}