using GitLinq.AST;
using Sprache;

namespace GitLinq
{
    public static class QueryParser
    {
        private static readonly Parser<string> Identifier =
            Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text().Token();

        private static readonly Parser<BaseNode> StringLiteral =
            from open in Parse.Char('"')
            from content in Parse.CharExcept('"').Many().Text()
            from close in Parse.Char('"')
            select (BaseNode) new StringLiteralNode(content);

        private static readonly Parser<BaseNode> IdNode =
            Identifier.Select(BaseNode (name) => new IdentifierNode(name));

        private static readonly Parser<BaseNode> MemberAccess =
            from target in IdNode
            from dot in Parse.Char('.')
            from member in Identifier
            select (BaseNode)new MemberAccessNode(target, member);

        private static readonly Parser<BaseNode> Call =
            from target in MemberAccess.Or(IdNode)
            from dot in Parse.Char('.')
            from method in Identifier
            from lparen in Parse.Char('(')
            from args in Parse.Ref(() => ExpressionParser).DelimitedBy(Parse.Char(',').Token()).Optional()
            from rparen in Parse.Char(')')
            select (BaseNode)new MethodCallNode(target, method, [..args.GetOrElse([])]);

        private static readonly Parser<BaseNode> Lambda =
            from lparen in Parse.Char('(').Optional()
            from param in Identifier
            from rparen in Parse.Char(')').Optional()
            from arrow in Parse.String("=>").Token()
            from body in ExpressionParser
            select (BaseNode)new LambdaNode(param, body);

        private static readonly Parser<BaseNode> ExpressionParser =
            Lambda.Or(Call).Or(MemberAccess).Or(IdNode).Or(StringLiteral);

        public static BaseNode ParseExpression(string inputExpression)
        {
            var rootNode = ExpressionParser.Parse(inputExpression);
            return rootNode;
        }

    }
}
