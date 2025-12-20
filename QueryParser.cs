using GitLinq.AST;
using Sprache;

namespace GitLinq
{
    public static class QueryParser
    {
        private static readonly Parser<string> Identifier = 
            Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text().Token();

        private static readonly Parser<string> StringLiteral =
            from open in Parse.Char('"')
            from content in Parse.CharExcept('"').Many().Text()
            from close in Parse.Char('"')
            select content;

        private static readonly Parser<BaseNode> IdNode = 
            Identifier.Select(name => (BaseNode)new IdentifierNode(name));

        private static readonly Parser<BaseNode> MemberAccess =
            from target in IdNode
            from dot in Parse.Char('.')
            from member in Identifier
            select (BaseNode)new MemberAccessNode(target, member);

        private static readonly Parser<BaseNode> Root =
            MemberAccess.Or(IdNode).Or(StringLiteral.Select(s => (BaseNode) new StringLiteralNode(s)));
            
        public static string ParseExpression(string inputExpression)
        {
            var rootNode = Root.Parse(inputExpression);
            return rootNode.ToString();
        }

    }
}
