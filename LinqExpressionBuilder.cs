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
                    return Expression.Constant(new GitService(".").GetCommits().ToList());
                else
                    throw new NotSupportedException("Only commits supported");
            case MemberAccessNode memberAccessNode:
                return new Expression.Field();
            case MethodCallNode methodCallNode:
            case LambdaNode lambda:
            default:
                throw new NotSupportedException("Node type not found");
        }
    }
}